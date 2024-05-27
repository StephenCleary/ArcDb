using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static ArcDb.Storage.OverlayHelpers;
using FolioOffset = uint;
using LogicalPageNumber = uint;

namespace ArcDb.Storage;

public struct DatabaseHeaderLayout
{
	private uint _magic;
	private uint _schemaVersion;
	private ulong _lastTransactionVersion;
	private FolioOffset _lpnFoRoot;
	private FolioOffset _foLpnRoot;
	private FolioOffset _ffoRoot;
	private FolioOffset _flpnRoot;
	private FolioOffset _freelistHead;
	private uint _ffoCount;
	private uint _folioCount;
	private LogicalPageNumber _largestAllocatedLpn;
	private Padding3768 _reserved1;
	private Padding4096 _userDatabaseHeader;
	private Padding232 _reserved2;

	public void InitializeMagic() => Write(out _magic, ExpectedMagic);
	public readonly bool ValidateMagic() => Read(_magic) == ExpectedMagic;

	public uint SchemaVersion
	{
		readonly get => Read(_schemaVersion);
		set => Write(out _schemaVersion, value);
	}

	public ulong LastTransactionVersion
	{
		readonly get => Read(_lastTransactionVersion);
		set => Write(out _lastTransactionVersion, value);
	}

	public FolioOffset LpnFoRoot
	{
		readonly get => Read(_lpnFoRoot);
		set => Write(out _lpnFoRoot, value);
	}

	public FolioOffset FoLpnRoot
	{
		readonly get => Read(_foLpnRoot);
		set => Write(out _foLpnRoot, value);
	}

	public FolioOffset FfoRoot
	{
		readonly get => Read(_ffoRoot);
		set => Write(out _ffoRoot, value);
	}

	public FolioOffset FlpnRoot
	{
		readonly get => Read(_flpnRoot);
		set => Write(out _flpnRoot, value);
	}

	public FolioOffset FreelistHead
	{
		readonly get => Read(_freelistHead);
		set => Write(out _freelistHead, value);
	}

	public uint FfoCount
	{
		readonly get => Read(_ffoCount);
		set => Write(out _ffoCount, value);
	}

	public uint FolioCount
	{
		readonly get => Read(_folioCount);
		set => Write(out _folioCount, value);
	}

	public LogicalPageNumber LargestAllocatedLpn
	{
		readonly get => Read(_largestAllocatedLpn);
		set => Write(out _largestAllocatedLpn, value);
	}

	public readonly ReadOnlySpan<byte> UserDatabaseHeaderReadOnly() => MemoryMarshal.CreateReadOnlySpan(in UnsafeAsReadonly<Padding4096, byte>(in _userDatabaseHeader), UserDatabaseHeaderSize);
	public Span<byte> UserDatabaseHeader() => MemoryMarshal.CreateSpan(ref Unsafe.As<Padding4096, byte>(ref _userDatabaseHeader), UserDatabaseHeaderSize);

	public const int UserDatabaseHeaderSize = 4096;
	private const uint ExpectedMagic = 0x415243DB; // 'A' 'R' 'C' 0xDB

	[InlineArray(232)]
	private struct Padding232
	{
		private byte _byte;
	}

	[InlineArray(3768)]
	private struct Padding3768
	{
		private byte _byte;
	}

	[InlineArray(4096)]
	private struct Padding4096
	{
		private byte _byte;
	}
}
