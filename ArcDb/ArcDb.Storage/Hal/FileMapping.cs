using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcDb.Storage.Hal;

public interface IFileMapping : IDisposable
{
	IFileOverlay CreateOverlay(long offset, long length);
}

internal sealed class FileMapping : IFileMapping
{
	public FileMapping(File file, long capacity)
	{
		MemoryMappedFile = MemoryMappedFile.CreateFromFile(
			file.Stream,
			mapName: null,
			capacity,
			file.IsReadOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite, HandleInheritability.None,
			leaveOpen: true);
		IsReadOnly = file.IsReadOnly;
	}

	public void Dispose() => MemoryMappedFile.Dispose();

	public IFileOverlay CreateOverlay(long offset, long length) => new FileOverlay(this, offset, length);

	public MemoryMappedFile MemoryMappedFile { get; }
	public bool IsReadOnly { get; }
}