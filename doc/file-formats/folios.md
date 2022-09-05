# Folios

Every Folio has a header and a footer. Both the header and footer are 20-byte SHA-1 hashes of all the bytes in-between the header and footer.

The hash is verified whenever the folio is read from disk, and it is updated when writing out to disk.
