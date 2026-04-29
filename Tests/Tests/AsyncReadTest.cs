using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Records;
using Tests.DAL.Records.Models;

namespace Tests.Tests
{
	[TestClass]
	public class AsyncReadTest
	{
		private Repository _repository = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();
			_repository = new Repository(appSettings.ConnectionStrings.DatabaseConnection);
		}

		[TestCleanup]
		public void TestCleanup() => _repository.Dispose();


		[TestMethod]
		public async Task ReadToArrayAsync_RegisteredMapper()
		{
			var array = await _repository.ReadToArrayAsync<Record>("dbo.GetRecords");

			Assert.IsNotNull(array);
			Assert.IsTrue(array.Length > 0);
			Assert.IsNotNull(array[0].Name);
		}

		[TestMethod]
		public async Task ReadToArrayAsync_WithCancellationToken()
		{
			using var cts = new CancellationTokenSource();
			var array = await _repository.ReadToArrayAsync<Record>("dbo.GetRecords", cts.Token);

			Assert.IsTrue(array.Length > 0);
		}

		[TestMethod]
		public async Task ReadAsArrayAsync_AutoMapping()
		{
			var array = await _repository.ReadAsArrayAsync<Record>("dbo.GetRecords");

			Assert.IsNotNull(array);
			Assert.IsTrue(array.Length > 0);
		}

		[TestMethod]
		public async Task ReadDynamicAsync_SingleRow()
		{
			var record = await _repository.ReadDynamicAsync(
				"select top 1 Id, Name from dbo.Records order by Id");

			Assert.IsNotNull(record);
			Assert.IsTrue(((int)record!.Id) > 0);
			Assert.IsNotNull((string)record.Name);
		}

		[TestMethod]
		public async Task ReadDynamicAsync_NoRows_ReturnsNull()
		{
			var record = await _repository.ReadDynamicAsync(
				"select Id, Name from dbo.Records where Id = -999");

			Assert.IsNull(record);
		}

		[TestMethod]
		public async Task ReadDynamicListAsync_AllRecords()
		{
			var list = await _repository.ReadDynamicListAsync(
				"select Id, Name from dbo.Records");

			Assert.IsNotNull(list);
			Assert.IsTrue(list.Count > 0);
			Assert.IsTrue(((int)list[0].Id) > 0);
		}

		[TestMethod]
		public async Task ReadDynamicArrayAsync_AllRecords()
		{
			var array = await _repository.ReadDynamicArrayAsync(
				"select Id, Name from dbo.Records");

			Assert.IsNotNull(array);
			Assert.IsTrue(array.Length > 0);
		}

		[TestMethod]
		public async Task ReadToObjectRowAsync_WithCreateFunc()
		{
			var row = await _repository.ReadToObjectRowAsync(
				"select top 1 Id, Name from dbo.Records order by Id",
				dr => new ObjectRow(2)
				{
					/* 0 - Id   */ dr.GetInt32(0),
					/* 1 - Name */ dr.GetString(1),
				});

			Assert.IsNotNull(row);
			Assert.IsTrue((int)row![0]! > 0);
			Assert.IsNotNull(row[1]);
		}

		[TestMethod]
		public async Task ReadToObjectRowsAsync_WithCreateFunc()
		{
			var rows = await _repository.ReadToObjectRowsAsync(
				"select Id, Name from dbo.Records",
				dr => new ObjectRow(2)
				{
					dr.GetInt32(0),
					dr.GetString(1),
				});

			Assert.IsNotNull(rows);
			Assert.IsTrue(rows.Count > 0);
		}

		[TestMethod]
		public async Task ReadToObjectRowAsync_GenericMapper()
		{
			var row = await _repository.ReadToObjectRowAsync<Record>(
				"select top 1 * from dbo.vwRecords order by Id");

			Assert.IsNotNull(row);
		}

		[TestMethod]
		public async Task ReadToObjectRowsAsync_GenericMapper()
		{
			var rows = await _repository.ReadToObjectRowsAsync<Record>(
				"select * from dbo.vwRecords");

			Assert.IsNotNull(rows);
			Assert.IsTrue(rows.Count > 0);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCancellationToken_AndParams()
		{
			// Exercise the new ExecuteAsync(sql, ct, params) overload.
			using var cts = new CancellationTokenSource();

			var ret = await _repository.ExecuteAsync(
				"select 1; return 42;",
				cts.Token);

			Assert.AreEqual(42, ret);
		}
	}
}
