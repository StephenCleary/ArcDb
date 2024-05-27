using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace ArcDb.Storage.Tests
{
	public class UnitTest1
	{
		[Fact]
		public void FolioSizes()
		{
			Assert.Equal(MetadataFolioLayout.FolioSize, Unsafe.SizeOf<MetadataFolioLayout>());
			Assert.Equal(MetadataFolioLayout.FolioDataSize, Unsafe.SizeOf<DatabaseHeaderLayout>());
		}

		[Fact]
		public void Test1()
		{
			var fileSystem = ArcDb.Storage.Hal.PhysicalFileSystem.Instance;

			File.Delete(@"C:\Work\tmp.arcdb");
			using var file = fileSystem.OpenOrCreateFile(@"C:\Work\tmp.arcdb");
			using var mapping = file.CreateMapping(MetadataFolioLayout.FolioSize);
			using var overlay = mapping.CreateOverlay(0, 0);
			ref var data = ref overlay.As<MetadataFolioLayout>();
			Assert.False(data.ValidateHash());
			data.Header.Type = MetadataFolioLayout.Type.DatabaseHeader;
			ref var header = ref data.DataAs<DatabaseHeaderLayout>();
			header.InitializeMagic();
			data.RecalculateHash();
			Assert.True(data.ValidateHash());
		}

		private static void Test()
		{

			// file.Flush(flushToDisk: true); // FlushFileBuffers

			// file.SetLength(10000); // Extending file length works on all systems; shrinking fails on Windows until mappings are all closed.
		}

		// Ideal future version of C#: https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md#safe-fixed-size-buffers
		// Currently causes the errors
		// Severity	Code	Description	Project	File	Line	Suppression State
		// Error	CS0650	Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
		// Error	CS0270	Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
		//private struct Data
		//{
		//	private int _first;
		//	private byte __padding[12];
		//	private int _second;
		//	public int First
		//	{
		//		readonly get => OverlayHelpers.ReadBigEndian(_first);
		//		set => OverlayHelpers.WriteBigEndian(out _first, value);
		//	}
		//	public int Second
		//	{
		//		readonly get => OverlayHelpers.ReadBigEndian(_second);
		//		set => OverlayHelpers.WriteBigEndian(out _second, value);
		//	}
		//}
	}
}
