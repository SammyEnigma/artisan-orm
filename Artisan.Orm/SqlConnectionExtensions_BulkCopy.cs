using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{

	public static class SqlConnectionExtensions
	{

		private static void AddColumnMappings(SqlBulkCopy bulkCopy, DataTable dataTable)
		{
			foreach (DataColumn column in dataTable.Columns)
				bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
		}


		/// <summary>
		/// Bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using a registered mapper
		/// (see <c>DataTableExtensions.ToDataTable&lt;T&gt;</c> — requires a registered mapping function).
		/// Opens the connection if it was closed and closes it again on exit.
		/// Returns the number of rows written.
		/// </summary>
		public static int BulkCopy<T>(
			this SqlConnection conn,
			IEnumerable<T> rows,
			string destinationTable,
			SqlTransaction? transaction = null,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			var dataTable = rows.ToDataTable<T>();

			var isConnectionClosed = conn.State == ConnectionState.Closed;

			if (isConnectionClosed)
				conn.Open();

			try
			{
				using var bulkCopy = new SqlBulkCopy(conn, options, transaction)
				{
					DestinationTableName = destinationTable,
					BatchSize = batchSize,
					BulkCopyTimeout = timeout,
				};

				AddColumnMappings(bulkCopy, dataTable);
				bulkCopy.WriteToServer(dataTable);

				return dataTable.Rows.Count;
			}
			finally
			{
				if (isConnectionClosed)
					conn.Close();
			}
		}


		/// <summary>
		/// Bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using auto-mapping
		/// (see <c>DataTableExtensions.AsDataTable&lt;T&gt;</c> — reflection-based, mapping cached after first call).
		/// Opens the connection if it was closed and closes it again on exit.
		/// Returns the number of rows written.
		/// </summary>
		public static int BulkCopyAs<T>(
			this SqlConnection conn,
			IEnumerable<T> rows,
			string destinationTable,
			SqlTransaction? transaction = null,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			var dataTable = rows.AsDataTable<T>(destinationTable);

			var isConnectionClosed = conn.State == ConnectionState.Closed;

			if (isConnectionClosed)
				conn.Open();

			try
			{
				using var bulkCopy = new SqlBulkCopy(conn, options, transaction)
				{
					DestinationTableName = destinationTable,
					BatchSize = batchSize,
					BulkCopyTimeout = timeout,
				};

				AddColumnMappings(bulkCopy, dataTable);
				bulkCopy.WriteToServer(dataTable);

				return dataTable.Rows.Count;
			}
			finally
			{
				if (isConnectionClosed)
					conn.Close();
			}
		}


		/// <summary>
		/// Asynchronously bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using a registered mapper
		/// (see <c>DataTableExtensions.ToDataTable&lt;T&gt;</c> — requires a registered mapping function).
		/// Opens the connection if it was closed and closes it again on exit.
		/// Returns the number of rows written.
		/// </summary>
		public static async Task<int> BulkCopyAsync<T>(
			this SqlConnection conn,
			IEnumerable<T> rows,
			string destinationTable,
			CancellationToken cancellationToken = default,
			SqlTransaction? transaction = null,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			var dataTable = rows.ToDataTable<T>();

			var isConnectionClosed = conn.State == ConnectionState.Closed;

			if (isConnectionClosed)
				await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				using var bulkCopy = new SqlBulkCopy(conn, options, transaction)
				{
					DestinationTableName = destinationTable,
					BatchSize = batchSize,
					BulkCopyTimeout = timeout,
				};

				AddColumnMappings(bulkCopy, dataTable);
				await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

				return dataTable.Rows.Count;
			}
			finally
			{
				if (isConnectionClosed)
					await conn.CloseAsync().ConfigureAwait(false);
			}
		}


		/// <summary>
		/// Asynchronously bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using auto-mapping
		/// (see <c>DataTableExtensions.AsDataTable&lt;T&gt;</c> — reflection-based, mapping cached after first call).
		/// Opens the connection if it was closed and closes it again on exit.
		/// Returns the number of rows written.
		/// </summary>
		public static async Task<int> BulkCopyAsAsync<T>(
			this SqlConnection conn,
			IEnumerable<T> rows,
			string destinationTable,
			CancellationToken cancellationToken = default,
			SqlTransaction? transaction = null,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			var dataTable = rows.AsDataTable<T>(destinationTable);

			var isConnectionClosed = conn.State == ConnectionState.Closed;

			if (isConnectionClosed)
				await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				using var bulkCopy = new SqlBulkCopy(conn, options, transaction)
				{
					DestinationTableName = destinationTable,
					BatchSize = batchSize,
					BulkCopyTimeout = timeout,
				};

				AddColumnMappings(bulkCopy, dataTable);
				await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

				return dataTable.Rows.Count;
			}
			finally
			{
				if (isConnectionClosed)
					await conn.CloseAsync().ConfigureAwait(false);
			}
		}

	}

}
