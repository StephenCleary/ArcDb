# Folios

A Folio is the unit of disk access, currently 8 KB. The term "Folio" is used to distinguish from data "pages", which are one specific kind of folio.

Every Folio has a header and a footer. Both the header and footer are 24 bytes. The first 20 bytes of the header and the last 20 bytes of the footer are SHA-1 hashes of all the bytes in-between the hashes. The 21st byte of the header is the folio type. The remaining 3 bytes in the header and first 4 bytes in the footer are reserved.

The hash is verified whenever the folio is read from disk, and it is updated when writing out to disk.

## Folio Types

The types of Folio pages are:
- (Metadata) Database Header (0)
- (Metadata) LPN-FO Node (1)
- (Metadata) FO-LPN Node (2)
- (Metadata) FFO Node (3)
- (Metadata) FLPN Node (4)
- (Metadata) Freelist (5)
- (Data) Data Page (6)
- (Data) Free Page (7)

## Pages

Data Pages are a logical concept. Each Data Page has a Logical Page Number.

A LPN may be [mapped](../mapping.md) to a Folio, which contains the data for that page. Depending on the state of the database, a LPN may map to a Folio in the [Main file or a WAL](./database.md).
