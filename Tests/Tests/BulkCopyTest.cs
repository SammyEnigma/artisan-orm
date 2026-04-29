using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Records;
using Tests.DAL.Records.Models;

namespace Tests.Tests
{
	[TestClass]
	public class BulkCopyTest
	{
		private Repository _repository = null!;
		private string _connectionString = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();
			_connectionString = appSettings.ConnectionStrings.DatabaseConnection;
			_repository = new Repository(_connectionString);

			// Clean target table before every test to make assertions deterministic.
			_repository.TruncateRecordsBulk();
		}

		[TestCleanup]
		public void TestCleanup() => _repository.Dispose();


		private static IList<Record> MakeRows(int count, int idStart = 1) => Enumerable.Range(0, count)
			.Select(i => new Record
			{
				Id            = idStart + i,
				GrandRecordId = 1,
				Name          = $"BulkRow-{idStart + i}",
				RecordTypeId  = (byte)(i % 3 + 1),
				Number        = (short)(i * 10),
				Date          = new DateTime(2026, 4, 29).AddDays(i),
				Amount        = 100m + i,
				IsActive      = i % 2 == 0,
				Comment       = $"comment {i}"
			}).ToList();


		[TestMethod]
		public void BulkCopy_RegisteredMapper_InsertsAllRows()
		{
			var rows  = MakeRows(50);
			var count = _repository.BulkInsertRecords(rows);

			Assert.AreEqual(50, count);
			Assert.AreEqual(50, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public void BulkCopyAs_AutoMapping_InsertsAllRows()
		{
			var rows  = MakeRows(30);
			var count = _repository.BulkInsertRecordsAs(rows);

			Assert.AreEqual(30, count);
			Assert.AreEqual(30, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public async Task BulkCopyAsync_RegisteredMapper()
		{
			var rows  = MakeRows(75);
			var count = await _repository.BulkInsertRecordsAsync(rows);

			Assert.AreEqual(75, count);
			Assert.AreEqual(75, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public async Task BulkCopyAsAsync_AutoMapping()
		{
			var rows  = MakeRows(20);
			var count = await _repository.BulkInsertRecordsAsAsync(rows);

			Assert.AreEqual(20, count);
			Assert.AreEqual(20, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public void BulkCopy_InsideTransaction_RolledBackOnException_LeavesNoRows()
		{
			Assert.ThrowsExactly<InvalidOperationException>(() =>
			{
				_repository.RunInTransaction(tran =>
				{
					_repository.BulkInsertRecords(MakeRows(10));
					Assert.AreEqual(10, _repository.CountRecordsBulk());

					// Force rollback — RunInTransaction rolls back when the action throws.
					throw new InvalidOperationException("force rollback");
				});
			});

			Assert.AreEqual(0, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public void BulkCopy_InsideTransaction_Committed_PersistsRows()
		{
			_repository.RunInTransaction(tran =>
			{
				_repository.BulkInsertRecords(MakeRows(7));
			});

			Assert.AreEqual(7, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public void BulkCopy_OnRawConnection_PassesThrough()
		{
			using var conn = new SqlConnection(_connectionString);
			var rows = MakeRows(15);

			var count = conn.BulkCopy(rows, "dbo.RecordsBulk");

			Assert.AreEqual(15, count);
			Assert.AreEqual(ConnectionState.Closed, conn.State);   // closed back after the call
			Assert.AreEqual(15, _repository.CountRecordsBulk());
		}

		[TestMethod]
		public async Task BulkCopyAsync_OnRawConnection_WithCancellationToken()
		{
			using var conn = new SqlConnection(_connectionString);
			using var cts  = new CancellationTokenSource();

			var rows  = MakeRows(8);
			var count = await conn.BulkCopyAsync(rows, "dbo.RecordsBulk", cts.Token);

			Assert.AreEqual(8, count);
			Assert.AreEqual(ConnectionState.Closed, conn.State);
		}
	}
}
