# Startup

There is some special logic necessary when ArcDb opens a database.

## Validating WAL Files

ArcDb is always asked to open a Main database file, but part of opening the Database is [also](./file-formats/database.md) opening all the WAL files associated with it.

All [WAL files](./file-formats/wal.md) are listed, ordered by Version, and minimal validation is performed. "Minimal validation" means the footers are validated.

If any WAL file *except* the last WAL file (the highest Version) is invalid, then the database is corrupted and the opening fails. If there is only one WAL file and it is invalid, then that is OK.

If the *last* WAL file (the highest Version) is invalid, then that WAL file represents a Write Transaction that was aborted. If the database was opened in read-only mode, that WAL file is ignored; otherwise, it is deleted. Note that this check is done *after* the check for other invalid WAL files, so if there are multiple invalid WAL files, ArcDb properly interprets that as corruption and not multiple aborted transactions if the database is opened repeatedly.

If the last WAL file is also the only WAL file and also has Version 0 (as determined by the WAL file name, not the data in the file), then the initial database creation WAL has failed, and [special steps](./startup.md) are taken to bootstrap the new database.

## Lazy Writer

If there are any WAL files at all and the database is not in read-only mode, then the [Lazy Writer](./lazy-writer.md) is immediately started. There are no Read Transactions on a newly opened database, so the Lazy Writer will be able to treat *all* the WAL files as candidates for merging.

Note: opening the database does not have to wait for the first WAL to complete. It is entirely possible that the Main database file is invalid at this point (due to an incomplete WAL merge), but that doesn't matter beause the mappings in the first WAL file include all folios in the Main file that may be invalid.

## Schema Version

Once the database is open, ArcDb checks the schema version. If it is a larger value than ArcDb recognizes, then the database open fails.

## Automatic Upgrades

Once the database is open, if the schema version is out of date, then the database is eligible for an upgrade.

Users must *opt in* to automatic upgrades when opening the database, and may specify a maximum schema version when doing so. This is to ensure that different versions of user applications may access the same database file successfully.
