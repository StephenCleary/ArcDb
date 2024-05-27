using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArcDb.Storage.Hal;

public interface IFileOverlay : IDisposable
{
	ref T As<T>() where T : struct;
}

internal sealed unsafe class FileOverlay : IFileOverlay
{
	public FileOverlay(FileMapping mapping, long offset, long length)
	{
		_view = mapping.MemoryMappedFile.CreateViewAccessor(offset, length, mapping.IsReadOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite);
		_view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
	}

	public void Dispose()
	{
		// TODO: Nito.Disposables to make this safe.
		_view.SafeMemoryMappedViewHandle.ReleasePointer();
		_view.Dispose();
	}

	public ref T As<T>() where T : struct => ref Unsafe.AsRef<T>(_pointer);

	private readonly MemoryMappedViewAccessor _view;
	private readonly byte* _pointer;
}