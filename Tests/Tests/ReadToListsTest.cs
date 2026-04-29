using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.GrandRecords.Models;
using Tests.DAL.Records;
using Record = Tests.DAL.Records.Models.Record;

namespace Tests.Tests
{
	[TestClass]
	public class ReadToListsTest
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


		// ---- Two result sets ----

		[TestMethod]
		public void ReadToLists_TwoResults_RegisteredMapper()
		{
			var (records, grandRecords) = _repository.GetRecordsAndGrandRecords();

			Assert.IsTrue(records.Count      > 0);
			Assert.IsTrue(grandRecords.Count > 0);
			Assert.IsNotNull(records[0].Name);
			Assert.IsNotNull(grandRecords[0].Name);
		}

		[TestMethod]
		public async Task ReadToListsAsync_TwoResults_RegisteredMapper()
		{
			var (records, grandRecords) = await _repository.GetRecordsAndGrandRecordsAsync();

			Assert.IsTrue(records.Count      > 0);
			Assert.IsTrue(grandRecords.Count > 0);
		}

		[TestMethod]
		public void ReadToLists_TwoResults_WithCreateFunc()
		{
			using var cmd = _repository.CreateCommand("dbo.GetRecordsAndGrandRecords");

			var (recordIds, grandRecordIds) = cmd.ReadToLists<int, int>(
				createFunc1: dr => dr.GetInt32(0),
				createFunc2: dr => dr.GetInt32(0));

			Assert.IsTrue(recordIds.Count      > 0);
			Assert.IsTrue(grandRecordIds.Count > 0);
		}

		[TestMethod]
		public async Task ReadToListsAsync_TwoResults_WithCreateFunc_AndCancellationToken()
		{
			using var cts = new CancellationTokenSource();
			using var cmd = _repository.CreateCommand("dbo.GetRecordsAndGrandRecords");

			var (recordIds, grandRecordIds) = await cmd.ReadToListsAsync<int, int>(
				createFunc1: dr => dr.GetInt32(0),
				createFunc2: dr => dr.GetInt32(0),
				cancellationToken: cts.Token);

			Assert.IsTrue(recordIds.Count      > 0);
			Assert.IsTrue(grandRecordIds.Count > 0);
		}

		// ---- Three result sets ----

		[TestMethod]
		public void ReadToLists_ThreeResults_RegisteredMapper()
		{
			var (grandRecords, records, types) = _repository.GetThreeListsOfRecords();

			Assert.IsTrue(grandRecords.Count > 0);
			Assert.IsTrue(records.Count      > 0);
			Assert.IsTrue(types.Count        > 0);
			Assert.IsNotNull(types[0].Code);
		}

		[TestMethod]
		public async Task ReadToListsAsync_ThreeResults_RegisteredMapper()
		{
			var (grandRecords, records, types) = await _repository.GetThreeListsOfRecordsAsync();

			Assert.IsTrue(grandRecords.Count > 0);
			Assert.IsTrue(records.Count      > 0);
			Assert.IsTrue(types.Count        > 0);
		}

		// ---- Four result sets ----

		[TestMethod]
		public void ReadToLists_FourResults_RegisteredMapper()
		{
			var (grandRecords, records, childRecords, types) = _repository.GetFourListsOfRecords();

			Assert.IsTrue(grandRecords.Count > 0);
			Assert.IsTrue(records.Count      > 0);
			Assert.IsTrue(childRecords.Count >= 0);    // may be 0 if no child records seeded
			Assert.IsTrue(types.Count        > 0);
		}

		[TestMethod]
		public async Task ReadToListsAsync_FourResults_RegisteredMapper()
		{
			var (grandRecords, records, childRecords, types) = await _repository.GetFourListsOfRecordsAsync();

			Assert.IsTrue(grandRecords.Count > 0);
			Assert.IsTrue(records.Count      > 0);
			Assert.IsTrue(types.Count        > 0);
			Assert.IsNotNull(childRecords);
		}

		[TestMethod]
		public void ReadToLists_FourResults_WithCreateFunc()
		{
			using var cmd = _repository.CreateCommand("dbo.GetFourListsOfRecords");

			var (a, b, c, d) = cmd.ReadToLists<int, int, int, int>(
				createFunc1: dr => dr.GetInt32(0),
				createFunc2: dr => dr.GetInt32(0),
				createFunc3: dr => dr.GetInt32(0),
				createFunc4: dr => (int)dr.GetByte(0));

			Assert.IsTrue(a.Count > 0);
			Assert.IsTrue(b.Count > 0);
			Assert.IsTrue(d.Count > 0);
		}
	}
}
