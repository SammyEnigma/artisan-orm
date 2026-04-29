using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Folders;
using Tests.DAL.Folders.Models;

namespace Tests.Tests
{
	[TestClass]
	public class TreeAsyncTest
	{
		private Repository _repository = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			var appSettings = new AppSettings();
			_repository = new Repository(appSettings.ConnectionStrings.DatabaseConnection);
		}

		[TestCleanup]
		public void TestCleanup() => _repository.Dispose();


		[TestMethod]
		public async Task ReadToTreeAsync_BuildsHierarchy()
		{
			var tree = await _repository.GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetFolderWithSubFolders");
				cmd.AddIntParam("@FolderId", 1);

				return cmd.ReadToTreeAsync<Folder>();
			});

			Assert.IsNotNull(tree);
			Assert.IsNotNull(tree!.Children);
		}

		[TestMethod]
		public async Task ReadToTreeAsync_HierarchicallySorted_WithCancellationToken()
		{
			using var cts = new CancellationTokenSource();

			var tree = await _repository.GetByCommandAsync(async (cmd, ct) =>
			{
				cmd.UseProcedure("dbo.GetFolderWithSubFolders");
				cmd.AddIntParam("@FolderId", 1);

				return await cmd.ReadToTreeAsync<Folder>(hierarchicallySorted: true, cancellationToken: ct).ConfigureAwait(false);
			}, cts.Token);

			Assert.IsNotNull(tree);
		}

		[TestMethod]
		public async Task ReadToTreeListAsync_ReturnsRoots()
		{
			var roots = await _repository.GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetFolderWithSubFolders");
				cmd.AddIntParam("@FolderId", 1);

				return cmd.ReadToTreeListAsync<Folder>();
			});

			Assert.IsNotNull(roots);
			Assert.IsTrue(roots.Count >= 1);
		}

		[TestMethod]
		public async Task ReadToTreeListAsync_WithCreateFunc()
		{
			var roots = await _repository.GetByCommandAsync(async (cmd, ct) =>
			{
				cmd.UseProcedure("dbo.GetFolderWithSubFolders");
				cmd.AddIntParam("@FolderId", 1);

				return await cmd.ReadToTreeListAsync<Folder>(
					createFunc: dr => new Folder
					{
						Id        = dr.GetInt32(0),
						ParentId  = dr.GetInt32Nullable(1),
						Name      = dr.GetString(2),
						Level     = dr.GetInt16(3),
						HidCode   = dr.FieldCount > 4 ? dr.GetStringNullable(4) : null,
						HidPath   = dr.FieldCount > 5 ? dr.GetStringNullable(5) : null,
						Path      = dr.FieldCount > 6 ? dr.GetStringNullable(6) : null,
						Children  = new List<Folder>()
					},
					cancellationToken: ct).ConfigureAwait(false);
			}, CancellationToken.None);

			Assert.IsNotNull(roots);
			Assert.IsTrue(roots.Count >= 1);
		}
	}
}
