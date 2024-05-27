using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcDb.Storage.Hal;

public interface IFile : IDisposable
{
	long Length { get; set; }
	IFileMapping CreateMapping(long capacity); // TODO: do we need capacity, or can it always just be the file size?
	void Flush();
}

public sealed class File : IFile
{
	public static File OpenOrCreate(string path)
	{
		// TODO: pass buffer size of 0 instead?
		return new(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, MetadataFolioLayout.FolioSize, FileOptions.RandomAccess), isReadOnly: false);
	}

	private File(FileStream stream, bool isReadOnly)
	{
		Stream = stream;
		IsReadOnly = isReadOnly;
	}

	public long Length
	{
		get => Stream.Length;
		set => Stream.SetLength(value);
	}

	public IFileMapping CreateMapping(long capacity) => new FileMapping(this, capacity);
	public void Flush() => Stream.Flush(flushToDisk: true);
	public void Dispose() => Stream.Dispose();

	public FileStream Stream { get; }
	public bool IsReadOnly { get; }
}