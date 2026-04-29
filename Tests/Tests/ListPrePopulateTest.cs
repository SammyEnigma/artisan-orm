using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Records.Models;

namespace Tests.Tests
{
	[TestClass]
	public class ListPrePopulateTest
	{
		private RepositoryBase _repository = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();
			_repository = new RepositoryBase(appSettings.ConnectionStrings.DatabaseConnection);
		}

		[TestCleanup]
		public void TestCleanup() => _repository.Dispose();


		[TestMethod]
		public void ReadToList_PrePopulatedList_AppendsRows()
		{
			var seed = new List<Record>
			{
				new() { Id = -1, Name = "preexisting" }
			};

			IList<Record> result = _repository.ReadToList<Record>("dbo.GetRecords", seed);

			Assert.AreSame(seed, result);
			Assert.IsTrue(result.Count > 1, "ReadToList should append fetched rows to the supplied list.");
			Assert.AreEqual(-1, result[0].Id);
		}

		[TestMethod]
		public void ReadAsList_PrePopulatedList_AppendsRows()
		{
			var seed = new List<Record>();

			IList<Record> result = _repository.ReadAsList<Record>(
				"select * from dbo.vwRecords",
				seed);

			Assert.AreSame(seed, result);
			Assert.IsTrue(result.Count > 0);
		}

		[TestMethod]
		public async Task ReadToListAsync_PrePopulatedList_AppendsRows()
		{
			var seed = new List<Record>
			{
				new() { Id = -2, Name = "preexisting-async" }
			};

			IList<Record> result = await _repository.ReadToListAsync<Record>("dbo.GetRecords", seed);

			Assert.AreSame(seed, result);
			Assert.IsTrue(result.Count > 1);
			Assert.AreEqual(-2, result[0].Id);
		}

		[TestMethod]
		public async Task ReadAsListAsync_PrePopulatedList_WithAction()
		{
			var seed = new List<Record>();

			IList<Record> result = await _repository.ReadAsListAsync<Record>(
				"dbo.GetRecords",
				seed,
				cmd => { /* no extra params */ });

			Assert.AreSame(seed, result);
			Assert.IsTrue(result.Count > 0);
		}

		[TestMethod]
		public void ReadToList_NullList_AllocatesNewList()
		{
			IList<Record> seed = null;

			IList<Record> result = _repository.ReadToList<Record>("dbo.GetRecords", seed);

			Assert.IsNotNull(result);
			Assert.IsTrue(result.Count > 0);
		}
	}
}
