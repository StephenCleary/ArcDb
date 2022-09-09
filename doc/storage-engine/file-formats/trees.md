# B-Trees

The metadata B-tree structures are slightly different in their details (e.g., FFO and FLPN have no values). But in general they follow these guidelines.

## Node Header

The node header includes:

- Height: Used to determine whether this node is a leaf node.
- B-Tree identifier: Used for validation only.
- Fence keys: Used for validation.
- (for leaf nodes) Previous/Next pointers: Used for scans and rebalancing.

## Algorithms

- Rebalancing will attempt to shuffle to left/right neighbors, to avoid splits/merges unless necessary.
- Right-only appends may use a Postgres-fastpath-style update. This does add a "last node" field to the database header which needs to be kept in sync.
- Right-only appends may use a SQLite-quickbalance-style update. This does leave the tree unbalanced.

Different metadata trees may opt-into different optimizations. TODO: Determine which ones use which optimizations.

Note: metadata trees do not support bulk loading optimizations (e.g., creating all-new leaf nodes and then merging with the existing last node).

## Defragmentation

ArcDb does not perform tree defragmentation. It should not be turned on for SSD drives and does not benefit many access patterns; it only provides benefits when doing scans on HDDs.

Note: defragmentation *within* each node is always on.
