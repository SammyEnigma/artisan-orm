# Articles

This folder contains a series of articles about the design patterns behind Artisan.Orm. They were originally published on [CodeProject](https://www.codeproject.com/) between 2016 and 2021. CodeProject went read-only in late 2024, leaving local PDFs as the only surviving copies; the articles have been re-published here from those PDFs.

The articles are licensed under **[CC-BY-SA-4.0](LICENSE)** — share, remix, and build upon them with attribution. The Artisan.Orm source code in the rest of the repository remains under the [MIT License](../Artisan.Orm/LICENSE.txt).

## The series

| # | Article | Original date |
|---|---|---|
| 1 | How to Save Object Graph in Master-Detail Relationship with One Stored Procedure | _to be re-published_ |
| 2 | Artisan.Orm — How to Reinvent the Wheel | _to be re-published_ |
| 3 | Artisan Way of Data Reply | _to be re-published_ |
| 4 | [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](04-hierarchyid-combination.md) | 17 Jul 2017 |

## Reading order

The articles can be read independently, but they were written as a progression:

1. **Article 1** introduces the core problem — saving a master-detail object graph through one stored procedure — and the *negative-identities* technique that makes it work.
2. **Article 2** generalises the patterns from Article 1 into a small ADO.NET helper library: Artisan.Orm.
3. **Article 3** adds the *DataReply* protocol for delivering structured business-logic outcomes from stored procedures back to the client.
4. **Article 4** extends the model with `hierarchyid` for cases where the data is itself hierarchical.

If you are new to the project and want a single recommended starting point, [Article 1](#) is the entry point.

## Relationship to the wiki

The [wiki](https://github.com/lobodava/artisan-orm/wiki) is the **reference documentation** for the library — short pages with the API of each method, organised for quick lookup. These articles are the **long-form narrative** behind the design — the *why* and the *how it evolved*. Where it makes sense, individual wiki pages link back to the corresponding article for the deeper story.
