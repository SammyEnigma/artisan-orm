using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class GetWithDefaultValueTest
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
		public void Get_NullColumns_ReturnDefaultValues()
		{
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql(@"select
					cast(null as tinyint),  cast(null as smallint), cast(null as int),    cast(null as bigint),
					cast(null as real),     cast(null as float),    cast(null as decimal(9,4)),
					cast(null as varchar(10)), cast(null as datetime2), cast(null as datetimeoffset),
					cast(null as time);");

				cmd.GetByReader(dr =>
				{
					Assert.IsTrue(dr.Read());

					Assert.AreEqual((byte)9,                            dr.GetByte(0,            defaultValue: (byte)9));
					Assert.AreEqual((short)-1,                          dr.GetInt16(1,           defaultValue: (short)-1));
					Assert.AreEqual(42,                                 dr.GetInt32(2,           defaultValue: 42));
					Assert.AreEqual(123456789012L,                      dr.GetInt64(3,           defaultValue: 123456789012L));
					Assert.AreEqual(3.14f,                              dr.GetFloat(4,           defaultValue: 3.14f));
					Assert.AreEqual(2.71828d,                           dr.GetDouble(5,          defaultValue: 2.71828d));
					Assert.AreEqual(7.77m,                              dr.GetDecimal(6,         defaultValue: 7.77m));
					Assert.AreEqual("fallback",                         dr.GetString(7,          defaultValue: "fallback"));

					var dtDefault = new DateTime(2000, 1, 1);
					Assert.AreEqual(dtDefault,                          dr.GetDateTime(8,        defaultValue: dtDefault));

					var dtoDefault = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
					Assert.AreEqual(dtoDefault,                         dr.GetDateTimeOffset(9,  defaultValue: dtoDefault));

					Assert.AreEqual(TimeSpan.FromHours(1),              dr.GetTimeSpan(10,       defaultValue: TimeSpan.FromHours(1)));

					return 0;
				});
			});
		}

		[TestMethod]
		public void Get_NonNullColumns_ReturnActualValues()
		{
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql(@"select
					cast(5 as tinyint),  cast(-100 as smallint), cast(2025 as int),    cast(9000000000 as bigint),
					cast(1.5 as real),   cast(2.5 as float),     cast(99.9999 as decimal(9,4)),
					cast('actual' as varchar(10)),
					cast('2026-01-15' as datetime2);");

				cmd.GetByReader(dr =>
				{
					Assert.IsTrue(dr.Read());

					Assert.AreEqual((byte)5,                            dr.GetByte(0,    defaultValue: (byte)9));
					Assert.AreEqual((short)-100,                        dr.GetInt16(1,   defaultValue: (short)-1));
					Assert.AreEqual(2025,                               dr.GetInt32(2,   defaultValue: 42));
					Assert.AreEqual(9000000000L,                        dr.GetInt64(3,   defaultValue: 0L));
					Assert.AreEqual(1.5f,                               dr.GetFloat(4,   defaultValue: 0f));
					Assert.AreEqual(2.5d,                               dr.GetDouble(5,  defaultValue: 0d));
					Assert.AreEqual(99.9999m,                           dr.GetDecimal(6, defaultValue: 0m));
					Assert.AreEqual("actual",                           dr.GetString(7,  defaultValue: "fallback"));
					Assert.AreEqual(new DateTime(2026, 1, 15),          dr.GetDateTime(8, defaultValue: DateTime.MinValue));

					return 0;
				});
			});
		}

		[TestMethod]
		public void GetUtcDateTimeNullable_RoundTrip()
		{
			_repository.RunCommand(cmd =>
			{
				cmd.UseSql("select cast('2026-04-29T10:30:00' as datetime2), cast(null as datetime2);");

				cmd.GetByReader(dr =>
				{
					Assert.IsTrue(dr.Read());

					var utc = dr.GetUtcDateTimeNullable(0);
					Assert.IsNotNull(utc);
					Assert.AreEqual(DateTimeKind.Utc, utc!.Value.Kind);
					Assert.AreEqual(new DateTime(2026, 4, 29, 10, 30, 0, DateTimeKind.Utc), utc.Value);

					Assert.IsNull(dr.GetUtcDateTimeNullable(1));

					return 0;
				});
			});
		}
	}
}
