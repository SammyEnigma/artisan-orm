-- Sets every supported output parameter so unit tests can assert
-- that Add*OutputParam round-trips correctly through Microsoft.Data.SqlClient.
create procedure dbo.GetOutputParams
	@Bit				bit					output,
	@TinyInt			tinyint				output,
	@SmallInt			smallint			output,
	@Int				int					output,
	@BigInt				bigint				output,
	@Decimal			decimal(19,4)		output,
	@Guid				uniqueidentifier	output,
	@DateTime2			datetime2(7)		output,
	@DateTimeOffset		datetimeoffset(7)	output
as
begin
	set nocount on;

	set @Bit			= 1;
	set @TinyInt		= 200;
	set @SmallInt		= 12345;
	set @Int			= 1234567890;
	set @BigInt			= 1234567890123456789;
	set @Decimal		= 9999.1234;
	set @Guid			= '11111111-2222-3333-4444-555555555555';
	set @DateTime2		= '2026-04-29T12:34:56.7654321';
	set @DateTimeOffset	= '2026-04-29T12:34:56.7654321+03:00';
end;
