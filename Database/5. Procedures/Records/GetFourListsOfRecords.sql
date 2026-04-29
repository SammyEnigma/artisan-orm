-- Returns four result sets: GrandRecords, Records, ChildRecords, RecordTypes.
-- Used by ReadToLists<T1, T2, T3, T4> tests.
create procedure dbo.GetFourListsOfRecords
as
begin
	set nocount on;

	select * from dbo.vwGrandRecords		order by Id;

	select * from dbo.vwRecords				order by Id;

	select Id, RecordId, [Name] from dbo.ChildRecords order by Id;

	select Id, Code, [Name] from dbo.RecordTypes order by Id;
end;
