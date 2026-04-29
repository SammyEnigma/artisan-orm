-- Accepts a JSON document (sent via AddJsonParam<T>) and projects its top-level
-- properties as a single result row, so tests can verify that .NET object ->
-- JSON -> SQL columns round-tripping works end to end.
create procedure dbo.GetJsonParam
	@Json	nvarchar(max)
as
begin
	set nocount on;

	select
		Id		,
		[Name]	,
		IsActive,
		Amount
	from openjson(@Json)
	with
	(
		Id			int				,
		[Name]		nvarchar(100)	,
		IsActive	bit				,
		Amount		decimal(19,4)
	);
end;
