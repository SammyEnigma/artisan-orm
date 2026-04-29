-- Returns three result sets: GrandRecords, Records, RecordTypes.
-- Used by ReadToLists<T1, T2, T3> tests.
create procedure dbo.GetThreeListsOfRecords
as
begin
	set nocount on;

	select * from dbo.vwGrandRecords	order by Id;

	select * from dbo.vwRecords			order by Id;

	select Id, Code, [Name] from dbo.RecordTypes order by Id;
end;
