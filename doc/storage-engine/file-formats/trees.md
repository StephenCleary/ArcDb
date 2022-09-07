# B-Trees

The metadata B-tree structures are slightly different in their details (e.g., FP has no values). But in general they follow these guidelines.

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

B-Trees on HDDs benefit from defragmenting their nodes; nodes should be ordered according to height and that order should be reflected in absolute file offsets.

However, tree defragmentation should *not* be turned on for SSD drives, which receive very little benefit at the cost of SSD lifetime. ArcDb by default has tree defragmentation turned off. TODO: allow the users to turn it on? It does seem like a lot of moving data around for relatively little benefit, even for HDDs.

Note: defragmentation *within* each node is always on.
