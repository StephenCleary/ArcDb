# Creating a New Database

ArcDb has some special logic when first creating a new database. This can happen in two situations:
- The Main file does not exist.
- The Main file is empty (zero bytes) and there are no valid WAL files.

In this case, ArcDb performs a maintenance Write Transaction to bootstrap the database. This Write Transaction is considered part of opening the database, and the open does not complete until the Write Transaction is committed.

## The Bootstrap Transaction

ArcDb will create 4 new folios, all of which are metadata: one header, plus one root node each for LPN-FO, FO-LPN, and FP. There are no data pages and no metadata freelist pages in a newly-created database.
