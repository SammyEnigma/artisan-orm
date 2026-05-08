# [![Artisan.Orm Logo](https://raw.githubusercontent.com/lobodava/artisan-orm/master/Logo.png)](http://www.nuget.org/packages/Artisan.ORM) Artisan.ORM

ADO.NET Micro-ORM to SQL Server. Multi-targets `.NET Standard 2.1`, `.NET 8` and `.NET 10` (since 4.0.0).
Use version 3.5.x for `.NET Standard 2.1`-only projects, or version 2.x.x (built against `.NET Standard 2.0`) for `.NET Framework` and earlier `.NET Core`.

## ADO.NET Micro-ORM to SQL Server.

First there was a desire to save a graph of objects for one access to the database: 
* one command on the client,
* one request to the application server,
* one access to the database.

Thus the method of [How to Save an Object Graph in Master-Detail Relationship with One Stored Procedure](articles/01-object-graph-saving.md) was found.

Then there was a desire of more control over Object-Relational Mapping, better performance and ADO.NET code reduction.

Thus a set of extensions to ADO.NET methods turned into a separate project. Here is a story about [Artisan.Orm or How To Reinvent the Wheel](articles/02-artisan-orm.md).

Finally the *object graph saving method* required a new approach to transmitting more details about exceptional cases. [The Artisan Way of Data Reply](articles/03-data-reply.md) became such an answer.

For hierarchical data — folder trees, organisation charts and the like — the Id-ParentId model can be combined with SQL Server's `hierarchyid` for the best of both worlds. See [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](articles/04-hierarchyid-combination.md).

> Note: the four articles above were originally published on [CodeProject](https://www.codeproject.com/) between 2016 and 2021. CodeProject went read-only in late 2024; the articles have been re-published in [`articles/`](articles/) under [CC-BY-SA-4.0](articles/LICENSE).  

## What's new in 4.0

- **Multi-target**: `netstandard2.1`, `net8.0`, `net10.0`.
- **`BulkCopy` / `BulkCopyAs`** — high-throughput inserts via `SqlBulkCopy`, fed by `MapperFor` mappers.
- **`ReadToLists<T1..T6>`** — multiple result sets to N strongly-typed lists in one call.
- **`ReadToAsyncEnumerable<T>` / `ReadAsAsyncEnumerable<T>`** — `IAsyncEnumerable<T>` streaming.
- **`MergeJoin`** — in-memory master/detail/sub-detail joining without ORM-level SQL joins.
- **`RunInTransaction` / `RunInTransactionAsync`** — autocommit-on-return transaction wrappers.
- **Nullable Reference Types** across the whole public API.
- **SourceLink + symbols (.snupkg)** — step-into debugging from any IDE.

See [CHANGELOG.md](CHANGELOG.md) for the full list.

## What to read for better understanding

Full information about Artisan.ORM is available in [documentation Wiki](https://github.com/lobodava/artisan-orm/wiki). 

The most interesting articles from Wiki are:

* [What's New in v4](https://github.com/lobodava/artisan-orm/wiki/Whats-New-in-v4)
* [The Sample](https://github.com/lobodava/artisan-orm/wiki/The-Sample)
* [Getting Started](https://github.com/lobodava/artisan-orm/wiki/Getting-Started)
* [Read Methods Understanding](https://github.com/lobodava/artisan-orm/wiki/Read-Methods-Understanding)
* [Mappers](https://github.com/lobodava/artisan-orm/wiki/Mappers)
* [cmd.AddTableParam](https://github.com/lobodava/artisan-orm/wiki/cmd.AddTableParam)
* [BulkCopy](https://github.com/lobodava/artisan-orm/wiki/BulkCopy)
* [MergeJoin](https://github.com/lobodava/artisan-orm/wiki/MergeJoin)
* [Streaming with IAsyncEnumerable](https://github.com/lobodava/artisan-orm/wiki/Streaming-Async-Enumerable)
* [Code Generation](https://github.com/lobodava/artisan-orm/wiki/Code-Generation)


## Some propositions, statements and additional information

Artisan.ORM was created to meet the following requirements:
* interactions with database should mostly be made through *stored procedures*;
* all calls to database should be encapsulated into *repository methods*;
* a *repository method* should be able to read or save a *complex object graph* with one *stored procedure*;
* it should work with the highest possible performance, even at the expense of the convenience and development time.

To achieve these goals Artisan.ORM uses:
* the `SqlDataReader` as the fastest method of data reading;
* a bunch of its own extensions to ADO.NET [SqlCommand](https://github.com/lobodava/artisan-orm/wiki/SqlCommand-Extensions) and [SqlDataReader](https://github.com/lobodava/artisan-orm/wiki/SqlDataReader-extentions) methods, both synchronous and asynchronous;
* strictly structured static [Mappers](https://github.com/lobodava/artisan-orm/wiki/Mappers);
* [user-defined table types](https://github.com/lobodava/artisan-orm/wiki/User-Defined-Table-Types) as a mean of object saving;
* [unique negative identities](https://github.com/lobodava/artisan-orm/blob/master/Artisan.Orm/NegativeIdentity.cs) as a flag of new entities;
* a [special approach](https://github.com/lobodava/artisan-orm/wiki/Negative-identities-and-object-graph-saving) to writing stored procedures for object reading and saving.

Artisan.ORM is available as [NuGet Package](http://www.nuget.org/packages/Artisan.ORM).

More examples of the Artisan.ORM usage are available in the [Tests](https://github.com/lobodava/artisan-orm/tree/master/Tests) and [Database](https://github.com/lobodava/artisan-orm/tree/master/Database) projects.
