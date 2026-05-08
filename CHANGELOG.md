# Changelog

All notable changes to **Artisan.ORM** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.0.0] — 2026

First major version since 3.5.1. Multi-targets `netstandard2.1`, `net8.0`, `net10.0`. Existing 3.x code continues to work; the changes below are additive or strictly internal unless called out under **Breaking**.

### Added

#### New APIs

- **`BulkCopy` / `BulkCopyAs` / `BulkCopyAsync` / `BulkCopyAsAsync`** — high-throughput inserts through `SqlBulkCopy`, fed by the same `MapperFor` mappers used elsewhere in the library. Sync and async variants on `RepositoryBase` and as `SqlConnection` extensions.
- **`ReadToLists<T1..T6>` / `ReadToListsAsync<T1..T6>`** — read multiple result sets into N strongly-typed lists in a single call. Available on `RepositoryBase`, `SqlCommand` and `SqlDataReader`. Removes the need for explicit `reader.NextResult()` boilerplate when stored procedures return several sets.
- **`ReadToAsyncEnumerable<T>` / `ReadAsAsyncEnumerable<T>`** — `IAsyncEnumerable<T>` streaming for very large result sets. Rows yield as they arrive; the reader and connection are disposed when enumeration ends or is cancelled.
- **`RunInTransaction` / `RunInTransactionAsync`** — `Action<SqlTransaction>` / `Func<SqlTransaction, …, Task>` wrappers with autocommit-on-return and rollback-on-exception. Overloads with and without explicit `IsolationLevel`.

#### MergeJoin

- Rewritten in-memory merge for master/detail and master/detail/sub-detail shapes; new overloads for two-detail (`TMaster + TFirstDetail + TSecondDetail`) and three-level (`TMaster + TDetail + TSubDetail`) result sets. Joins by FK lambdas, no ORM-level SQL `JOIN` required.

#### Async transaction API

- `RepositoryBase` now exposes proper `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` paths.
- `IAsyncDisposable` implemented; `await using var repo = new MyRepo()` is supported.
- `ConfigureAwait(false)` everywhere in async paths.
- `CancellationToken` parameters are honoured all the way down to ADO.NET calls.

#### Other additions

- `ArtisanMappingException` — single dedicated exception replacing internal `NullReferenceException` / `ApplicationException` from earlier versions.
- API symmetry pass: filled in missing sync/async overload pairs and `Action<SqlCommand>` / `params SqlParameter[]` variants across `Read*`, `Get*`, `Execute*`. (See commits `b8a3bfa`, `c48db1a`, `4c704b4` for the audit.)
- XML documentation comments on the entire public API; the generated `.xml` file ships inside the NuGet package.

### Changed

- **Targets** changed from single `netstandard2.1` to multi-target `netstandard2.1; net8.0; net10.0`.
- **Microsoft.Data.SqlClient** bumped from `5.2.3` (3.5.1) to `7.0.1`.
- **Nullable Reference Types** enabled across the entire library; public API now expresses nullability.
- **`AddParam` extensions** no longer use `dynamic` — replaced with `object`. Faster dispatch and AOT-friendlier; behaviour is unchanged.
- **`MappingManager`** — safe reflection and lazy initialization; no eager scan of all loaded assemblies on first use.
- **Mapper cache key** — derived from the reader schema instead of reflection on `SqlDataReader.Command` (which was brittle across `Microsoft.Data.SqlClient` versions).
- `NegativeIdentity` generators (`Int16/Int32/Int64NegativeIdentity`) are now thread-safe — `Interlocked`-based, no lock contention. Previously not safe for concurrent graph saves under load.
- `RepositoryBase` Dispose pattern reworked: proper `Dispose(bool disposing)` + finalizer-free design + `IAsyncDisposable`.

### Fixed

- `RecordTypeMapper` bug surfaced by the new test suite (commit `3957646`).
- Several spelling / formatting fixes in code samples and XML docs.

### Breaking

- `Microsoft.Data.SqlClient` major bump (5 → 7). Consumers that pin the package directly may need to update their constraint.
- Public types with reference-typed properties now declare nullability. Code that ignored nullable warnings will continue to compile; code with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` may need annotations.
- `ApplicationException` / bare `NullReferenceException` previously thrown from mapping paths are now `ArtisanMappingException`. `catch (Exception)` keeps working; specific catches need updating.
- Some internal helpers were removed during the symmetry pass; the public surface is unchanged.

### Packaging & tooling

- Deterministic builds (`ContinuousIntegrationBuild` on CI).
- [SourceLink](https://github.com/dotnet/sourcelink) embedded — F12 / step-into into library sources from any IDE.
- Symbol package (`.snupkg`) published alongside the main package.
- The four design articles originally on CodeProject ([Object Graph Saving](articles/01-object-graph-saving.md), [Reinventing the Wheel](articles/02-artisan-orm.md), [Data Reply](articles/03-data-reply.md), [Id-ParentId & HierarchyId](articles/04-hierarchyid-combination.md)) re-published in the [`articles/`](articles/) folder under [CC-BY-SA-4.0](articles/LICENSE) after CodeProject went read-only in late 2024.

---

## [3.5.1] — 2024-12

- `CancellationToken` support across asynchronous methods.
- `Microsoft.Data.SqlClient` upgraded to 5.2.3.

## [3.5.0] — 2024

- Library re-targeted to `.NET Standard 2.1`.

## [3.0.x]

- `3.0.0` — re-implemented as a `.NET Core 6.0` library.
- `Microsoft.Data.SqlClient` upgraded to 5.1.4 (vulnerability fix).
- Added `GetSqlXmlNullable`.

## [2.x.x]

- Built against `.NET Standard 2.0` for use with the .NET Framework and earlier .NET Core.
- Added `ReadDynamic` / `ReadDynamicList`.

## [1.x.x]

- Original release. Introduced the core patterns: `MapperFor`, `cmd.AddTableParam`, negative identities, the `DataReply` protocol, `ReadToTree` / `INode<T>`.
- See `articles/02-artisan-orm.md` for the origin story.

---

[4.0.0]: https://github.com/lobodava/artisan-orm/releases/tag/v4.0.0
[3.5.1]: https://www.nuget.org/packages/Artisan.ORM/3.5.1
[3.5.0]: https://www.nuget.org/packages/Artisan.ORM/3.5.0
