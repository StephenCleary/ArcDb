# Expanding Metadata

The Database is composed of Metadata and Data. The Metadata section keeps track of its free Folios using a freelist. When the metadata section runs out of free pages, it must *expand*.

The Metadata size (in folios) is always doubled when it expands. Metadata never shrinks; if data is mass-deleted from the database, the data pages are heuristically freed and returned to the OS by the Garbage Collector, but Metadata folio pages are never returned to the OS. When freed, Metadata pages enter the Metadata freelist.

Only Write Transactions can change any folios at all, so a metadata expansion always exists within a Write Transaction. The metadata expansion itself is included in the WAL and eventually merged to Main by the Lazy Writer, just like any other WAL modifications.

Note: Even if a Write Transaction only *frees* data pages, it is possible that the Metadata FP (Free Pages) set may run out of metadata space and trigger a metadata expansion.

Note: This means that metadata expansion must be safe to execute from within a write transaction. Indeed, *any* number of metadata expansions need to be safe (e.g., if a write transaction is a large bulk import).

## Steps

To expand the Metadata section, ArcDb follows these steps:

1. Calculate the old and new size of the Metadata. Since Metadata folios always preceded Data folios, `new - old` folios must be moved. These folios may be data pages or they may be free pages.
2. Allocate `new - old` new folios at the end of the database and copy the folio data from the old ones to the newly allocated ones.
3. Extend the metadata size. Add all new pages to the metadata freelist.
4. Update the metadata structures (LPN-FO, FO-LPN, and FP) with the new offsets for those pages.
