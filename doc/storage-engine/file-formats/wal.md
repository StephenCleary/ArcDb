# WAL (Write-Ahead-Log) Files

There is one actual file per WAL.

Each WAL is a sequence of one or more [folios](./folios.md) that are updates to database folios (or new appended database folios), and the WAL ends with a sequence of at least one WAL metadata folio.

## WAL Metadata

The WAL Metadata always ends with a single Footer folio.

Before the footer folio, the WAL metadata may also include other metadata folios.

## Table of Contents (TOC)

WAL Metadata always includes a TOC. The TOC provides the mapping from database folios to WAL folios.

The TOC will exist within the footer folio if it can fit; otherwise, it is a B-tree preceding the footer.

## Footer

The footer is the last folio of a WAL file. Due to [the way WAL files are written](../transactions.md#commitrollback), an invalid footer makes the WAL invalid, and a valid footer makes the WAL valid.

The footer contains the number of database folios in the WAL file. It also contains the number of WAL metadata folios preceding the footer.

If the TOC fits within the footer, then the footer contains the TOC, and there are no other metadata folios.

If the TOC is large enough to require its own folio(s), then the footer contains a bloom filter. Footer folios for all WALs are always in memory, so this provides a quick check when looking up folio offsets for data pages (or other folios). Note that for very large WALs, the bloom filter loses its effectiveness, since it has a hard size limit (fitting in the footer).
