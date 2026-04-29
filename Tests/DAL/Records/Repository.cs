using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Artisan.Orm;
using Tests.DAL.Records.Models;
using GrandRecord = Tests.DAL.GrandRecords.Models.GrandRecord;
using ChildRecord = Tests.DAL.GrandRecords.Models.ChildRecord;
using RecordType  = Tests.DAL.GrandRecords.Models.RecordType;

namespace Tests.DAL.Records
{
	public class Repository: RepositoryBase
	{
		public Repository(string connectionString) : base(connectionString) { }


		#region [ GetRecordById ]


		public Record GetRecordById(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("Id", id);

				return cmd.ReadTo<Record>();
			});
		}
		
		public Record GetRecordByIdWithAutoMapping(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("Id", id);

				return cmd.ReadAs<Record>();
			});
		}

		public Record GetRecordByIdOnBaseLevel(int id)
		{
			//var sql = @"select
			//				Id				,
			//				GrandRecordId	,
			//				Name			,
			//				RecordTypeId	,
			//				Number			,
			//				[Date]			,
			//				Amount			,
			//				IsActive		,
			//				Comment			
			//			from
			//				dbo.Records
			//			where
			//				Id = @Id";

			var sql = "dbo.GetRecordById";

			return ReadTo<Record>(sql, new SqlParameter("Id", id));
		}
		

		public async Task<Record> GetRecordByIdAsync(int id)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.ReadToAsync<Record>();
			});
		}


		#endregion 


		#region [ GetRecords ]


		public IList<Record> GetRecords()
		{
			return ReadToList<Record>("dbo.GetRecords");

			//return GetByCommand(cmd =>
			//{
			//	cmd.UseProcedure("dbo.GetRecords");

			//	return cmd.ReadToList<Record>();
			//});
		}

		public IList<Record> GetRecordsWithAutoMapping()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadAsList<Record>();
			});
		}

		public async Task<IList<Record>> GetRecordsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToListAsync<Record>();
			});
		}

		public async Task<IList<Record>> GetRecordsWithAutoMappingAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadAsListAsync<Record>();
			});
		}

		public IEnumerable<Record> GetRecordsToEnumerable()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToEnumerable<Record>();
			});
		}

		public IEnumerable<Record> GetRecordsAsEnumerable()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadAsEnumerable<Record>();
			});
		}


		public IEnumerable<Record> GetRecordsToEnumerableOnBaseLevel()
		{
			return ReadToEnumerable<Record>("dbo.GetRecords");
		}

		public IEnumerable<Record> GetRecordsAsEnumerableOnBaseLevel()
		{
			return ReadAsEnumerable<Record>("dbo.GetRecords");
		}


		public IList<Record> GetRecordsOnBaseLevel()
		{
			return ReadToList<Record>("dbo.GetRecords");
		}


		#endregion

		#region [ GetRecordRows ]


		public ObjectRows GetRecordRows()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRows<Record>();
			});
		}

		public async Task<ObjectRows> GetRecordRowsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRowsAsync<Record>();
			});
		}

		public ObjectRows GetRecordRowsOnBaseLevel()
		{
			return ReadToObjectRows<Record>("dbo.GetRecords");
		}

		public ObjectRows GetRecordRowsWithHandMapping()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRows(dr => new ObjectRow(9) 
				{
					/* Id			 */	dr.GetInt32(0)				,
					/* GrandRecordId */	dr.GetInt32(1)				,
					/* Name			 */	dr.GetString(2)				,
					/* RecordTypeId	 */	dr.GetByteNullable(3)		,
					/* Number		 */	dr.GetInt16Nullable(4)		,
					/* Date			 */	dr.GetDateTimeNullable(5)	,
					/* Amount		 */	dr.GetDecimalNullable(6)	,
					/* IsActive		 */	dr.GetBooleanNullable(7)	,
					/* Comment		 */	dr.GetStringNullable(8)	
				});
			});
		}

		public async Task<ObjectRows> GetRecordRowsWithHandMappingAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRowsAsync(dr => new ObjectRow(9) 
				{
					/* Id			 */	dr.GetInt32(0)				,
					/* GrandRecordId */	dr.GetInt32(1)				,
					/* Name			 */	dr.GetString(2)				,
					/* RecordTypeId	 */	dr.GetByteNullable(3)		,
					/* Number		 */	dr.GetInt16Nullable(4)		,
					/* Date			 */	dr.GetDateTimeNullable(5)	,
					/* Amount		 */	dr.GetDecimalNullable(6)	,
					/* IsActive		 */	dr.GetBooleanNullable(7)	,
					/* Comment		 */	dr.GetStringNullable(8)	
				});
			});
		}

		#endregion


		#region [ Save ONE Record ]

		public Record SaveRecord(Record record)
		{	
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableRowParam("@Records", record);

				return cmd.ReadTo<Record>();
			});
		}

		public async Task<Record> SaveRecordAsync(Record record)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableRowParam("@Records", record);

				return cmd.ReadToAsync<Record>();
			});
		}


		#endregion

		#region [ Save MANY Records ]


		public IList<Record> SaveRecords(IList<Record> records)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableParam("@Records", records);

				return cmd.ReadToList<Record>();
			});
		}

		public async Task<IList<Record>> SaveRecordsAsync(IList<Record> records)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableParam("@Records", records.ToDataTable());

				return cmd.ReadToListAsync<Record>();
			});
		}


		#endregion


		#region [ BulkCopy — exercises Connection.BulkCopy / RepositoryBase.BulkCopy* ]

		public int BulkInsertRecords(IList<Record> records)
		{
			return BulkCopy(records, "dbo.RecordsBulk");
		}

		public int BulkInsertRecordsAs(IList<Record> records)
		{
			return BulkCopyAs(records, "dbo.RecordsBulk");
		}

		public Task<int> BulkInsertRecordsAsync(IList<Record> records, CancellationToken ct = default)
		{
			return BulkCopyAsync(records, "dbo.RecordsBulk", ct);
		}

		public Task<int> BulkInsertRecordsAsAsync(IList<Record> records, CancellationToken ct = default)
		{
			return BulkCopyAsAsync(records, "dbo.RecordsBulk", ct);
		}

		public int TruncateRecordsBulk()
		{
			return Execute("truncate table dbo.RecordsBulk");
		}

		public int CountRecordsBulk()
		{
			return ReadTo<int>("select count(*) from dbo.RecordsBulk");
		}

		#endregion


		#region [ ReadToLists — exercises ReadToLists<T1, T2[, T3[, T4]]> ]

		public (IList<Record> records, IList<GrandRecord> grandRecords) GetRecordsAndGrandRecords()
		{
			return ReadToLists<Record, GrandRecord>("dbo.GetRecordsAndGrandRecords");
		}

		public Task<(IList<Record>, IList<GrandRecord>)> GetRecordsAndGrandRecordsAsync(CancellationToken ct = default)
		{
			return ReadToListsAsync<Record, GrandRecord>("dbo.GetRecordsAndGrandRecords", ct);
		}

		public (IList<GrandRecord>, IList<Record>, IList<RecordType>) GetThreeListsOfRecords()
		{
			return ReadToLists<GrandRecord, Record, RecordType>("dbo.GetThreeListsOfRecords");
		}

		public Task<(IList<GrandRecord>, IList<Record>, IList<RecordType>)> GetThreeListsOfRecordsAsync(CancellationToken ct = default)
		{
			return ReadToListsAsync<GrandRecord, Record, RecordType>("dbo.GetThreeListsOfRecords", ct);
		}

		public (IList<GrandRecord>, IList<Record>, IList<ChildRecord>, IList<RecordType>) GetFourListsOfRecords()
		{
			return ReadToLists<GrandRecord, Record, ChildRecord, RecordType>("dbo.GetFourListsOfRecords");
		}

		public Task<(IList<GrandRecord>, IList<Record>, IList<ChildRecord>, IList<RecordType>)> GetFourListsOfRecordsAsync(CancellationToken ct = default)
		{
			return ReadToListsAsync<GrandRecord, Record, ChildRecord, RecordType>("dbo.GetFourListsOfRecords", ct);
		}

		#endregion

	}
}
