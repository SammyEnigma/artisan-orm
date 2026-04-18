using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public class RepositoryBase: IDisposable, IAsyncDisposable
	{
		private bool _disposed;

		public SqlConnection Connection { get; private set; }

		public string ConnectionString { get; private set; }

		public SqlTransaction Transaction { get; set; }


			public RepositoryBase
			(
				SqlTransaction transaction,
				string connectionString
				//string connectionStringName = "DatabaseConnection",
				//string activeSolutionConfiguration = null,
				//string connectionStringsSectionName = "ConnectionStrings",
				//string jsonSettingsFileRelativePath = "appsettings.json"
			)
			{
				//ConnectionString = ConnectionStringHelper.GetConnectionString(connectionStringName, activeSolutionConfiguration, connectionStringsSectionName, jsonSettingsFileRelativePath);
				ConnectionString = connectionString;


				Connection = new SqlConnection(ConnectionString);

				Transaction = transaction;
			}

			//public RepositoryBase ()
			//: this((SqlTransaction)null) {}


		//public RepositoryBase(SqlTransaction transaction, string connectionStringOrconnectionStringName)
		//{
		//	if (connectionStringOrconnectionStringName.Contains(';') && connectionStringOrconnectionStringName.Contains('='))
		//		ConnectionString = connectionStringOrconnectionStringName;
		//	else
		//		ConnectionString = ConnectionStringHelper.GetConnectionString(connectionStringOrconnectionStringName);

		//	Connection = new SqlConnection(ConnectionString);

		//	Transaction = transaction;
		//}

		public RepositoryBase(string connectionString)
			: this((SqlTransaction)null, connectionString) {}


		/// <summary>
		/// <para>Low-level transaction helper. Opens the connection if it was closed,
		/// begins a <see cref="SqlTransaction"/> with the given <paramref name="isolationLevel"/>
		/// and passes it to <paramref name="action"/>.</para>
		///
		/// <para><b>The caller is fully responsible for committing the transaction.</b>
		/// If <paramref name="action"/> returns without calling
		/// <see cref="SqlTransaction.Commit"/>, the transaction is disposed on exit
		/// and all changes are rolled back. This is by design: the method does not
		/// know whether the work inside <paramref name="action"/> is meant to be
		/// persisted or not.</para>
		///
		/// <para>If <paramref name="action"/> throws, the transaction is rolled back
		/// and the exception is rethrown.</para>
		///
		/// <para>On exit — whether successful or not — the transaction is disposed,
		/// <see cref="Transaction"/> is set to <c>null</c>, and the connection is
		/// closed if it was opened by this method.</para>
		///
		/// <para>Prefer <see cref="RunInTransaction(IsolationLevel, Action{SqlTransaction})"/>
		/// if you want automatic commit-on-success / rollback-on-exception semantics.</para>
		/// </summary>
		/// <param name="isolationLevel">The isolation level under which the transaction runs.</param>
		/// <param name="action">The code to execute inside the transaction. Must call
		/// <see cref="SqlTransaction.Commit"/> explicitly to persist any changes.</param>
		public void BeginTransaction(IsolationLevel isolationLevel, Action<SqlTransaction> action)
		{
			var isConnectionClosed = Connection.State == ConnectionState.Closed;

			if (isConnectionClosed)
				Connection.Open();

			Transaction = Connection.BeginTransaction(isolationLevel);

			try
			{
				action(Transaction);
			}
			catch
			{
				Transaction.Rollback();
				throw;
			}
			finally
			{
				Transaction?.Dispose();
				Transaction = null;

				if(isConnectionClosed)
					Connection.Close();
			}
		}

		/// <summary>
		/// <para>Same as <see cref="BeginTransaction(IsolationLevel, Action{SqlTransaction})"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>, which lets SQL Server apply
		/// its default isolation level (normally <c>READ COMMITTED</c>).</para>
		///
		/// <para><b>The caller is fully responsible for committing the transaction</b> —
		/// see the overload for details.</para>
		///
		/// <para>Prefer <see cref="RunInTransaction(Action{SqlTransaction})"/> if you want
		/// automatic commit-on-success / rollback-on-exception semantics.</para>
		/// </summary>
		/// <param name="action">The code to execute inside the transaction. Must call
		/// <see cref="SqlTransaction.Commit"/> explicitly to persist any changes.</param>
		public void BeginTransaction(Action<SqlTransaction> action)
		{
			BeginTransaction(IsolationLevel.Unspecified, action);
		}

		/// <summary>
		/// <para>Runs <paramref name="action"/> inside a <see cref="SqlTransaction"/>
		/// with automatic commit/rollback semantics.</para>
		///
		/// <para>Opens the connection if it was closed, begins a transaction with the
		/// given <paramref name="isolationLevel"/>, and passes it to <paramref name="action"/>.</para>
		///
		/// <para>If <paramref name="action"/> completes normally, the transaction is
		/// committed automatically. If it throws, the transaction is rolled back and
		/// the exception is rethrown.</para>
		///
		/// <para>On exit — whether successful or not — the transaction is disposed,
		/// <see cref="Transaction"/> is set to <c>null</c>, and the connection is
		/// closed if it was opened by this method.</para>
		///
		/// <para>The caller must not call <see cref="SqlTransaction.Commit"/> or
		/// <see cref="SqlTransaction.Rollback()"/> inside <paramref name="action"/> —
		/// that is handled by this method.</para>
		/// </summary>
		/// <param name="isolationLevel">The isolation level under which the transaction runs.</param>
		/// <param name="action">The code to execute inside the transaction.</param>
		public void RunInTransaction(IsolationLevel isolationLevel, Action<SqlTransaction> action)
		{
			var isConnectionClosed = Connection.State == ConnectionState.Closed;

			if (isConnectionClosed)
				Connection.Open();

			Transaction = Connection.BeginTransaction(isolationLevel);

			try
			{
				action(Transaction);
				Transaction.Commit();
			}
			finally
			{
				// SqlTransaction.Dispose() rolls back an uncommitted transaction,
				// so no explicit Rollback is needed in the catch path.
				Transaction?.Dispose();
				Transaction = null;

				if (isConnectionClosed)
					Connection.Close();
			}
		}

		/// <summary>
		/// <para>Same as <see cref="RunInTransaction(IsolationLevel, Action{SqlTransaction})"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>, which lets SQL Server apply
		/// its default isolation level (normally <c>READ COMMITTED</c>).</para>
		///
		/// <para>Commits on successful completion of <paramref name="action"/>,
		/// rolls back on exception. The caller must not commit or roll back manually.</para>
		/// </summary>
		/// <param name="action">The code to execute inside the transaction.</param>
		public void RunInTransaction(Action<SqlTransaction> action)
		{
			RunInTransaction(IsolationLevel.Unspecified, action);
		}

		/// <summary>
		/// <para>Asynchronous counterpart of
		/// <see cref="BeginTransaction(IsolationLevel, Action{SqlTransaction})"/>.</para>
		///
		/// <para><b>The caller is fully responsible for committing the transaction</b>
		/// (via <c>await transaction.CommitAsync(ct)</c>). If <paramref name="asyncAction"/>
		/// returns without committing, the transaction is rolled back on dispose.</para>
		///
		/// <para>Prefer <see cref="RunInTransactionAsync(IsolationLevel, Func{SqlTransaction, CancellationToken, Task}, CancellationToken)"/>
		/// for automatic commit-on-success / rollback-on-exception semantics.</para>
		/// </summary>
		public async Task BeginTransactionAsync(IsolationLevel isolationLevel, Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			var isConnectionClosed = Connection.State == ConnectionState.Closed;

			if (isConnectionClosed)
				await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

			Transaction = (SqlTransaction)await Connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

			try
			{
				await asyncAction(Transaction, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				await Transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
				throw;
			}
			finally
			{
				if (Transaction != null)
				{
					await Transaction.DisposeAsync().ConfigureAwait(false);
					Transaction = null;
				}

				if (isConnectionClosed)
					await Connection.CloseAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Same as <see cref="BeginTransactionAsync(IsolationLevel, Func{SqlTransaction, CancellationToken, Task}, CancellationToken)"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>. The caller is fully responsible for committing.
		/// </summary>
		public Task BeginTransactionAsync(Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			return BeginTransactionAsync(IsolationLevel.Unspecified, asyncAction, cancellationToken);
		}

		/// <summary>
		/// <para>Asynchronous counterpart of
		/// <see cref="RunInTransaction(IsolationLevel, Action{SqlTransaction})"/>:
		/// commits <paramref name="asyncAction"/> on successful completion,
		/// rolls back on exception.</para>
		///
		/// <para>The caller must not commit or roll back manually.</para>
		/// </summary>
		public async Task RunInTransactionAsync(IsolationLevel isolationLevel, Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			var isConnectionClosed = Connection.State == ConnectionState.Closed;

			if (isConnectionClosed)
				await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

			Transaction = (SqlTransaction)await Connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

			try
			{
				await asyncAction(Transaction, cancellationToken).ConfigureAwait(false);
				await Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				// SqlTransaction.DisposeAsync rolls back an uncommitted transaction.
				if (Transaction != null)
				{
					await Transaction.DisposeAsync().ConfigureAwait(false);
					Transaction = null;
				}

				if (isConnectionClosed)
					await Connection.CloseAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Same as <see cref="RunInTransactionAsync(IsolationLevel, Func{SqlTransaction, CancellationToken, Task}, CancellationToken)"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>.
		/// </summary>
		public Task RunInTransactionAsync(Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			return RunInTransactionAsync(IsolationLevel.Unspecified, asyncAction, cancellationToken);
		}


		public SqlCommand CreateCommand()
		{
			var cmd = Connection.CreateCommand();

			if (Transaction != null)
				cmd.Transaction = Transaction;

			return cmd;
		}


		public SqlCommand CreateCommand(string sql, params SqlParameter[] sqlParameters)
		{
			var cmd = CreateCommand();

			cmd.ConfigureCommand(sql, sqlParameters);

			return cmd;
		}


		public SqlCommand CreateCommand(string sql, Action<SqlCommand> action)
		{
			var cmd = CreateCommand();

			cmd.ConfigureCommand(sql, action);

			return cmd;
		}


		/// <summary> 
		/// <para/>Prepares SqlCommand and pass it to a Func-parameter.
		/// <para/>Parameter "func" is the code where SqlCommand has to be configured with parameters, execute reader and return result. 
		/// </summary>
		public T GetByCommand<T>(Func<SqlCommand, T> func)
		{
			using var cmd = CreateCommand();
			return func(cmd);
		}

		/// <summary> 
		/// <para/>Prepares SqlCommand and pass it to a Func-parameter.
		/// <para/>Parameter "func" is the code where SqlCommand has to be configured with parameters, execute reader and return result. 
		/// </summary>
		public async Task<T> GetByCommandAsync<T>(Func<SqlCommand, Task<T>> funcAsync)
		{
			using var cmd = CreateCommand();
			return await funcAsync(cmd).ConfigureAwait(false);
		}

		/// <summary> 
		/// <para/>Prepares SqlCommand and pass it to a Func-parameter.
		/// <para/>Parameter "func" is the code where SqlCommand has to be configured with parameters, execute reader and return result.
		/// <para/>CancellationToken 
		/// </summary>
		public async Task<T> GetByCommandAsync<T>(Func<SqlCommand, CancellationToken, Task<T>> funcAsync, CancellationToken cancellationToken)
		{
			using var cmd = CreateCommand();
			return await funcAsync(cmd, cancellationToken).ConfigureAwait(false);
		}

		private static int ExecuteCommand(SqlCommand cmd)
		{
			var returnValueParam = cmd.GetReturnValueParam();
			var isConnectionClosed = true;

			try
			{
				isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

				if (isConnectionClosed)
					cmd.Connection.Open();

				cmd.ExecuteNonQuery();
			}
			finally
			{
				if (isConnectionClosed)
					cmd.Connection.Close();
			}

			return (int) returnValueParam.Value;
		}


		/// <summary> 
		/// <para/>Executes SqlCommand which returns nothing but ReturnValue.
		/// <para/>Calls ExecuteNonQueryAsync inside.
		/// <para/>Parameter "action" is the code where SqlCommand has to be configured with parameters. 
		/// <para/>Returns ReturnValue - the value from TSQL "RETURN [Value]" statement. If there is no RETURN in TSQL then returns 0.
		/// </summary>
		public Int32 ExecuteCommand (Action<SqlCommand> action)
		{
			using var cmd = CreateCommand();

			cmd.AddReturnValueParam();

			action(cmd);

			return ExecuteCommand(cmd);
		}

		public Int32 Execute (string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);

			cmd.AddReturnValueParam();

			return ExecuteCommand(cmd);
		}

		public Int32 Execute (string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);

			cmd.AddReturnValueParam();

			return ExecuteCommand(cmd);
		}
	
		private static async Task<Int32> ExecuteCommandAsync (SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var returnValueParam = cmd.GetReturnValueParam();
			var isConnectionClosed = true;

			try
			{
				isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

				if (isConnectionClosed)
					cmd.Connection.Open();

				await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				if (isConnectionClosed)
					cmd.Connection.Close();
			}

			return (int) returnValueParam.Value;
		}
	
		/// <summary> 
		/// <para/>Executes SqlCommand which returns nothing but ReturnValue.
		/// <para/>Calls ExecuteNonQueryAsync inside.
		/// <para/>Parameter "action" is the code where SqlCommand has to be configured with parameters. 
		/// <para/>Returns ReturnValue - the value from TSQL "RETURN [Value]" statement. If there is no RETURN in TSQL then returns 0.
		/// </summary>
		public async Task<Int32> ExecuteCommandAsync (Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand();

			cmd.AddReturnValueParam();

			action(cmd);

			return await ExecuteCommandAsync(cmd, cancellationToken).ConfigureAwait(false);
		}
	
		public async Task<Int32> ExecuteAsync (string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);

			cmd.AddReturnValueParam();

			return await ExecuteCommandAsync(cmd).ConfigureAwait(false);
		}

		public async Task<Int32> ExecuteAsync (string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);

			cmd.AddReturnValueParam();

			return await ExecuteCommandAsync(cmd, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// <para>Creates SqlCommand, passes it to Action argument as SqlCommand parameter, returns nothing.</para>
		/// <para>See GitHub Wiki about this method: <a href="https://github.com/lobodava/artisan-orm/wiki/RepositoryBase-methods-for-SqlCommand-initialization#runcommand">https://github.com/lobodava/artisan-orm/wiki/RepositoryBase-methods-for-SqlCommand-initialization#runcommand</a></para>
		/// </summary>
		public void RunCommand(Action<SqlCommand> action)
		{
			using var cmd = CreateCommand();
			action(cmd);
		}

		public async Task RunCommandAsync(Func<SqlCommand, Task> asyncAction)
		{
			using var cmd = CreateCommand();
			await asyncAction(cmd).ConfigureAwait(false);
		}

		public async Task RunCommandAsync(Func<SqlCommand, CancellationToken, Task> asyncAction, CancellationToken cancellationToken)
		{
			using var cmd = CreateCommand();
			await asyncAction(cmd, cancellationToken).ConfigureAwait(false);
		}

		#region [ ReadTo, ReadAs ]
	
		public T ReadTo<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadTo<T>();
		}

		public T ReadTo<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadTo<T>();
		}
	
		public async Task<T> ReadToAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToAsync<T>().ConfigureAwait(false);
		}

		public async Task<T> ReadToAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<T> ReadToAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public T ReadAs<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAs<T>();
		}

		public T ReadAs<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAs<T>();
		}

		public async Task<T> ReadAsAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsAsync<T>().ConfigureAwait(false);
		}
	
		public async Task<T> ReadAsAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<T> ReadAsAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region [ ReadToList, ReadAsList ]

		public IList<T> ReadToList<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToList<T>();
		}

		public IList<T> ReadToList<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToList<T>();
		}

		public async Task<IList<T>> ReadToListAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListAsync<T>().ConfigureAwait(false);
		}

		public async Task<IList<T>> ReadToListAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<IList<T>> ReadToListAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToListAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public IList<T> ReadAsList<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsList<T>();
		}

		public IList<T> ReadAsList<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsList<T>();
		}
	
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsListAsync<T>().ConfigureAwait(false);
		}

		public async Task<IList<T>> ReadAsListAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsListAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<IList<T>> ReadAsListAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsListAsync<T>(cancellationToken).ConfigureAwait(false);
		}
	
		#endregion

		#region [ ReadToObjectRow, ReadAsObjectRow ]

		public ObjectRow ReadToObjectRow<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToObjectRow<T>();
		}
	
		public ObjectRow ReadToObjectRow<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToObjectRow<T>();
		}

		public async Task<ObjectRow> ReadToObjectRowAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowAsync<T>().ConfigureAwait(false);
		}

		public async Task<ObjectRow> ReadToObjectRowAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<ObjectRow> ReadToObjectRowAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToObjectRowAsync<T>(cancellationToken).ConfigureAwait(false);
		}
	
		public ObjectRow ReadAsObjectRow(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsObjectRow();
		}

		public ObjectRow ReadAsObjectRow(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsObjectRow();
		}
	
		public async Task<ObjectRow> ReadAsObjectRowAsync(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowAsync().ConfigureAwait(false);
		}
		public async Task<ObjectRow> ReadAsObjectRowAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowAsync(cancellationToken).ConfigureAwait(false);
		}

		public async Task<ObjectRow> ReadAsObjectRowAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsObjectRowAsync(cancellationToken).ConfigureAwait(false);
		}

	
		#endregion

		#region [ ReadToObjectRows, ReadAsObjectRows ]

		public ObjectRows ReadToObjectRows<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToObjectRows<T>();
		}

		public ObjectRows ReadToObjectRows<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToObjectRows<T>();
		}
	
		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowsAsync<T>().ConfigureAwait(false);
		}

		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowsAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToObjectRowsAsync<T>(cancellationToken).ConfigureAwait(false);
		}
	
		public ObjectRows ReadAsObjectRows(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsObjectRows();
		}

		public ObjectRows ReadAsObjectRows(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsObjectRows();
		}
	
		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowsAsync().ConfigureAwait(false);
		}

		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowsAsync(cancellationToken).ConfigureAwait(false);
		}

		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsObjectRowsAsync(cancellationToken).ConfigureAwait(false);
		}

		#endregion
	
		#region [ ReadToDictionary ]

		public IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(string sql, params SqlParameter[] sqlParameters) 
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToDictionary<TKey, TValue>();
		}

		public IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToDictionary<TKey, TValue>();
		}

		public IDictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(string sql, params SqlParameter[] sqlParameters) 
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsDictionary<TKey, TValue>();
		}

		public IDictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsDictionary<TKey, TValue>();
		}

		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, params SqlParameter[] sqlParameters) 
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToDictionaryAsync<TKey, TValue>().ConfigureAwait(false);
		}

		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters) 
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(string sql, params SqlParameter[] sqlParameters) 
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsDictionaryAsync<TKey, TValue>().ConfigureAwait(false);
		}
		public async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(string sql, CancellationToken cancellationToken = default, params SqlParameter[] sqlParameters) 
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		public async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		#endregion 

		#region [ ReadToEnumerable, ReadAsEnumerable ]

		public IEnumerable<T> ReadToEnumerable<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToEnumerable<T>();
		}

		public IEnumerable<T> ReadToEnumerable<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToEnumerable<T>();
		}

		public IEnumerable<T> ReadAsEnumerable<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsEnumerable<T>();
		}

		public IEnumerable<T> ReadAsEnumerable<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsEnumerable<T>();
		}
	
		#endregion
	
		#region [ ReadToTree, ReadToTreeList ]

		public T ReadToTree<T>(string sql, bool hierarchicallySorted = false, params SqlParameter[] sqlParameters) where T: class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToTree<T>(hierarchicallySorted);
		}

		public T ReadToTree<T>(string sql, Action<SqlCommand> action, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToTree<T>(hierarchicallySorted);
		}

		public IList<T> ReadToTreeList<T>(string sql, bool hierarchicallySorted = false, params SqlParameter[] sqlParameters) where T: class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToTreeList<T>(hierarchicallySorted);
		}

		public IList<T> ReadToTreeList<T>(string sql, Action<SqlCommand> action, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToTreeList<T>(hierarchicallySorted);
		}
	
		#endregion

		/// <summary>
		/// Adds parameters to <paramref name="cmd"/> by enumerating public properties of
		/// <paramref name="parameters"/> (typically an anonymous type, e.g. <c>new { Id = 1, Name = "Foo" }</c>).
		/// Each property becomes a parameter named after the property, with the property's value.
		/// </summary>
		/// <param name="cmd">The command to add parameters to.</param>
		/// <param name="parameters">
		/// An object whose public properties describe the parameters to add.
		/// Must not be <c>null</c>.
		/// </param>
		public static void AddParams(SqlCommand cmd, object parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var dict = new Dictionary<string, object>();

			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(parameters))
			{
				dict.Add(descriptor.Name, descriptor.GetValue(parameters));
			}

			cmd.AddParams(dict);
		}

		public static void CheckForDataReplyException(SqlDataReader dr)
		{
			var statusCode = dr.ReadTo<string>(getNextResult: false);

			var dataReplyStatus = DataReply.ParseStatus(statusCode);

			if (dataReplyStatus != null )
			{
				if (dr.NextResult())
					throw new DataReplyException(dataReplyStatus.Value, dr.ReadToArray<DataReplyMessage>());

				throw new DataReplyException(dataReplyStatus.Value);
			}

			dr.NextResult();
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;

			if (disposing)
			{
				Transaction?.Dispose();
				Transaction = null;

				Connection?.Dispose();
				Connection = null;
			}

			_disposed = true;
		}

		public async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore().ConfigureAwait(false);

			// The managed-resource branch of Dispose(bool) is a no-op after
			// DisposeAsyncCore has released everything, but we still call
			// Dispose(false) so derived classes that override it to release
			// unmanaged resources get their callback.
			Dispose(disposing: false);
			GC.SuppressFinalize(this);
		}

		protected virtual async ValueTask DisposeAsyncCore()
		{
			if (_disposed) return;

			if (Transaction != null)
			{
				await Transaction.DisposeAsync().ConfigureAwait(false);
				Transaction = null;
			}

			if (Connection != null)
			{
				await Connection.DisposeAsync().ConfigureAwait(false);
				Connection = null;
			}

			_disposed = true;
		}
	}

}

