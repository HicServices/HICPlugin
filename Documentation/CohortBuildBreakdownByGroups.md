# Cohort Build Breakdown By Groups

Command: `ExportCohortBuildBreakDownByGroups` (project `RdmpCohortBuildBreakdownByGroups`).

Reproduces the Cohort Builder's count tree (each set's and container's FinalCount plus the cumulative
running totals as UNION/INTERSECT/EXCEPT are applied) split by a group column, for example Scottish
health board. Output is a wide CSV: one row per count point, a Total column (RDMP's own unfiltered
number), one column per group recognised by a user-supplied lookup table, then Other (present codes
the lookup does not recognise) and NotKnown (not in the reference table, or NULL group), with two
percentage rows at the bottom (% of final cohort and % of reference population). Groups plus Other
plus NotKnown reconcile to Total on every row. Columns appear only for groups observed in the cohort
or the reference population.

## How it works

The cohort is built once through `CohortCompiler` (populating the query cache); the lookup table is
read once. Every count point is then recomposed from the cached per-set identifier tables (the
cohort-set source queries are never re-run) and split by the group column with one GROUP BY per node,
joining the reference table on the cache server. Set operations are rendered per DBMS (Oracle EXCEPT
becomes MINUS); identifiers are quoted via the query syntax helpers.

## Inputs

Four `ColumnInfo` objects, mappable from the CLI (`ColumnInfo:1234`); the tables are derived:

- group-by column - its table is the reference table; the patient identifier is that table's single
  IsExtractionIdentifier column (a transformed identifier expression is refused)
- lookup key column (its table is the lookup table), lookup label column, and an optional lookup
  grouping column used to order the output columns

CLI example:

```
rdmp cmd ExportCohortBuildBreakDownByGroups CohortIdentificationConfiguration:<id> ColumnInfo:<group> ColumnInfo:<key> ColumnInfo:<label> out.csv ColumnInfo:<grouping>
```

## GUI and the SHARE preset

Right-clicking a cohort identification configuration offers two entries:

- "Export Build Breakdown By Groups (SHARE preset)" - resolves `SHARE_Demography`.`Region` and
  `z_hb_lookup`.`Region`/`HB_Name` by name at runtime (when several tables share the lookup name, the
  one co-located with the reference table wins); prompts only for a column it cannot resolve.
- "Export Build Breakdown By Groups (choose inputs)" - prompts for the group, key and label columns.

`SharePreset.cs` is the only place deployment-specific names live; on deployments without those
objects the preset finds nothing and the command prompts as normal.

## Requirements and preconditions

- The cohort identification configuration must have a query caching server (the command refuses
  otherwise - it works only on cached results).
- The reference table must be on the same database server as the query cache (validated); on
  PostgreSQL it must also be in the same database (one PostgreSQL connection cannot cross databases).
- Each identifier must map to at most one group (single-valued identifier-to-group mapping, e.g. a
  patient belongs to one health board). Multi-group membership double-counts and invalidates the
  NotKnown residual.
- The lookup table must have one row per code, unique labels, and labels that do not collide with the
  report's fixed column headers (Total, Other, NotKnown, ...); violations stop with a clear error.

## Tests

`HICPluginTests/CohortBuildBreakdownByGroupsTests.cs`: unit tests (lookup validation, transformed
identifier whitelist including aliases, PostgreSQL database-name normalization, per-DBMS set
operators, report projection) plus a deterministic DB-integration fixture (top EXCEPT over an
inclusion INTERSECT minus four exclusion sets, driven by a synthetic z_hb_lookup) asserting national
and per-group counts and cumulatives explicitly, that the unfiltered column equals RDMP's own
CohortCompiler counts, and that groups + Other + NotKnown == Total on every row. The fixture is
parameterised for SQL Server and PostgreSQL (the PostgreSql case skips where no connection string is
configured).

## History

Originally reviewed as HicServices/RDMP#2368 (all review threads addressed there); moved here as the
agreed long-term home.
