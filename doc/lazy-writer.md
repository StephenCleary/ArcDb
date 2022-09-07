# Lazy Writer

The Lazy Writer is a maintenance process that continuously runs in the background. Its job is to merge WAL files into the Main file.

The Lazy Writer does not run when the database is in read-only mode.

## Candidate WAL Files

The Lazy Writer only considers merging valid WAL files that are not needed by any current Read Transactions. I.e., the WAL Transaction Version must be less than the smallest Read Transaction Version.

## Merging WAL Files

The Lazy Writer merges its candidate WAL files one at a time, from the smallest Transaction Version to the largest.

For each file:
- The Lazy Writer uses the TOC within the WAL to copy folios from the WAL file into the Main file.
  - As each write completes, the in-memory mapping for the WAL folio is removed.
- Truncates the Main file if necessary, using the total number of database folios in the WAL footer.
- Flushes the Main file.
- Updates in-memory structures to remove the WAL completely (all mappings are already removed at this point).
- Deletes the WAL file.

Note that if a crash occurs when a WAL file is partially merged, on startup the WAL file will still override all those mappings (including those for metadata folios). In this case, the Lazy Writer will just start merging that WAL file over again.
