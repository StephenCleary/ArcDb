# Transactions

All transactions are Read Transactions or Write Transactions. This is specified when the transaction begins, and cannot be changed.

Read Transactions begin and eventually complete. There is no notion of "commit" or "rollback", since Read Transactions cannot change any on-disk data at all.

Write Transactions begin and eventually commit or rollback.

## Transaction Versions

Each Transaction Version identifies a fully consistent state of the database.

A Transaction Version is an unsigned 64-bit integer. This is sufficient for a sustained 1 billion write transactions per second for more than 500 years. ArcDb treats this as an infinite supply of monotonically increasing values.

The current version of the database is the Database Version. This exists on-disk and in-memory. It is equal to the most-recently-committed Write Transaction Version.

## Write Transactions

ArcDb is a single-writer system (technically, one-writer-at-a-time system). So, all write transactions exclude each other through the use of a database-wide asynchronous mutex. Note, however, that Read Transactions do *not* take that mutex, so they may run concurrently with the current Write Transaction.

### Begin

Write Transactions begin by taking the mutex. Once the mutex is taken, the write transaction has begun.

The current Write Transaction determines its Transaction Version by reading the Database Version and adding one. The Database Version is not updated (even in memory) until the Write Transaction is committed.

### Writes

All Write Transaction modifications - whether of data or metadata - are always done in a dedicated [WAL file](./file-formats/wal.md).

The first updated [folio](./file-formats/folios.md) also causes an update to the [database header](./file-formats/database.md#database-header) folio, updating the Database Version to the Write Transaction Version.

TODO: As each folio is written, we can request an OS write at that time, cancelling any previous writes to that WAL file offset.

Write Transaction folio mappings are buffered in memory, and written out to the [TOC in the WAL metadata](./file-formats/wal.md) when the transaction is committed.

TODO: It's possible to *stall* instead of *fail* when the disk is full, if writing to a WAL file. Should we? Or just fail-fast instead? A disk full error when extending the Main file would always be an error.

### Reads

When reading pages, the Write Transaction [maps](./mapping.md) Logical Page Numbers to Folios by using its own WAL first, then the list of valid WALs, and finally the Main file.

### Commit/Rollback

If the Write Transaction is committed but there are no updated folios, that commit is silently treated as a rollback instead.

If the Write Transaction is committed, part of that commit updates the Database Version to the Write Transaction's Version.

If the Write Transaction is rolled back, it is simply deleted and the Database Version does not increment.

To ensure atomicity and durability, committing a transaction follows these steps:
- Write out WAL metadata except the footer. If the TOC fits within the footer, this step is a noop.
- Flush to disk.
- Write out the WAL footer.
- Flush to disk.

Once the transaction has been committed to disk, it is committed to memory:
- The WAL file is added to the list of WAL files (including an in-memory copy of the WAL footer).
- The in-memory Database Version is updated to this Write Transaction's Version.

The Write Transaction is considered complete when the WAL footer has been flushed (and the in-memory commits are made). It does not have to wait for the [Lazy Writer](./lazy-writer.md) to merge the WAL into the Main database file.

### Notes

Database Versions are unique only if you only consider committed transactions. They may be equal to previous Write Transaction Versions that were rolled back, or equal to a Read Transaction's Version.

## Read Transactions

### Begin

Read Transactions copy the current Database Version, and that identifies the snapshot of the database read by this transaction. Read Transactions thus "share" their Transaction Versions with a former Write Transaction, and possibly other Read Transactions. They do not have a unique Transaction Version.

Read Transactions add their Transaction Version to a global in-memory list of Read Transactions. Note that there may be multiple Read Transactions with the same Transaction Version.

### Reads

When reading pages, the Read Transaction uses its Transaction Version to [map](./mapping.md) Logical Page Numbers to Folios using all valid WALs and the Main file.

### End

When the Read Transaction ends, it removes itself from the global in-memory list of Read Transactions. This notifies the [Lazy Writer](./lazy-writer.md) to merge the next WAL, if that action is applicable.
