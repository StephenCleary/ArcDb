# ArcDb

An ACID .NET relational database.

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

## Non-Goals

- SQL. ArcDb is a relational database, but does not support SQL in any form.
  - ArcDb will expose an API based on relational algebra; SQL is a form of relational calculus. The two are equivalent in terms of expressive power.
- Relaxing isolation. Only Serializable transactions are supported; Repeatable Read, Read Committed, and Read Uncommitted are not supported.
- Relaxing durability. E.g., choosing to flush to disk less often than is required for full durability.

## Terminology

- Read Transaction: A transaction that has a read-only view of the database. All Read Transactions use snapshot semantics.
- Write Transaction: A transaction that can modify the database. All Write Transactions use serializable semantics.
  - ArcDb is implemented as a single-writer system (technically, single-writer-at-a-time system).
  - Every Write Transaction that actually updates the database has exactly one WAL (write-ahead log). Write Transactions that never write do not have a WAL.
- WAL: A write-ahead log *for a specific Write Transaction*.
  - WALs may be valid (representing a committed transaction) or invalid (representing a rolled back or in-progress transaction). Only Write Transactions have WALs.
- Main: The primary database file.
- Database: The primary database file logically combined with all valid WALs. This represents the current state of the database.
- Metadata: The portion of the database dedicated to storing database metadata, including the database header as well as structures for tracking page and folio usage.
- Folio: A contiguous block in the database (Main or WAL). Folios are the unit of read/write operations. Folios are either metadata or pages.
- Page: A folio that represents data rather than metadata. Pages (a.k.a. Data Pages) contain the relational database structures such as indexes, as well as end-user records.

## Technology

See the docs folder for details. To summarize:

### Transactions

- Transactions are explicitly Read or Write; it is not possible to change the type of a transaction once created.
- Read Transactions always use Snapshot semantics. They observe the state of the database that was current at the time of their creation.
- Write Transactions always use Serializable semantics. There is only one active Write Transaction permitted at a time (the others asynchronously block).
- Result: Read Transactions may run concurrently with other Read Transactions or Write Transactions.
- Result: Write Transactions may run concurrently with Read Transactions, and block on other Write Transactions.
- Result: Dirty Reads, Nonrepeatable Reads, Phantom Reads, Lost Updates, and Dirty Writes are not possible. Write Skew is possible; see below.
- Result: Write Transactions are never aborted by the system; there's no "deadlock loser" or "aborted transaction". Transaction deadlocks are not possible. The only way a Write Transaction can unexpectedly fail is if the underlying storage disk fails.
- Recommendations for transactions that read and then write:
  - If it is sufficiently performant, combine both the read and write into a single Write Transaction.
  - Otherwise, you will have to do a Read Transaction followed by a Write Transaction.
    - This approach can cause Write Skew. Recommendation: do a second read from within the Write Transaction to determine if it is still valid.

### MVCC (Multi-Version Concurrency Control)

ArcDb implements snapshot semantics by a kind of MVCC, but applied to database folios rather than end-user records:

- All Write Transactions write to a WAL. No part of the database is ever updated in-place. The WAL may be updated in-place since it is not considered part of the database until it is valid (committed).
- ArcDb is a single-writer system (technically, one-at-a-time writer). At all times, there must be either zero or one invalid (uncommitted) WALs.
- The WAL contains updates of the database, including both metadata and data. WALs are stored as folio updates.
- During the Write Transaction, Read Transactions may access the database freely.
- If the Write Transaction is rolled back, its WAL is simply deleted.
- If the Write Transaction is committed, then its WAL is made valid. This action atomically includes the new valid WAL in the database. Future Read and Write Transactions will use the new state of the database, including this now-valid WAL.

WALs are merged into the Main file by a maintenance process. WALs can only be merged if they are valid and if all Read Transactions previous to that WAL have completed.

Side effects:
- A long-running Read Transaction can severely impact system performance. The behaviour will always be *correct*, but disk space usage will grow significantly since the old state of the database must be retained.

This is similar to copy-on-write implementations, but with ArcDb the copies always go into the WAL instead of the Main database file.
