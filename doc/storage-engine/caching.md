# Caching

ArcDb is intended to work with the OS caching rather than bypassing it, like many other db systems do.

However, for efficiency, ArcDb has its own cache also. This can be considered a "hot cache", while letting the OS manage a "cold cache".

## Examples

### Reading a Folio

Folios are always read via a requested `(Version, FolioOffset)` tuple. The mapping system will translate this into a `(Version', FolioOffset)` tuple that may have a lower `Version` but must have the same `FolioOffset`. The mapping system also identifies the filename and disk offset where that folio data lives.

The cache manager then reads that folio (validating it) and stores it in its Read Cache (with the `(Version', FolioOffset)` key). This folio may be handed out to multiple transactions regardless of the type of the transaction. The folio usage is tracked via reference counting disposables. TODO: Should it stay in the Read Cache after being unused?

### Writing a Folio

Write access to a folio can only be requested by the single active Write Transaction. A newly allocated folio can be created in-memory, or a Read Folio can be converted to a Write Folio. To do this conversion, the Cache Manager will create a new `(Version, FolioOffset)` folio matching the Write Transaction version. Optimization: if the existing folio's version is *already* matching, then that folio has already been written by this Write Transaction and no creation is necessary. The Cache Manager then adds this folio to its Write Cache and returns it.

When the page is returned to the Write Cache, the Cache Manager may issue an eager write. TODO: limit the number of concurrent eager writes.

## File Access

All APIs used are asynchronous, but may run synchronously. This is because the Windows cache manager only has a certain number of dedicated threads, and it uses (synchronous) page faults to read data from disk. Other APIs only have synchronous versions (looking at you, `FlushFileBuffers`). And other APIs always run synchronously in some situations even if there are sufficient threads (e.g., writes that extend the file length). TODO: Consider running *all* our file APIs through `Task.Run`.

TODO: Fix-Use-Unfix protocol:
- Fix: Ensure page is loaded.
- Use: Obtain direct-access pointer.
- Unfix: Allow page to be unloaded.
(Buffer manager flushes as necessary)
