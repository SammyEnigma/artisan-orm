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


		// SQL Server forbids "return <value>" in ad-hoc batches — only stored procedures
		// can set @ReturnValue. So these tests just verify the new overload is reachable
		// and runs end-to-end through the cancellation-token-aware code path.

		[TestMethod]
		public async Task ExecuteAsync_WithCancellationToken_NoParams_ExecutesAdHocSql()
		{
			using var cts = new CancellationTokenSource();

			// No-op delete: SQL parses, runs, removes 0 rows.
			await _repository.ExecuteAsync(
				"delete from dbo.RecordsBulk where Id = -999;",
				cts.Token);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCancellationToken_AndParams_BindsParameters()
		{
			using var cts = new CancellationTokenSource();

			await _repository.ExecuteAsync(
				"delete from dbo.RecordsBulk where Id = @Id;",
				cts.Token,
				new SqlParameter("@Id", -999));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCancellationToken_OnStoredProc_ReturnsValue()
		{
			// dbo.DeleteUser returns 2 when the user is missing; pick a nonexistent id
			// so the proc returns without mutating data.
			using var cts = new CancellationTokenSource();

			var returnValue = await _repository.ExecuteAsync(
				"dbo.DeleteUser",
				cts.Token,
				new SqlParameter("@UserId", -999));

			Assert.AreEqual(2, returnValue);
		}

		[TestMethod]
		public async Task ExecuteAsync_NoCancellation_ParamsOverload_StillWorks()
		{
			// The existing overload (sql, params SqlParameter[]) — keeps working with the
			// new ConfigureAwait(false) wiring.
			var returnValue = await _repository.ExecuteAsync(
				"dbo.DeleteUser",
				new SqlParameter("@UserId", -999));

			Assert.AreEqual(2, returnValue);
		}
	}
}
