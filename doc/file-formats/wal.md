# WAL Files

There is one actual file per WAL.

Each WAL is a set of one or more folio pages that are updates to database folios or new database folios, and the WAL ends with a TOC (Table of Contents) block.

## TOC (Table of Contents)

The TOC always ends with a single footer folio.

The TOC may also include other folio pages before the footer. These additional folio pages map from the 

## Footer

The footer is the last folio of a WAL file. Due to the way WAL files are written, an invalid footer makes the WAL invalid, and a valid footer makes the WAL valid.

The footer contains the number of database folio pages in the WAL file.

If the TO