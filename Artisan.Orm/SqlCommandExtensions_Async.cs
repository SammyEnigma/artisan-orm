using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlCommandExtensions
	{
		#region [ GetReaderFlagsAndOpenConnectionAsync ]

		private static async Task<CommandBehavior> GetReaderFlagsAndOpenConnectionAsync(SqlCommand cmd, CommandBehavior defaultReaderFlag = CommandBehavior.Default)
		{
			var readerFlags = defaultReaderFlag;

			if (cmd.Connection.State == ConnectionState.Closed)
			{
				await cmd.Connection.OpenAsync().ConfigureAwait(false);

				if (defaultReaderFlag == CommandBehavior.Default)
					readerFlags = CommandBehavior.CloseConnection;
				else
					readerFlags |= CommandBehavior.CloseConnection;
			}

			return readerFlags;
		}
	
		#endregion


		#region [ GetByReaderAsync, ExecuteReaderAsync ]

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, T> func, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return func(dr);
		}

		public static async Task<T> GetByReaderAsync<T>(this SqlCommand cmd,  Func<SqlDataReader, SqlParameter, T> func, CancellationToken cancellationToken = default)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return func(dr, returnValueParam);
		}

		public static async Task<int> ExecuteReaderAsync(this SqlCommand cmd, Action<SqlDataReader> action, CancellationToken cancellationToken = default)
		{
			var returnValueParam = cmd.ReturnValueParam();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd).ConfigureAwait(false);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				action(dr);
			}

			return (int)returnValueParam.Value!;  // ReturnValueParam() always returns non-null; Value is set by SQL Server
		}

		#endregion


		#region [ ReadToAsync, ReadAsAsync ]
	
		private static async Task<T?> ReadToValueAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			if (dr.Read())
			{
				if (typeof(T).IsNullableValueType() && dr.IsDBNull(0))
					return default;

				return dr.GetValue<T>();
			}

			return default;
		}


		public static async Task<T?> ReadToAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.Read() ? createFunc(dr) : default;
		}

		public static async Task<T?> ReadToAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>(cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToAsync(MappingManager.GetCreateObjectFunc<T>(), cancellationToken).ConfigureAwait(false);
		}


		public static async Task<T?> ReadAsAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToValueAsync<T>(cancellationToken).ConfigureAwait(false);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.Read() ? dr.CreateObject<T>() : default;
		}

		#endregion


		#region [ ReadToListAsync, ReadAsListAsync, ReadToArrayAsync, ReadAsArrayAsync ]
	
		private static async Task<IList<T>> ReadToListOfValuesAsync<T>(this SqlCommand cmd, IList<T>? list, CancellationToken cancellationToken = default)
		{
			list ??= new List<T>();

			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				if (isNullableValueType)
				{
					var underlyingType = type.GetUnderlyingType();
					while (dr.Read())
						list.Add(dr.IsDBNull(0) ? default! : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType));  // default! — T is Nullable<X> here, null is valid
				}
				else
				{
					while (dr.Read())
						list.Add(SqlDataReaderExtensions.GetValue<T>(dr, type));
				}
			}

			return list;
		}

		private static async Task<IList<T>> ReadToListOfObjectsAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, CancellationToken cancellationToken = default)
		{
			list ??= new List<T>();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
				while (dr.Read())
					list.Add(createFunc(dr));

			return list;
		}
	

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, CancellationToken cancellationToken = default)
		{
			list ??= new List<T>();

			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				while (dr.Read())
				{
					if (isNullableValueType && dr.IsDBNull(0))
						list.Add(default!);  // default! — T is Nullable<X> here, null is valid
					else
						list.Add(createFunc(dr));
				}
			}

			return list;
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken).ConfigureAwait(false);
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, IList<T>? list, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken).ConfigureAwait(false);
		}

		public static async Task<IList<T>> ReadToListAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken).ConfigureAwait(false);
		}


		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, IList<T>? list, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(list).ConfigureAwait(false);

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using (var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false))
			{
				list = dr.ReadAsList<T>(list, getNextResult: false);
			}

			return list;
		}

		public static async Task<IList<T>> ReadAsListAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return await cmd.ReadToListOfValuesAsync<T>(null, cancellationToken).ConfigureAwait(false);

			return await cmd.ReadAsListAsync<T>(null, cancellationToken).ConfigureAwait(false);
		}
	

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadToListAsync<T>(createFunc, cancellationToken).ConfigureAwait(false)).ToArray();
		}

		public static async Task<T[]> ReadToArrayAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadToListAsync<T>(cancellationToken).ConfigureAwait(false)).ToArray();
		}
	
		public static async Task<T[]> ReadAsArrayAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			return (await cmd.ReadAsListAsync<T>(cancellationToken).ConfigureAwait(false)).ToArray();
		}

		#endregion


		#region [ ReadToAsyncEnumerable, ReadAsAsyncEnumerable ]

		private static async IAsyncEnumerable<T> ReadToAsyncEnumerableValues<T>(
			this SqlCommand cmd,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var type = typeof(T);
			var isNullableValueType = type.IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			await using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);

			if (isNullableValueType)
			{
				var underlyingType = type.GetUnderlyingType();
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					yield return dr.IsDBNull(0) ? default! : SqlDataReaderExtensions.GetValue<T>(dr, underlyingType);  // default! — T is Nullable<X> here, null is valid
			}
			else
			{
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					yield return SqlDataReaderExtensions.GetValue<T>(dr, type);
			}
		}

		private static async IAsyncEnumerable<T> ReadToAsyncEnumerableObjects<T>(
			this SqlCommand cmd,
			Func<SqlDataReader, T> createFunc,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var isNullableValueType = typeof(T).IsNullableValueType();

			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			await using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);

			if (isNullableValueType)
			{
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
				{
					if (dr.IsDBNull(0))
						yield return default!;  // default! — T is Nullable<X> here, null is valid
					else
						yield return createFunc(dr);
				}
			}
			else
			{
				while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
					yield return createFunc(dr);
			}
		}

		private static async IAsyncEnumerable<T> ReadAsAsyncEnumerableObjects<T>(
			this SqlCommand cmd,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			await using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);

			var key = SqlDataReaderExtensions.GetAutoCreateObjectFuncKey<T>(dr);
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key);

			if (autoMappingFunc == null)
			{
				autoMappingFunc = SqlDataReaderExtensions.CreateAutoMappingFunc<T>(dr);
				MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
			}

			while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
				yield return autoMappingFunc(dr);
		}


		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using <paramref name="createFunc"/> to map each row.
		/// Rows are yielded one at a time — no buffering into a list.
		/// </summary>
		public static IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			this SqlCommand cmd,
			Func<SqlDataReader, T> createFunc,
			CancellationToken cancellationToken = default)
		{
			return cmd.ReadToAsyncEnumerableObjects(createFunc, cancellationToken);
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using a registered mapping function for <typeparamref name="T"/> (or scalar conversion for simple types).
		/// Rows are yielded one at a time — no buffering into a list.
		/// </summary>
		public static IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToAsyncEnumerableValues<T>(cancellationToken);

			return cmd.ReadToAsyncEnumerableObjects(MappingManager.GetCreateObjectFunc<T>(), cancellationToken);
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using auto-mapping (reflection-based, result cached after first execution).
		/// Rows are yielded one at a time — no buffering into a list.
		/// </summary>
		public static IAsyncEnumerable<T> ReadAsAsyncEnumerable<T>(
			this SqlCommand cmd,
			CancellationToken cancellationToken = default)
		{
			if (typeof(T).IsSimpleType())
				return cmd.ReadToAsyncEnumerableValues<T>(cancellationToken);

			return cmd.ReadAsAsyncEnumerableObjects<T>(cancellationToken);
		}

		#endregion


		#region [ ReadToObjectRow(s)Async, ReadAsObjectRow(s)Async ]

		public static async Task<ObjectRow?> ReadToObjectRowAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadToObjectRow(createFunc, false);
		}

		public static async Task<ObjectRow?> ReadToObjectRowAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadToObjectRow<T>();
		}


		public static async Task<ObjectRows> ReadToObjectRowsAsync(this SqlCommand cmd, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadToObjectRows(createFunc, false);
		}

		public static async Task<ObjectRows> ReadAsObjectRowsAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadAsObjectRows();
		}


		public static async Task<ObjectRow?> ReadAsObjectRowAsync(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleRow).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadAsObjectRow();
		}

		public static async Task<ObjectRows> ReadToObjectRowsAsync<T>(this SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadToObjectRows<T>();
		}
	
		#endregion


		#region [ ReadToDictionaryAsync, ReadAsDictionaryAsync ]

		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, Func<SqlDataReader, TValue> createFunc, CancellationToken cancellationToken = default) where TKey : notnull
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadToDictionary<TKey, TValue>(createFunc);
		}


		public static async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(this SqlCommand cmd, CancellationToken cancellationToken = default) where TKey : notnull
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadToDictionary<TKey, TValue>();
		}

		public static async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(this SqlCommand cmd, CancellationToken cancellationToken = default) where TKey : notnull
		{
			var readerFlags = await GetReaderFlagsAndOpenConnectionAsync(cmd, CommandBehavior.SingleResult).ConfigureAwait(false);

			using var dr = await cmd.ExecuteReaderAsync(readerFlags, cancellationToken).ConfigureAwait(false);
			return dr.ReadAsDictionary<TKey, TValue>();
		}

		#endregion 


		#region [ ReadToTree, ReadToTreeList ]
	
		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		public static async Task<T?> ReadToTreeAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken).ConfigureAwait(false)).ToTree(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, list, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(createFunc, null, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, IList<T>? list, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), list, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		public static async Task<IList<T>> ReadToTreeListAsync<T>(this SqlCommand cmd, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			return (await cmd.ReadToListOfObjectsAsync<T>(MappingManager.GetCreateObjectFunc<T>(), null, cancellationToken).ConfigureAwait(false)).ToTreeList(hierarchicallySorted);
		}

		#endregion

	}

}
