-- Returns two result sets: all Records, then all GrandRecords.
-- Used by ReadToLists<T1, T2> tests.
create procedure dbo.GetRecordsAndGrandRecords
as
begin
	set nocount on;

	select * from dbo.vwRecords		order by Id;

	select * from dbo.vwGrandRecords order by Id;
end;
