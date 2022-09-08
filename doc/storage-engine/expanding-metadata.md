# Expanding Metadata

[The Database is composed of Metadata and Data extents](./file-formats/database.md). The Metadata Extent keeps track of its free [Folios](./file-formats/folios.md) using a [freelist](./file-formats/database.md#freelist). When the metadata extent runs out of free pages, it must *expand*. Expansion is triggered by a metadata operation being unable to allocate a new metadata folio.

The Metadata extent size (in folios) is always doubled when it expands. The metadata extent never shrinks; if data is mass-deleted from the database, the data pages are heuristically freed and returned to the OS by the [Garbage Collector](./garbage-collection.md), but Metadata folio pages are never returned to the OS. When freed, Metadata pages enter the Metadata freelist.

Only Write Transactions can change any folios at all, so a metadata expansion always exists within a [Write Transaction](./transactions.md#write-transactions). The metadata expansion itself is included in the [WAL](./file-formats/wal.md) and eventually merged to Main by the [Lazy Writer](./lazy-writer.md), just like any other Folio modifications.

Note: Even if a Write Transaction only *frees* data pages, it is possible that the [Metadata FFO (Free Folio Offset) set](./file-formats/database.md#ffo-free-folio-offsets) may run out of metadata space and trigger a metadata expansion.

Note: This means that metadata expansion must be safe to execute from within a write transaction. Indeed, *any* number of metadata expansions need to be safe (e.g., if a write transaction is a large bulk import).

## Steps

To expand the Metadata section, ArcDb follows these steps:

1. Calculate the old and new size of the Metadata Extent. Since Metadata Extent folios always preceded Data Extent folios, `new - old` Data Extent folios must be moved. These folios may be data pages or they may be free pages.
2. Allocate `new - old` new folios at the end of the database and copy the folio data from the old ones to the newly allocated ones.
   - If a folio is already in the WAL, it just needs its offset updated.
   - Optimization: ignore folios if they are free pages, and adjust the allocation so they are not included.
3. Extend the metadata size. Add all new pages to the [metadata freelist](./file-formats/database.md#freelist).
4. Update the metadata structures ([LPN-FO](./file-formats/database.md#lpn-fo-logical-page-number---folio-offset), [FO-LPN](./file-formats/database.md#fo-lpn-folio-offset---logical-page-number), and [FFO](./file-formats/database.md#ffo-free-folio-offsets)) with the new offsets for those pages.

Finally, the original metadata operation (that was unable to allocate the metadata folio) is retried.
