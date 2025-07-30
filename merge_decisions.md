# Merge Decisions

We merged the `LOCAL` branch into `main` using the recursive strategy with `theirs`
preference to maintain the new structure introduced in `LOCAL`. All conflicting
files were resolved automatically by accepting `LOCAL` versions. The previous
versions from `main` remain in history and in the `_backup` folder where
available. Further manual reconciliation may be required to fully integrate
features from both branches.
