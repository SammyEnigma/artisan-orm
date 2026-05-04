# Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data

> **Note (2024).** Originally published on [CodeProject in July 2017](https://www.codeproject.com/Articles/1192607/Combination-of-Id-ParentId-and-HierarchyId).
> CodeProject has since gone read-only and PDFs were the only surviving
> snapshot. This re-publication preserves the original content; a handful of
> typos and PDF extraction artifacts have been cleaned up, and the SQL is
> formatted for modern Markdown rendering. The patterns are timeless and
> apply unchanged to every SQL Server version since 2008.
>
> A working implementation lives in the Artisan.Orm sample repository:
> [`Database/`](../Database/) for the schema, procedures and SQL functions;
> [`Tests/DAL/Folders/`](../Tests/DAL/Folders/) for the C# repository code.
>
> — *Vadim Loboda, originally 17 Jul 2017*

Combine Id-ParentId and HierarchyId approaches to keep and fetch the data in hierarchical and alphabetical order.

## Introduction

### Hierarchical and Alphabetical Sorting

To sort the data hierarchically and alphabetically means sorting a tree using a depth-first search algorithm and selecting nodes with the same parent in the alphabetical order of their names.

The hierarchical sorting together with the level of nesting is a good way to emulate an expanded tree or a table of book contents using just a flat list of objects without the necessity to build a tree of objects.

Just fetch the records, sort them hierarchically and alphabetically, indent each record _n_ times — where _n_ is the level of nesting — and you get the expanded tree for UI and reports.

### Two Approaches

There are several approaches to structuring a relational database to store hierarchical data. The one that is used most frequently is the **Id-ParentId** approach.

The Id-ParentId approach has a few drawbacks that make it necessary to seek alternative methods. The most difficult and expensive tasks in Id-ParentId are those that require recursion, for example:

- find all descendants
- find all ancestors
- calculate the level of nesting
- sort flat records hierarchically and then alphabetically

In response to these difficulties, Microsoft added the **HierarchyId** data type to SQL Server starting from version 2008. HierarchyId:

- helps finding descendants and ancestors without recursion
- has a fast `GetLevel()` method
- contains the information about a node's position in the hierarchy
- contains the information about a node's order within its parent's children

If you build a clustered index on HierarchyId, all node-records are physically stored in the order in which they are most frequently fetched.

But HierarchyId still has drawbacks — trade-offs for its useful properties:

- impossibility to use HierarchyId as a foreign key to a parent (no self-referencing). A parent FK can technically be substituted with `PERSISTED REFERENCES`, but the pros and cons of such a solution are still under-researched.
- difficult ways to generate HierarchyId values for added and changed records to keep the required order — SQL Server does not do this automatically; it is up to the programmer to manage it.
- difficult use of HierarchyId as a primary or foreign key: it may regularly change.
- it has no corresponding type in JavaScript (for example).

So what to choose: Id-ParentId or HierarchyId?

An acceptable solution to this dilemma is **the combination of two approaches**: Id-ParentId and HierarchyId. This combination will allow us to:

- use `Id` as an integer, ever-increasing, unchangeable primary key
- use `ParentId` as a self-referencing foreign key
- use `Hid` (HierarchyId) to keep the records physically sorted in hierarchical and alphabetical order

This article describes the way to unite the both approaches and ensure their normal operation.

## Some Facts about HierarchyId

### Column and Field Names in this Article

- `Hid` — column of HierarchyId data type
- `HidPath` — string column for human-readable path of HierarchyId
- `HidCode` — a binary representation of HierarchyId converted to a string and without the leading `0x`

### To Human-Readable Format

The HierarchyId data type is a materialized path encoded into binary format. It can be decoded to a human-readable string path with the `ToString()` method:

```sql
select Hid, Hid.ToString() as HidPath, Hid.GetLevel() as Level
from   Folders
order by Hid;
```

```text
Hid           HidPath        Level
-----------   --------------  ------
0x            /               0
0x58          /1/             1
0x5AC0        /1/1/           2
0x5AD6        /1/1/1/         3
0x5AD6B0      /1/1/1/1/       4
0x5AD6B580    /1/1/1/1/1/     5
0x5AD6B680    /1/1/1/1/2/     5
0x5AD6B780    /1/1/1/1/3/     5
0x5AD6D0      /1/1/1/2/       4
0x5AD6D580    /1/1/1/2/1/     5
0x5AD6D680    /1/1/1/2/2/     5
0x5AD6D780    /1/1/1/2/3/     5
...
```

> Note: `ToString()` and `GetLevel()` are case-sensitive!

### From Human-Readable Format

The opposite operation — from a string path to the binary format — works as well:

```sql
declare @Hid hierarchyid = '/1/1/1/1/3/';
select @Hid;
-- 0x5AD6B780
```

The above is the implicit conversion. HierarchyId has the explicit `Parse()` method, although the call format is a little strange:

```sql
declare @Hid hierarchyid = hierarchyid::Parse('/1/1/1/1/3/');  -- Parse() is case-sensitive!
select @Hid;
-- 0x5AD6B780
```

### Tricky Numbering System of Paths

The interesting thing about HierarchyId is the way it manages the numeration of a node inserted between two other nodes.

To get a Hid for a new node, HierarchyId uses the `GetDescendant()` method on the parent Hid and the two Hids between which the new one has to be inserted:

```sql
declare @Parent hierarchyid = 0x;

print @Parent.GetDescendant('/1/',   '/2/'  ).ToString();   -- /1.1/
print @Parent.GetDescendant('/1/',   '/1.1/').ToString();   -- /1.0/
print @Parent.GetDescendant('/1/',   '/1.0/').ToString();   -- /1.-1/
print @Parent.GetDescendant('/1/',   '/1.-1/').ToString();  -- /1.-2/
print @Parent.GetDescendant('/1.1/', '/2/'  ).ToString();   -- /1.2/
print @Parent.GetDescendant('/1.2/', '/2/'  ).ToString();   -- /1.3/
print @Parent.GetDescendant('/1.3/', '/2/'  ).ToString();   -- /1.4/
print @Parent.GetDescendant('/1.3/', '/1.4/').ToString();   -- /1.3.1/

print @Parent.GetDescendant('/1.2.3.4.5.6.7.8/', '/1.2.3.4.5.6.7.9/').ToString();
-- /1.2.3.4.5.6.7.8.1/

-- by the way
declare @Hid hierarchyid = '/1.2.3.4.5.6.7.8.1/';
select @Hid;  -- 0x63A08A49A85258

declare @Hid hierarchyid = '/-1.-2.-3.-4.-5.-6.-7.-8.-1234567890/';
select @Hid;  -- 0x41F8F87A3C1D8E87216D9A81A73A

-- special cases with null
print @Parent.GetDescendant(null,  null ).ToString();  -- /1/
print @Parent.GetDescendant('/1/', null ).ToString();  -- /2/
print @Parent.GetDescendant(null,  '/1/').ToString();  -- /0/
```

### Why Need Binary Encoding?

If we can build a string path, why do we need this HierarchyId binary encoding? Could we just sort the hierarchical data by this string path?

Look at this example:

```sql
select hierarchyid::Parse('/1/')  as Hid, '/1/'  as HidPath union all
select hierarchyid::Parse('/2/')        , '/2/'             union all
select hierarchyid::Parse('/10/')       , '/10/'
order by HidPath;
```

```text
Hid     HidPath
-----   --------
0x58    /1/
0xAA    /10/
0x68    /2/
```

The same query but ordered by `Hid` produces the right sorting:

```text
Hid     HidPath
-----   --------
0x58    /1/
0x68    /2/
0xAA    /10/
```

Microsoft uses a sophisticated algorithm in HierarchyId to encode the string path so that it can be used for hierarchical sorting just by the `Hid` column value.

## The Solution

### Master and Slave

So let's combine the Id-ParentId self-referencing approach with HierarchyId.

The Id-ParentId part will be responsible for the `Id` primary key and the self-referencing `ParentId` foreign key — and will be the leading, or **master**.

The HierarchyId part will be a calculated field — the **slave**.

Of course, `Hid` as a persistent calculated field is denormalization. But this is a conscious step and a compromise for the advantages HierarchyId brings.

The best place and moment to calculate the HierarchyId and keep it in a coordinated state is a stored procedure, while saving the node.

### User Catalog for Experiments

Let's imagine a multi-user application where each user keeps its own catalog. This catalog stores information in hierarchical folders. The table for these folders might look like this:

```sql
create table dbo.Folders
(
    UserId    int           not null,
    Hid       hierarchyid   not null,
    Id        int           not null  identity,
    ParentId  int               null,
    Name      nvarchar(50)  not null,

    constraint CU_Folders        unique    clustered    (UserId asc, Hid asc),
    constraint PK_Folders        primary key nonclustered (Id asc),
    constraint FK_Folders_UserId foreign key (UserId)   references dbo.Users   (Id),
    constraint FK_Folders_ParentId foreign key (ParentId) references dbo.Folders (Id),

    constraint CH_Folders_ParentId check
        (Hid = 0x  and ParentId is null
         or Hid <> 0x and ParentId is not null)
);
```

Note that the **clustered index is built on `(UserId, Hid)`**, but the **primary key is on `Id`**. This allows us to:

- keep all the records of each user's Folders hierarchically sorted physically (the clustered index dictates physical order),
- use the integer `Id` as a primary key — and as a foreign key for other tables and DTOs.

It is well known that a hierarchy built on HierarchyId may have one root only. Because the clustered key is the combination of `UserId` and `Hid`, the `dbo.Folders` table may have one root **per user**.

The root node is mandatory — every user's catalog must have one — so the root is mostly a system record that users do not edit. Once the root is a system record, every other node must have a parent, and `ParentId` must be `not null`.

### The Best Place to Make the Calculation

As Microsoft states it: "It is up to the application to generate and assign HierarchyId values in such a way that the desired relationship between rows is reflected in the values."

The best way to create and keep the integrity and consistency of hierarchical data, in our case, is to always use the same stored procedure to insert and update a Folder node.

A user-defined table type that represents the saving Folder may serve as a parameter to the `SaveFolder` stored procedure:

```sql
create type dbo.FolderTableType as table
(
    Id        int           not null  primary key clustered,
    ParentId  int           not null,
    Name      nvarchar(50)  not null
);
```

And here is the `SaveFolder` stored procedure itself:

```sql
create procedure dbo.SaveFolder
    @Folder dbo.FolderTableType readonly
as
begin
    set nocount on;

    begin -- variable declaration

        declare
            @ParamId         int         ,
            @ParentId        int         ,
            @UserId          int         ,
            @ParentHid       hierarchyid ,
            @StartTranCount  int         ,
            @OldHid          hierarchyid ,
            @NewHid          hierarchyid ;

        declare @FolderIds table (
            InsertedId  int          not null,
            OldHid      hierarchyid      null,
            NewHid      hierarchyid      null
        );
    end;

    begin try
        set @StartTranCount = @@trancount;
        if @StartTranCount = 0
        begin
            set transaction isolation level serializable;
            begin transaction;
        end;

        begin -- init variables and lock parent for update

            select
                @ParamId  = Id,
                @ParentId = ParentId
            from
                @Folder;

            select
                @UserId    = UserId,
                @ParentHid = Hid
            from
                dbo.Folders
            where
                Id = @ParentId;
        end;

        begin -- save the @Folder
            merge into dbo.Folders as target
                using
                (
                    -- full join in this 'select' allows to see the picture
                    -- as if the change had already applied;
                    -- coalesce(t.x, f.x) takes the new value if t.x exists
                    -- and the old value if not.
                    select
                        -- LAG and LEAD functions help find the previous and next
                        -- Hid values when sorting by Name
                        Hid       =  @ParentHid.GetDescendant(
                                        LAG (case when t.Id is null then f.Hid end)
                                            over (order by coalesce(t.Name, f.Name)),
                                        LEAD(case when t.Id is null then f.Hid end)
                                            over (order by coalesce(t.Name, f.Name))
                                     ),
                        Id        =  coalesce(t.Id      , f.Id      ),
                        ParentId  =  coalesce(t.ParentId, f.ParentId),
                        Name      =  coalesce(t.Name    , f.Name    )
                    from
                        (select * from dbo.Folders where ParentId = @ParentId) f
                        full join @Folder t on t.Id = f.Id
                )
                -- For LAG and LEAD we need all children of @ParentId,
                -- but for MERGE we want only the Folder where source.Id = @ParamId.
                as source on source.Id = @ParamId and source.Id = target.Id

            -- target.UserId = @UserId here just to make sure that
            -- we do not reparent a Folder to another user.
            when matched and target.UserId = @UserId then
                update set
                    Hid       = source.Hid,
                    Name      = source.Name,
                    ParentId  = source.ParentId

            -- source.Id = 0 here just to make sure that we insert a new Folder,
            -- not an already-deleted one. A not-matched Id can be deleted by
            -- another user, and we can try to add it again.
            when not matched by target and source.Id = 0 then
                insert (UserId, Hid, ParentId, Name)
                values (@UserId, source.Hid, source.ParentId, source.Name)

            output
                inserted.Id,
                deleted.Hid,
                inserted.Hid
            into @FolderIds (InsertedId, OldHid, NewHid);
        end;

        begin -- reparent children
            -- Saving a folder might change its ParentId. In that case the
            -- Hid of all its children should reflect the change as well.
            select top 1
                @OldHid = OldHid, @NewHid = NewHid
            from @FolderIds;

            if @OldHid <> @NewHid
                update dbo.Folders set
                    Hid = Hid.GetReparentedValue(@OldHid, @NewHid)
                where
                    UserId = @UserId
                    and Hid.IsDescendantOf(@OldHid) = 1;
        end;

        if @StartTranCount = 0 commit transaction;
    end try
    begin catch
        if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;

        declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
        raiserror(@ErrorMessage, 16, 1);
        return;
    end catch;
end;
```

If you save each Folder using only this stored procedure, the `Hid` column will always have actual and consistent information.

## Fetch a Folder With Its Descendants in Hierarchical Order

The following stored procedure fetches a Folder with all its descendants and sorts them hierarchically and then alphabetically — as if they are an expanded tree:

```sql
create procedure dbo.GetFolderWithSubFolders
    @FolderId int
as
begin
    set nocount on;

    select
        d.Id       ,
        d.ParentId ,
        d.Name     ,
        [Level]  =  d.Hid.GetLevel(),
        HidCode  =  dbo.GetHidCode(d.Hid),
        HidPath  =  d.Hid.ToString()
    from
        dbo.Folders f
        inner join dbo.Folders d
                on d.UserId = f.UserId
               and d.Hid.IsDescendantOf(f.Hid) = 1
    where
        f.Id = @FolderId
    order by
        d.Hid;
end;
```

For example:

```sql
exec dbo.GetFolderWithSubFolders @FolderId = 40;
```

```text
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
40    13        3A    3      5AD6       /1/1/1/
121   40        4A    4      5AD6B0     /1/1/1/1/
364   121       5A    5      5AD6B580   /1/1/1/1/1/
365   121       5B    5      5AD6B680   /1/1/1/1/2/
366   121       5C    5      5AD6B780   /1/1/1/1/3/
122   40        4B    4      5AD6D0     /1/1/1/2/
367   122       5A    5      5AD6D580   /1/1/1/2/1/
368   122       5B    5      5AD6D680   /1/1/1/2/2/
369   122       5C    5      5AD6D780   /1/1/1/2/3/
123   40        4C    4      5AD6F0     /1/1/1/3/
370   123       5A    5      5AD6F580   /1/1/1/3/1/
371   123       5B    5      5AD6F680   /1/1/1/3/2/
372   123       5C    5      5AD6F780   /1/1/1/3/3/
```

### What is the Use of `HidCode`?

As stated above, `HidCode` is a binary representation of HierarchyId converted to varchar, with the leading `0x` stripped.

Here is the `dbo.GetHidCode` scalar function:

```sql
create function dbo.GetHidCode (@Hid hierarchyid)
returns varchar(1000)
as
begin
    return replace(
        convert(varchar(1000), cast(@Hid as varbinary(892)), 2),
        '0x', '');
end;
```

The `HidCode` string can be used on the client side to sort a flat list of Folders hierarchically — convenient when you want to keep the heavy lifting in SQL but ship a flat list to JavaScript and let the UI re-sort on the fly.

## Chapter for the Perfectionists

The above method of `Hid` calculation works well, but it has a tiny drawback.

```text
-- imagine you have two sibling folders
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
364   121       5A    5      5AD6B580   /1/1/1/1/1/
365   121       5B    5      5AD6B680   /1/1/1/1/2/

-- and you want to insert a new folder
Id    ParentId  Name
----- --------- -----
0     121       5Aaa

-- the result will be
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
364   121       5A    5      5AD6B580   /1/1/1/1/1/
1234  121       5Aaa  5      5AD6B62C   /1/1/1/1/1.1/
365   121       5B    5      5AD6B680   /1/1/1/1/2/
```

See this sequence in `HidPath`: `1 → 1.1 → 2` instead of `1 → 2 → 3`. In time, this ugly sequence may become uglier still.

If you are OK with that — skip this chapter. But for the rest of us, here is one more option for keeping the hierarchical data in a clean state.

The following stored procedure recalculates the HierarchyId so the path consists of integer numbers in a continuous sequence:

```sql
create procedure dbo.SaveFolderWithHidReculc
    @Folder dbo.FolderTableType readonly
as
begin
    set nocount on;

    begin -- variable declaration

        declare
            @ParentId        int            ,
            @UserId          int            ,
            @ParentHid       hierarchyid    ,
            @ParentHidStr    varchar(1000)  ,
            @StartTranCount  int            ,
            @OldParentId     int            ,
            @OldParentHid    hierarchyid    ;

        declare @FolderIds table (
            InsertedId    int          not null,
            OldParentId   int              null,
            OldParentHid  hierarchyid      null
        );
    end;

    begin try
        set @StartTranCount = @@trancount;
        if @StartTranCount = 0
        begin
            set transaction isolation level serializable;
            begin transaction;
        end;

        begin -- init variables and lock parent for update
            select @ParentId = ParentId from @Folder;

            select
                @UserId       = UserId,
                @ParentHid    = Hid,
                @ParentHidStr = cast(Hid as varchar(1000))
            from
                dbo.Folders
            where
                Id = @ParentId;
        end;

        begin -- merge calculated hierarchical data with existing folders
            merge into dbo.Folders as target
                using
                (
                    select
                        Hid       =  cast(concat(@ParentHidStr, -1, '/') as varchar(1000)),
                        Id        ,
                        ParentId  ,
                        Name
                    from
                        @Folder
                )
                as source on source.Id = target.Id

            when matched and target.UserId = @UserId then
                update set
                    ParentId = source.ParentId,
                    Name     = source.Name

            when not matched by target and source.Id = 0 then
                insert (UserId, Hid, ParentId, Name)
                values (@UserId, source.Hid, source.ParentId, source.Name)

            output
                inserted.Id,
                deleted.ParentId,
                deleted.Hid.GetAncestor(1)
            into @FolderIds (InsertedId, OldParentId, OldParentHid);
        end;

        begin -- recalculate sub-folder Hids
            select top 1
                @OldParentId  = OldParentId,
                @OldParentHid = OldParentHid
            from @FolderIds;

            exec dbo.ReculcSubFolderHids
                @UserId, @ParentId, @ParentHid, @OldParentId, @OldParentHid;
        end;

        if @StartTranCount = 0 commit transaction;
    end try
    begin catch
        if xact_state() <> 0 and @StartTranCount = 0 rollback transaction;

        declare @ErrorMessage nvarchar(4000) = dbo.GetErrorMessage();
        raiserror(@ErrorMessage, 16, 1);
        return;
    end catch;
end;
```

The trick is in the call to `ReculcSubFolderHids` after the `@Folder` save:

```sql
create procedure dbo.ReculcSubFolderHids
    @UserId        int,
    @ParentId      int,
    @ParentHid     hierarchyid,
    @OldParentId   int          = null,
    @OldParentHid  hierarchyid  = null
as
begin
    declare @ParentHidStr    varchar(1000) = cast(@ParentHid    as varchar(1000));
    declare @OldParentHidStr varchar(1000) = cast(@OldParentHid as varchar(1000));

    with Recursion as
    (
        select
            Id        ,
            ParentId  ,
            Name      ,
            OldHid    =  cast(Hid as varchar(1000)),
            NewHid    =  cast(
                            concat(
                                case when ParentId = @ParentId
                                     then @ParentHidStr
                                     else @OldParentHidStr
                                end,
                                row_number() over (order by Name, Id),
                                '/'
                            )
                            as varchar(1000)
                         )
        from
            dbo.Folders
        where
            ParentId in (@ParentId, @OldParentId)

        union all

        select
            Id        =  f.Id,
            ParentId  =  f.ParentId,
            Name      =  f.Name,
            OldHid    =  cast(f.Hid as varchar(1000)),
            NewHid    =  cast(
                             concat(
                                 r.NewHid,
                                 row_number() over (partition by f.ParentId order by f.Name, f.Id),
                                 '/'
                             )
                             as varchar(1000)
                         )
        from
            Recursion r
            inner join dbo.Folders f on f.ParentId = r.Id
        where
            r.OldHid <> r.NewHid
    )
    update f set
        Hid = r.NewHid
    from
        dbo.Folders f
        inner join Recursion r on r.Id = f.Id and f.Hid <> r.NewHid
    where
        f.UserId = @UserId;
end;
```

The above procedure produces paths consisting of integer numbers in a continuous sequence:

```text
-- Result of SaveFolderWithHidReculc on the previous example
Id    ParentId  Name  Level  HidCode    HidPath
----- --------- ----- ------ ---------- ------------
364   121       5A    5      5AD6B580   /1/1/1/1/1/
1234  121       5Aaa  5      5AD6B680   /1/1/1/1/2/
365   121       5B    5      5AD6B780   /1/1/1/1/3/
```

This method has a drawback too: it works well as long as the recalculated branch consists of up to about 10,000 nodes (calculation under one second). For 100,000 nodes the recalculation may take 15 seconds or more.

## Read a Tree in C#

The implemented Id-ParentId reference allows us to build a tree of Folders in C#. Moreover, once the Folders are sorted hierarchically, the tree can be built in a single pass through the flat list.

Suppose we have a `Folder` class:

```csharp
public class Folder
{
    public int             Id          { get; set; }
    public int?            ParentId    { get; set; }
    public string          Name        { get; set; } = null!;
    public IList<Folder>?  SubFolders  { get; set; }
}
```

The following method builds a tree in one pass through a hierarchically sorted Folder list, using a stack to track the current parent chain:

```csharp
public static IList<Folder> ConvertHierarchicallySortedFolderListToTrees(
    IEnumerable<Folder> folders)
{
    var parentStack = new Stack<Folder>();
    var parent      = default(Folder);
    var prevNode    = default(Folder);
    var rootNodes   = new List<Folder>();

    foreach (var folder in folders)
    {
        if (parent == null || folder.ParentId == null)
        {
            rootNodes.Add(folder);
            parent = folder;
        }
        else if (folder.ParentId == parent.Id)
        {
            parent.SubFolders ??= new List<Folder>();
            parent.SubFolders.Add(folder);
        }
        else if (folder.ParentId == prevNode.Id)
        {
            parentStack.Push(parent);
            parent = prevNode;
            parent.SubFolders ??= new List<Folder>();
            parent.SubFolders.Add(folder);
        }
        else
        {
            var parentFound = false;
            while (parentStack.Count > 0 && parentFound == false)
            {
                parent = parentStack.Pop();
                if (folder.ParentId != null && folder.ParentId.Value == parent.Id)
                {
                    parent.SubFolders!.Add(folder);
                    parentFound = true;
                }
            }
            if (parentFound == false)
            {
                rootNodes.Add(folder);
                parent = folder;
            }
        }

        prevNode = folder;
    }

    return rootNodes;
}
```

The above runs about twice as fast as the equivalent for an unsorted Folder list, which has to build a dictionary first:

```csharp
public static IList<Folder> ConvertHierarchicallyUnsortedFolderListToTrees(
    IEnumerable<Folder> folders)
{
    var dictionary  = folders.ToDictionary(n => n.Id, n => n);
    var rootFolders = new List<Folder>();

    foreach (var folder in dictionary.Select(item => item.Value))
    {
        if (folder.ParentId.HasValue
            && dictionary.TryGetValue(folder.ParentId.Value, out var parent))
        {
            parent.SubFolders ??= new List<Folder>();
            parent.SubFolders.Add(folder);
        }
        else
        {
            rootFolders.Add(folder);
        }
    }

    return rootFolders;
}
```

Both methods can build multiple roots, so the return type is `IList<Folder>`. That is useful when you need to fetch several branches detached from the tree, or several independent trees.

## Where This Lives in Artisan.Orm

The patterns above became part of the Artisan.Orm sample database and library:

- The `dbo.Folders` table and its companion procedures (`SaveFolder`, `SaveFolderWithHidReculc`, `ReculcSubFolderHids`, `GetFolderWithSubFolders`, `GenerateFolders`, etc.) live in the [`Database/`](../Database/) project.
- The C# repository that calls them — including the tree-building helpers — lives in [`Tests/DAL/Folders/Repository.cs`](../Tests/DAL/Folders/Repository.cs).
- Artisan.Orm's [`INode<T>`](https://github.com/lobodava/artisan-orm/wiki/INode-Interface-and-ToTree-Methods) interface generalises the in-memory tree-building algorithm shown above, so the two `ConvertHierarchically*FolderListToTrees` methods can be replaced by a single `repo.ReadToTree<Folder>(...)` call when `Folder` implements `INode<Folder>`.

The integration tests in [`Tests/Tests/FolderTests.cs`](../Tests/Tests/FolderTests.cs) demonstrate the full flow.

## About the Source Code

The original article shipped with a standalone Hierarchy solution containing two projects: a `Hierarchy.DB` SSDT project and a `Hierarchy.Tests` test project. That code has been folded into the Artisan.Orm repository — see the links in the previous section.

The `Database/` project contains a `dbo.GenerateFolders` stored procedure which generates hierarchical data for tests; the generation runs automatically during the database publish phase, so as soon as you publish the schema you have a populated `dbo.Folders` table to play with.

## See Also

- Wiki page: [INode Interface and ToTree Methods](https://github.com/lobodava/artisan-orm/wiki/INode-Interface-and-ToTree-Methods) — how to consume hierarchical data through Artisan.Orm's high-level API.
- Other articles in this series: [`README.md`](README.md).

---

*Originally published as [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](https://www.codeproject.com/Articles/1192607/Combination-of-Id-ParentId-and-HierarchyId) on CodeProject, 17 July 2017. CodeProject went read-only in late 2024; this version preserves the article for posterity. Re-published under [CC-BY-SA-4.0](LICENSE).*
