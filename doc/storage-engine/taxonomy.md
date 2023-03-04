# Taxonomy

When comparing databases, it can be useful to use precise terminology to classify database engines.

The following taxonomy statements use the terminology from [Transactional Information Systems: Theory, Algorithms, and the Practice of Concurrency Control and Recovery (Gerhard Weikum and Gottfried Vossen)](https://amzn.to/3KREWtG):

ArcDb uses the read-only multiversion protocol (ROMV), where writes use a 2-phase locking protocol (2PL) that is both conservative (C2PL) and strict (S2PL).

ArcDb's recovery algorithm is no-undo/with-redo (no-steal, no-force), deferred using page versioning.
