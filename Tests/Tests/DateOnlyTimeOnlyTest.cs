#if NET6_0_OR_GREATER

using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class DateOnlyTimeOnlyTest
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
		public void DateOnly_TimeOnly_RoundTripThroughProc()
		{
			var date         = new DateOnly(2026, 4, 29);
			var dateNullable = (DateOnly?)null;
			var time         = new TimeOnly(13, 45, 30);
			var timeNullable = (TimeOnly?)new TimeOnly(0, 0, 0);

			_repository.RunCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetDateOnlyTimeOnlyParams");

				cmd.AddDateParam("@Date",         date);
				cmd.AddDateParam("@DateNullable", dateNullable);
				cmd.AddTimeParam("@Time",         time);
				cmd.AddTimeParam("@TimeNullable", timeNullable);

				cmd.GetByReader(dr =>
				{
					Assert.IsTrue(dr.Read());

					Assert.AreEqual(date,                        dr.GetDateOnly(0));
					Assert.IsNull  (                             dr.GetDateOnlyNullable(1));
					Assert.AreEqual(time,                        dr.GetTimeOnly(2));
					Assert.AreEqual(timeNullable,                dr.GetTimeOnlyNullable(3));

					return 0;
				});
			});
		}

		[TestMethod]
		public void DateOnly_TimeOnly_DefaultValueOverloads()
		{
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast(null as date), cast(null as time);");

				cmd.GetByReader(dr =>
				{
					Assert.IsTrue(dr.Read());

					var dateDefault = new DateOnly(2000, 1, 1);
					var timeDefault = new TimeOnly(9, 0, 0);

					Assert.AreEqual(dateDefault, dr.GetDateOnly(0, defaultValue: dateDefault));
					Assert.AreEqual(timeDefault, dr.GetTimeOnly(1, defaultValue: timeDefault));

					return 0;
				});
			});
		}
	}
}

#endif
