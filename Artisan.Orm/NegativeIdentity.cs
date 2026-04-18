using System.Threading;

namespace Artisan.Orm
{

	/// <summary>
	/// Thread-safe generator of unique negative <see cref="long"/> identity values,
	/// used to mark new entities in object graphs before the real identity is assigned by the database.
	/// Values are produced monotonically starting from <see cref="long.MinValue"/>.
	/// </summary>
	public static class Int64NegativeIdentity
	{
		private static long _identity = long.MinValue;

		public static long Next => Interlocked.Increment(ref _identity) - 1;
	}

	/// <summary>
	/// Thread-safe generator of unique negative <see cref="int"/> identity values,
	/// used to mark new entities in object graphs before the real identity is assigned by the database.
	/// Values are produced monotonically starting from <see cref="int.MinValue"/>.
	/// </summary>
	public static class Int32NegativeIdentity
	{
		private static int _identity = int.MinValue;

		public static int Next => Interlocked.Increment(ref _identity) - 1;
	}

	/// <summary>
	/// Thread-safe generator of unique negative <see cref="short"/> identity values,
	/// used to mark new entities in object graphs before the real identity is assigned by the database.
	/// Values are produced monotonically starting from <see cref="short.MinValue"/>.
	/// Note: <see cref="Interlocked"/> has no 16-bit overload, so the counter is kept in an <see cref="int"/>
	/// and the result is narrowed back to <see cref="short"/>.
	/// </summary>
	public static class Int16NegativeIdentity
	{
		private static int _identity = short.MinValue;

		public static short Next => (short)(Interlocked.Increment(ref _identity) - 1);
	}

}
