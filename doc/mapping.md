# Mapping

One of the more complex parts of ArcDb is how mapping is handled.

## WAL Folio Mapping

[The current Database is comprised of the Main file with all valid WALs](./file-formats/database.md). More generally, the Database at any specific Version is comprised of the Main file with all valid WALs where the WAL Version is less than or equal to the specified Version.

Thus, to read the data at a specific Folio Offset for a specific Version, the code will search the TOC of each WAL from largest to smallest (ignoring WALs for larger Versions) and take the first match, falling back to the Main file.

This is equivalent to thinking of the Database at that Version being the Main folios with each successive WAL overwriting them with its own changes.

So, in the general case, a specific Version of any Folio may exist either in the Main file or in any valid WAL.

In addition, for a [Write Transaction](./transactions.md), it *does* include its own (not yet valid) WAL in that mapping.

Optimization: if a Read Transaction has the same Version as the Main file, it can skip all the WAL mappings.

## Data Page Mapping

Data Pages have an additional mapping. To read the data of a specific Version of a logical page, it has to be first looked up in the [LPN-FO mapping](./file-formats/database.md#lpn-fo-logical-page-number---folio-offset) in the metadata. This metadata is *also* stored in folios, so it is subject to the same WAL folio mapping above.

The full steps are as follows:
- Put the database header folio (offset `0`) through the WAL mapping and read that data.
  - The [database header](./file-formats/database.md#database-header) has the folio offset of the LPN-FO tree root.
  - Optimization note: The database header will always be found in the WAL with the largest Version (or Main, if none), since every WAL updates the header with its own Version.
- Walk the LPN-FO tree to translate the LPN to an FO. Note that *each folio* of the LPN-FO tree also goes through the WAL mapping before it is read.
- Once you have the FO for the LPN, run that FO through the WAL mapping as well.
  - The data at that location is the logical page data for the specified version of the database.

Clearly, caching is *necessary* for reasonable performance.
