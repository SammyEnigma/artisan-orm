create table dbo.RecordsBulk
(
	Id				int				not null	,
	GrandRecordId	int					null	,
	[Name]			varchar(30)		not null	,
	RecordTypeId	tinyint				null	,
	Number			smallint			null	,
	[Date]			datetime2(0)		null	,
	Amount			decimal(19,2)		null	,
	IsActive		bit					null	,
	Comment			nvarchar(500)		null	,

	constraint PK_RecordsBulk primary key clustered (Id)
);
