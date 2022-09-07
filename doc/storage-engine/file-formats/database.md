# The Database

The Database is comprised of the Main database file combined with all valid [WAL](./wal.md) files. When composed, all these files together provide a logical view of the database.

If a database Main file is named `<base>.arc.db`, then the WAL files are named `<base>.<XXXXXXXXXXXXXXXX>.arc.wal`, where `<XXXXXXXXXXXXXXXX>` is the hexadecimal value of the transaction Version.

The Database has a number of [Folios](./folios.md) (blocks of data on disk). It starts with a variable number of metadata folios (the Metadata Extent), followed by a variable number of data pages (the Data Extent).

Note: the Main database file may contain invalid folios. The Database as a whole has only valid folios, but the Main file may temporarily be invalid during a [WAL merge](../lazy-writer.md#merging-wal-files).

## Metadata Extent

The first metadata folio - and thus the first folio in the Main database file - is the database header. The remaining metadata folios consist of free folios and metadata structures.

### Database Header

The database header folio includes these fields:

- A magic number identifying this as an ArcDb database file.
- A schema version identifying this on-disk structure.
- The last committed [transaction version](../transactions.md#transaction-versions). Note that this means any transaction that updates any data in the database at all also updates the database header folio.
- A "data header" section that is made available to higher levels as header space.
- Metadata folio numbers of:
  - The root node of the [LPN-FO map](#lpn-fo-logical-page-number---folio-offset).
  - The root node of the [FO-LPN map](#fo-lpn-folio-offset---logical-page-number).
  - The root node of the [FP set](#fp-free-pages).
- The metadata folio number of the first entry in the [metadata freelist](#freelist).
- The total number of entries in the FP set.
- The total number of folios in the Database.

## Metadata Structures

### LPN-FO (Logical Page Number -> Folio Offset)

This is a [B-Tree](./trees.md) that maps Logical Page Numbers (LPNs) to Folio offsets (FO).

This map is read when reading a logical page; the page number is looked up in this map to get the folio offset of the page data.

This map is appended to when allocating a data page, and removed from when deallocating a data page.

This map is updated when moving a logical page from one folio to another.

### FO-LPN (Folio Offset -> Logical Page Number)

This is a [B-Tree](./trees.md) that maps Folio offsets (used data pages) to Logical Page Numbers.

This map is read when doing garbage collection or metadata expansion.
- TODO: if we took more of the data header, we wouldn't need this for metadata expansion; can it be done away with completely?

This map is appended to when allocating a data page, and removed from when deallocating a data page.

This map is appended to and removed from when moving a logical page from one folio to another.

### FP (Free Pages)

This is a [B-Tree](./trees.md) without values; it is just a set of Folio offsets in the data section that are unused. Only data pages are represented in the FP set; free metadata pages are tracked via a freelist.

This set is read when searching for a free page or when doing garbage collection.

This set is appended to when deallocating a data page, and removed from when allocating a data page.

### Freelist

Unused metadata folios are in the freelist, which is just a linked list of free folios, with each folio pointing to the next folio in the freelist (or `0` if at the end).

## Data Extent

Folios in the Data Extent are either Data Pages or Free Pages.
