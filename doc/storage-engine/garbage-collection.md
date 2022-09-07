# Garbage Collection (GC)

The goal of GC at the storage engine level is just to consolidate Free Pages in the [Data Extent of the Database](./file-formats/database.md) into the end of the file, and then truncate the Database file, returning space to the OS disk.

The GC does not perform any kind of B-Tree optimization or compaction, nor does it concern itself with what is going on inside a [Folio](./file-formats/folios.md). It only cares whether Folios are used or not. It ignores metadata Folios completely, even if unused; metadata folios have their own [freelist](./file-formats/database.md#freelist) that does not have a GC.

The GC is a maintenance process that is always running, and will occasionally issue a Write Transaction. The GC does not run if the database is in read-only mode.

## GC Trigger

The [database header](./file-formats/database.md#database-header) tracks the number of free pages (the size of the FP set) and the total numer of folios in the Database. After the last [WAL](./file-formats/wal.md) is [Merged](./lazy-writer.md#merging-wal-files), the GC process checks whether a GC would be useful (e.g., database size could be reduced by 20% or more). If there are no Write Transactions waiting, the GC kicks off a GC Write Transaction.

## GC as a Write Transaction

The GC is a normal [Write Transaction](./transactions.md) that updates the Database Version as it moves data pages and modifies metadata structures.

The GC work should be limited to a reasonable number; never issue a "full GC". Each GC can only move some threshold of pages (though it can free more if they happen to already be at the end of the Database). Rationale: GC Write Transactions will block User Write Transactions.

The GC can move data pages from maximum FO (using the FO-LPN map) to minimum free pages (FP). Then trim to the new maximum FO.

The GC can also just be a "quick trim" (without moving pages) if there are sufficient free data pages already at the end of the data extent. Note that this does update the database header folio with the new total number of folios, so the Write Transaction is not actually empty.

TODO: Perhaps a GC should *always* be either a "quick trim" or a "move page(s)" operation.

Note: The GC can never run again immediately; the Lazy Writer must merge the GC before it can trigger again.

Note: Just like any other Write Transaction, the GC Write Transaction may include a Metadata Expansion.
