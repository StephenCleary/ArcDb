# ArcDb

An ACID .NET relational database. More specifically, ArcDb is a disk-based, row-oriented OLTP storage engine.

## Goals

### Use Cases

ArcDb is intended to be embedded within a .NET application. While you *could* embed it within an ASP.NET application to create a more complete or traditional RDBMS, that is not the core use case of this library.

### Goals

- Simple enough to understand.
- Reasonable performance. However, unlike other relational database systems, performance is a secondary concern after simplicity.
- Reasonable maximums. ArcDb supports databases up to a few terabytes, but not petabyte databases.
- Support concurrency as much possible.
- Fully asynchronous from the ground up.
- Streaming result sets.
- Prefer reliability over performance. I.e., validate all reads from disk. More expensive reliability checks (e.g., structure validation) should be done opportunistically (e.g., during backups), but should not cause I/O beyond what would otherwise be done.
- Do not require the end-user to do regular maintenance (i.e., "vacuuming").
  - If large amounts of data are added and then removed, ArcDb must automatically reduce itself to a reasonable size (not necessarily as small as it was originally, but reduce to a *reasonable* size).
- Incorporate C# code into the database.
  - Conditions/checks are implemented in C#.
  - Use code generation to provide strongly-typed APIs and also hooks for arbitrary checks.
  - Note: new code checks are not automatically run on existing data.

### Non-Goals

- SQL. ArcDb is a relational database, but does not support SQL in any form.
  - ArcDb will expose an API based on relational algebra; SQL is a form of relational calculus. The two are equivalent in terms of expressive power.
- Relaxing isolation. Only Serializable transactions are supported; Repeatable Read, Read Committed, and Read Uncommitted are not supported.
- Relaxing durability. E.g., choosing to flush to disk less often than is required for full durability.
- `NULL`. It constantly causes surprising behavior.
  - Operations such as joins will require a user-supplied "null object value". This may be a literal C# `null`.
  - Missing data is represented as an additional index.

### Sub-Goal: Avoiding Pitfalls

There are some common pitfalls when working with databases which ArcDb is attempting to avoid. Hopefully we won't create our own common pitfalls along the way...

- Missing indexes. Perhaps one of the most common problems with all SQL databases is that a query is slow because there's a missing index in the database, or because the query planner didn't choose to use the desired index for a reason that isn't immediately obvious. It isn't long before every SQL programmer has to learn how to debug their query optimizer in addition to their own code.
  - ArcDb avoids this by making all index accesses explicit (relational algebra instead of relational calculus). The developer has to choose the index(es) to use. E.g., full table scans are *possible*, but they have to be explicit in the code, which also makes them *obvious*.
- Unsafe defaults. Some database systems ship out of the box with unsafe default behavior, usually for performance reasons. E.g., some common databases do not enforce foreign keys by default; other less-common databases are not ACID by default due to relaxed durability.
  - ArcDb chooses to be correct, even if it means being slower.
- Hidden maintenance requirements. Some common database systems have regular maintenance required or they will grow without bound over time, even if the average data usage remains constant. This is often not mentioned in the "getting started" documentation, and is often not discovered until after the database is causing problems in production.
  - ArcDb will not grow without bound over time for constant average data usage. Any required maintenance is included out of the box in an always-on state.
- Copy/paste ACID relaxing. This usually takes the form of relaxed transaction isolation (e.g., copy/pasting Read Uncommitted / Read Committed / Repeatable Read) or relaxed SQL locks (e.g., copy/pasting `NOLOCK`). These are done as optimizations, but they relax the ACID requirements in a way that is not appropriate for most scenarios. Then this code gets copy/pasted everywhere as "the faster version", often causing subtle bugs since the developers assume ACID is enforced when it has actually been relaxed.
  - ArcDb does not allow relaxing ACID. At all. If you want the fastest possible database at the expense of correctness, then this is not the solution for you.
- Retries for deadlocks. Most database systems use locking, and even the best lock strategies have some scenarios that can cause deadlocks. In this case, the deadlock must be detected and one of the transactions must choose to be (or be chosen as) the deadlock "victim" and rollback so that the system as a whole can proceed. At the very least, this means developers need to catch aborted-due-to-deadlock exceptions and retry them, treating that specific kind of error as transient. This is often not mentioned in the "getting started" documentation, and is often not discovered until after the database is causing problems in production.
  - ArcDb does not use any kinds of locks within its database. These kinds of deadlocks are not possible in ArcDb, so developers don't have to add code to handle them.

## Terminology

- [Read Transaction](./doc/storage-engine/transactions.md#read-transactions): A transaction that has a read-only view of the database. All Read Transactions use snapshot semantics.
- [Write Transaction](./doc/storage-engine/transactions.md#write-transactions): A transaction that can modify the database. All Write Transactions use serializable semantics.
  - ArcDb is implemented as a single-writer system (technically, single-writer-at-a-time system).
  - Every Write Transaction that actually updates the database has exactly one WAL (write-ahead log). Write Transactions that never write do not have a WAL.
- [WAL](./doc/storage-engine/file-formats/wal.md): A write-ahead log *for a specific Write Transaction*.
  - WALs may be valid (representing a committed transaction) or invalid (representing a rolled back or in-progress transaction). Only Write Transactions have WALs.
- Main: The primary database file.
- [Database](./doc/storage-engine/file-formats/database.md): The primary database file logically combined with all valid WALs. This represents the current state of the database.
- Metadata: The portion of the database dedicated to storing database metadata, including the database header as well as structures for tracking page and folio usage.
- [Folio](./doc/storage-engine/file-formats/folios.md): A contiguous block in the database (Main or WAL). Folios are the unit of read/write operations. Folios are either metadata or pages.
- Page: A folio that represents data rather than metadata. Pages (a.k.a. Data Pages) contain the relational database structures such as indexes, as well as end-user records.

## Technology

See the [docs folder](./doc/) for details. To summarize:

### Transactions

- Transactions are explicitly Read or Write; it is not possible to change the type of a transaction once created.
- Read Transactions always use Snapshot semantics. They observe the state of the database that was current at the time of their creation.
- Write Transactions always use Serializable semantics. There is only one active Write Transaction permitted at a time (the others asynchronously block).
- Result: Read Transactions may run concurrently with other Read Transactions or Write Transactions.
- Result: Write Transactions may run concurrently with Read Transactions, and block on other Write Transactions.
- Result: Dirty Reads, Nonrepeatable Reads, Phantom Reads, Lost Updates, and Dirty Writes are not possible. Write Skew is possible; see below.
- Result: Write Transactions are never aborted by ArcDb; there's no "deadlock loser" or "aborted transaction". Transaction deadlocks are not possible. The only way a Write Transaction can unexpectedly fail is if the underlying storage disk fails.
- Recommendations for transactions that read and then write:
  - If it is sufficiently performant, combine both the read and write into a single Write Transaction.
  - Otherwise, you will have to do a Read Transaction followed by a Write Transaction.
    - This approach can cause Write Skew. Recommendation: do a second read from within the Write Transaction to determine if it is still valid.

### MVCC (Multi-Version Concurrency Control)

ArcDb implements snapshot semantics by a kind of MVCC, but applied to database folios rather than end-user records:

- All Write Transactions write to a [WAL](./doc/storage-engine/file-formats/wal.md). No part of the database is ever updated in-place. The WAL may be updated in-place since it is [not considered part of the database](./doc/storage-engine/file-formats/database.md) until it is valid (committed).
- ArcDb is a single-writer system (technically, one-at-a-time writer). At all times, there must be either zero or one invalid (uncommitted) WALs.
- The WAL contains updates of the database, including both metadata and data. WALs are stored as folio updates.
- During the Write Transaction, Read Transactions may access the database freely.
- If the Write Transaction is rolled back, its WAL is simply deleted.
- If the Write Transaction is committed, then its WAL is made valid. This action atomically includes the new valid WAL in the database. Future Read and Write Transactions will use the new state of the database, including this now-valid WAL.

WALs are [merged into the Main file by a maintenance process](./doc/storage-engine/lazy-writer.md). WALs can only be merged if they are valid and if all Read Transactions previous to that WAL have completed.

Side effects:
- Prefer short-running Read Transactions (in terms of time) and smaller Write Transactions (in terms of space).
  - A long-running Read Transaction can severely impact system performance. The behaviour will always be *correct*, but disk space usage will grow significantly since the old state of the database must be retained.
  - Smaller Write Transactions are better. Specifically, when bulk loading records into a database that is concurrently being read from, you should break the bulk loading into groups of records, if possible.
- ArcDb gives strong guarantees (Serializable Writes and Snapshot Reads) and avoids common problems (Transaction Deadlocks, Phantom Reads, etc), but this comes at the cost of *disk space*. A busy ArcDb system will need sufficient temporary space for its WALs.

This is similar to copy-on-write implementations, but with ArcDb the copies always go into the WAL instead of the Main database file. It's also similar to multicomponent LSM trees and how they depend on compaction, but ArcDb uses B-trees instead of LSM trees; ArcDb does use WAL-scoped bloom filters in the same way as LSMs.

Using copy-on-write B-trees with WALs while insisting on full transaction isolation has an interesing result: ArcDb completely fails the RUM conjecture. Mutable B-Trees optimize for reads; immutable LSMs optimize for writes; and relaxing isolation optimizes for memory. ArcDb optimizes for none of these. As such, it *may* be useful as a performance baseline for database systems that do optimize for one of those concerns.

## Limitations

- Folio/Page size is 8 KB (8192 bytes).
- Folio Offsets (FO) are 32-bit (4-byte) numbers that are folio-sized. This results in a maximum Database size of 4294967296 * 8192 = 35184372088832 bytes, or 32 TB.
  - This is an acceptable limitation for an embedded relational database.
  - The actual amount of user data stored will be less, due to metadata and relational structure overheads.

## Comparison with SQLite

SQLite is currently the most common embedded relational database. ArcDb has several design differences with the out-of-the-box SQLite experience:

- Static typing instead of dynamic typing.
- Foreign keys are always enforced.
- No manual/periodic vacuuming or other maintenance tasks are necessary.
