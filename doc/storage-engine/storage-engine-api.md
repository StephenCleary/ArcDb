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
- Determine the LPN; either increment largest (if 64-bit) or perform a find-unused search in the LPN-FO mapping (if 32-bit).
- Remove the first entry in the FP set. This is a Folio Offset (FO).
  - If there are no entries, then use a new FO appended to the file.
- Add to the LPN-FO and FO-LPN maps.
- Return the LPN.

Free Page:
- Remove the LPN from the LPN-FO map, saving the FO.
- Remove the FO from the FO-LPN map.
- Add the FO to the FP set.

# Logical Page Numbers (LPNs)

The Storage Engine API always accesses pages in terms of Logical Page Numbers. The Allocate Page method returns an LPN, and the Read Page, Write Page, and Free Page operations all take LPNs.

Nothing above the Storage Engine has any concept of Folios or metadata (except the reserved section in the database header). However, the Backup and Validate operations do operate on Folios.

TODO: Determine if LPNs are 32-bit integers (requires some awkward logic when allocating a new one) or 64-bit integers (has higher storage costs for LPN-FO and FO-LPN mappings).

# Fail-Fast

The Database has a fail-fast system: a `TaskCompletionSource` that latches the first unexpected exception. The Database is placed into an error state, with all future APIs immediately failing.
