using System.Runtime.CompilerServices;

namespace ArcDb.Storage;

public sealed unsafe class Overlay : IDisposable
{
	private readonly MemoryMappedViewAccessor _view;
	private readonly byte* _pointer;

	public Overlay(MemoryMappedViewAccessor view)
	{
		_view = view;
		view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
	}

	public void Dispose() => _view.SafeMemoryMappedViewHandle.ReleasePointer();

	public ref T As<T>() where T : struct => ref Unsafe.AsRef<T>(_pointer);
}
