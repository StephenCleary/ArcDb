# Validation

Different kinds of validation are done at different times.

## Continuous Validation

[Folio hashes](./file-formats/folios.md) are validated each time data is read from disk (or *may* have been read from disk).

TODO: We can add folio type validation here, too, if we store it.

## Backup Validation

The [metadata tree structures](./file-formats/database.md#metadata-structures) are validated using a one-pass algorithm. The metadata freelist is also validated using a one-pass algorithm.

TODO: B-tree height is also validated.
TODO: B-trees should all have fence keys to allow the one-pass algorithm to work completely.
TODO: B-trees may also have DLL pointers, which also will need validation.

## Full Validation

A Validate operation (full validation) performs an entire read of every folio in the database, and may need to read them multiple times. It includes all the validation performed by Continuous Validation and Backup Validation, and also validates:
- TODO: The page type of each page referenced by a metadata structure.
