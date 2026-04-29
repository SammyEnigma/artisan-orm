using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class ExecuteAsyncTest
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
		public async Task ExecuteAsync_WithCancellationToken_NoParams()
		{
			using var cts = new CancellationTokenSource();

			var ret = await _repository.ExecuteAsync("select 1; return 7;", cts.Token);

			Assert.AreEqual(7, ret);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCancellationToken_AndParams()
		{
			using var cts = new CancellationTokenSource();

			var ret = await _repository.ExecuteAsync(
				"if @P = 5 return 100; return 0;",
				cts.Token,
				new SqlParameter("@P", 5));

			Assert.AreEqual(100, ret);
		}

		[TestMethod]
		public async Task ExecuteAsync_NoCancellation_PathStillWorks()
		{
			// Pre-existing overload that takes (sql, params SqlParameter[]).
			var ret = await _repository.ExecuteAsync(
				"if @P = 1 return 11; return 0;",
				new SqlParameter("@P", 1));

			Assert.AreEqual(11, ret);
		}
	}
}
