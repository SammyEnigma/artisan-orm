-- Echoes back four parameters bound from .NET DateOnly / TimeOnly types
-- (NET6+) so tests can assert AddDateParam(DateOnly) / AddTimeParam(TimeOnly)
-- and the matching GetDateOnly / GetTimeOnly round-trip.
create procedure dbo.GetDateOnlyTimeOnlyParams
	@Date			date		,
	@DateNullable	date		,
	@Time			time(7)		,
	@TimeNullable	time(7)
as
begin
	set nocount on;

	select
		@Date			,
		@DateNullable	,
		@Time			,
		@TimeNullable	;
end;
