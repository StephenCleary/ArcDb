# The Storage Engine API

The Storage Engine API is the API divides the ArcDb storage engine from the ArcDb index engine.

The Storage Engine handles all the file formats and provides ACID guarantees. It provides Data Pages to the higher-level index engine.

All APIs are asynchronous.

## Entry-Point APIs

A given storage engine provides these APIs as the entry point to its services:

- OpenOrCreate Database: Returns a read/write database object.
- Open ReadOnly Database: Returns a read-only database object.

The database is closed when it is disposed.

TODO: Determine if we want to allow maintenance transactions to complete if closed asynchronously, e.g., GC. This would make asynchronous disposal work differently than synchronous disposal, though.

## Read-Only Database APIs

- Start Read Transaction: Starts a new [Read Transaction](./transactions.md) and returns a disposable object that ends the transaction when disposed.
- Backup: Perform a backup of this database to a new location. Backups are a special form of Read Transaction, and may run concurrently with Write Transactions without issue.
  - Some additional validation is performed during database backups.
- Validate: Performs a full validation of the database. Like Backups, Validations are a special form of Read Transaction and do not block Write Transactions.

## Read/Write Database APIs

In addition to the read-only database APIs:

- Start Write Transaction: Starts a new [Write Transaction](./transactions.md) and returns a disposable object that, when disposed, rolls back the transaction if it has not been committed.

## Read Transaction APIs

The Read Transaction object exposes these APIs:
- Read Database Header: Reads the reserved header section.
- Read Page: Reads a Data Page specified by a Logical Page Number.

## Write Transaction APIs

The Write Transaction object exposes these APIs, in addition to all the APIs on the Read Transaction object:

- Write Database Header: Writes the reserved header section.
- Allocate Page: Allocates a new Data Page, returning the new Logical Page Number.
- Free Page: Deallocates the specified Data Page.
- Write Page: Writes data to a Data Page.

### Implementations

Allocate Page:
- Allocate an LPN; if the [FLPN set](./file-formats/database.md#flpn-free-logical-page-numbers) has any entries, then remove the smallest one and use that value; otherwise, increment the largest allocated LPN in the [database header](./file-formats/database.md#database-header) and use that value.
- Remove the first entry in the [FFO set](./file-formats/database.md#ffo-free-folio-offsets). This is a Folio Offset (FO).
  - If there are no entries, then use a new FO appended to the file.
    - If the FO value is too large, then throw a TooMuchData exception.
- Add to the [LPN-FO](./file-formats/database.md#lpn-fo-logical-page-number---folio-offset) and [FO-LPN](./file-formats/database.md#fo-lpn-folio-offset---logical-page-number) maps.
- Return the LPN.

Free Page:
- Remove the LPN from the [LPN-FO](./file-formats/database.md#lpn-fo-logical-page-number---folio-offset) map, saving the FO.
- Remove the FO from the [FO-LPN](./file-formats/database.md#fo-lpn-folio-offset---logical-page-number) map.
- Add the FO to the [FFO set](./file-formats/database.md#ffo-free-folio-offsets).
- If the LPN is the largest allocated LPN in the [database header](./file-formats/database.md#database-header), then perform an LPN Trim; otherwise, add it to the [FLPN set](./file-formats/database.md#flpn-free-logical-page-numbers).
  - LPN Trim: Remove all consecutive highest values from the [FLPN](./file-formats/database.md#flpn-free-logical-page-numbers), and set the largest allocated LPN in the database header to the smallest of these.
    - Optimization: The smallest LPN in the trimmed values is also the largest LPN remaining in the [LPN-FO map](./file-formats/database.md#lpn-fo-logical-page-number---folio-offset).

# Logical Page Numbers (LPNs)

The Storage Engine API always accesses pages in terms of Logical Page Numbers. The Allocate Page method returns an LPN, and the Read Page, Write Page, and Free Page operations all take LPNs.

Nothing above the Storage Engine has any concept of Folios or metadata (except the reserved section in the database header). However, the Backup and Validate operations do operate on Folios.

LPNs are 32-bit (4-byte) unsigned integers, with `0` as an invalid LPN value.

# Fail-Fast

The Database has a fail-fast system: a `TaskCompletionSource` that latches the first unexpected exception. The Database is placed into an error state, with all future APIs immediately failing.
