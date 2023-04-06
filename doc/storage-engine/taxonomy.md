# Taxonomy

When comparing databases, it can be useful to use precise terminology to classify database engines.

## Transactional Information Systems

The following taxonomy statements use the terminology from [Transactional Information Systems: Theory, Algorithms, and the Practice of Concurrency Control and Recovery (Gerhard Weikum and Gottfried Vossen)](https://amzn.to/3KREWtG):

ArcDb uses the read-only multiversion protocol (ROMV), where writes use a 2-phase locking protocol (2PL) that is both conservative (C2PL) and strict (S2PL).

ArcDb's recovery algorithm is no-undo/with-redo (no-steal, no-force), deferred using page versioning.

## Transaction Processing

The following taxonomy statements use the terminology from [Transaction Processing: Concepts and Techniques (Jim Gray and Adreas Reuter)](https://amzn.to/3TJ0hYL000):

ArcDb's transaction manager uses physical logging with shadows. Since there is only one write transaction at a time, there's an action-consistent checkpoint at each write transaction commit corresponding with a trivial action quiesce. The physical log uses a WAL with force-log-at-commit. The two fundamental problems of shadows (undoing failed operations, and reconstructing page-action consistency at restart) are handled with simple solutions: transaction-specific WALs are dropped, and WAL plus force-log-at-commit, respectively (fixing is trivially accomplished via the global write transaction lock). This results in a solution simpler than physiological logging, although it is definitely subject to the other problems mentioned: hotspot pages, long-running actions, and loss of physical clustering.

The book's authors summarize three problems with shadow paging: extra disk reads for page tables translating block addresses into slot addresses ("logical page number" to "folio offset" in ArcDb terms, i.e., the LPN-FO tree), destruction of data locality, and periodic closing requiring a "flurry of synchronous I/O activity. The first two problems are acknowledged, although their impact is not considered severe for an embedded database running on somewhat modern hardware. The third problem is eliminated by extending the canonical database to include completed WALs before they are merged into the main database file.

ArcDb's buffer management stores versioned pages using a modified differerential side file algorithm where side files are considered canonical (similar to a shadow page algorithm) the moment their transaction is committed. Copying pages into the original file is done eventually, after the commit.

Tuples are identified by primary keys. Regarding tuple values, F ≡ E, but because ArcDb is a database specifically for C#, the domain of E is approximately the domain of P.
