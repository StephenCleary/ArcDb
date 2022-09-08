# Folios

A Folio is the unit of disk access, currently 8 KB. The term "Folio" is used to distinguish from data "pages", which are one specific kind of folio.

Every Folio has a header and a footer. Both the header and footer are 20-byte SHA-1 hashes of all the bytes in-between the header and footer.

The hash is verified whenever the folio is read from disk, and it is updated when writing out to disk.

## Folio Types

The types of Folio pages are:
- (Metadata) Database Header
- (Metadata) LPN-FO Node
- (Metadata) FO-LPN Node
- (Metadata) FFO Node
- (Metadata) FLPN Node
- (Metadata) Freelist
- (Data) Data Page
- (Data) Free Page

TODO: Should this be an on-disk value in a folio header?

## Pages

Data Pages are a logical concept. Each Data Page has a Logical Page Number.

A LPN may be [mapped](../mapping.md) to a Folio, which contains the data for that page. Depending on the state of the database, a LPN may map to a Folio in the [Main file or a WAL](./database.md).
