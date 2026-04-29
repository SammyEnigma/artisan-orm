#if NET6_0_OR_GREATER

using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class JsonParamTest
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


		private record JsonPayload(int Id, string Name, bool IsActive, decimal Amount);

		[TestMethod]
		public void AddJsonParam_RecordTypeRoundTrip()
		{
			var payload = new JsonPayload(Id: 42, Name: "Test", IsActive: true, Amount: 1234.5678m);

			var (id, name, isActive, amount) = _repository.GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetJsonParam");
				cmd.AddJsonParam("@Json", payload);

				return cmd.ReadTo(r => (
					Id:       r.GetInt32(0),
					Name:     r.GetString(1),
					IsActive: r.GetBoolean(2),
					Amount:   r.GetDecimal(3)));
			});

			Assert.AreEqual(payload.Id,       id);
			Assert.AreEqual(payload.Name,     name);
			Assert.AreEqual(payload.IsActive, isActive);
			Assert.AreEqual(payload.Amount,   amount);
		}

		[TestMethod]
		public void AddJsonParam_NullValue_PassesAsDbNull()
		{
			JsonPayload payload = null;

			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select case when @Json is null then 1 else 0 end;");
				cmd.AddJsonParam("@Json", payload);

				Assert.AreEqual(1, cmd.ReadTo<int>());
			});
		}

		[TestMethod]
		public void AddJsonParam_AnonymousType()
		{
			var (id, name) = _repository.GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetJsonParam");
				cmd.AddJsonParam("@Json", new { Id = 7, Name = "Anon", IsActive = false, Amount = 0m });

				return cmd.ReadTo(r => (Id: r.GetInt32(0), Name: r.GetString(1)));
			});

			Assert.AreEqual(7,       id);
			Assert.AreEqual("Anon",  name);
		}
	}
}

#endif
