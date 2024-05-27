using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static ArcDb.Storage.OverlayHelpers;

namespace ArcDb.Storage;

public struct MetadataFolioLayout
{
	public HeaderLayout Header;
	private FolioDataLayout _data; // offset divisible by 8
	public FooterLayout Footer;

	public readonly bool ValidateHash()
	{
		ReadOnlySpan<byte> headerHash = Header.Hash, footerHash = Footer.Hash;
		Span<byte> calculatedHash = stackalloc byte[Sha1HashSize];
		SHA1.HashData(HashedBytesSpan(), calculatedHash);
		return headerHash.SequenceEqual(calculatedHash) && footerHash.SequenceEqual(calculatedHash);
	}

	public void RecalculateHash()
	{
		Span<byte> headerHash = Header.Hash, footerHash = Footer.Hash;
		SHA1.HashData(HashedBytesSpan(), headerHash);
		headerHash.CopyTo(footerHash);
	}

	[UnscopedRef]
	public ref T DataAs<T>() where T : struct => ref Unsafe.As<FolioDataLayout, T>(ref _data);

	[UnscopedRef]
	public readonly ref readonly T DataAsReadonly<T>() where T : struct => ref UnsafeAsReadonly<FolioDataLayout, T>(in _data);

	public struct FooterLayout
	{
		private Padding4 _reserved;
		public Hash Hash;
	}

	public struct HeaderLayout
	{
		public Hash Hash;
		public Type Type;
		private Padding3 _reserved;
	}

	[InlineArray(Sha1HashSize)]
	public struct Hash
	{
		private byte _byte;
	}

	private readonly ReadOnlySpan<byte> HashedBytesSpan() =>
		MemoryMarshal.CreateReadOnlySpan(in UnsafeAsReadonly<Type, byte>(in Header.Type), FolioSize - Sha1HashSize - Sha1HashSize);

	public enum Type : byte
	{
		DatabaseHeader = 0,
		LpnFoNode = 1,
		FoLpnNode = 2,
		FfoNode = 3,
		FlpnNode = 4,
		Freelist = 5,
	}

	[InlineArray(3)]
	private struct Padding3
	{
		private byte _byte;
	}

	[InlineArray(4)]
	private struct Padding4
	{
		private byte _byte;
	}

	[InlineArray(FolioDataSize)]
	private struct FolioDataLayout
	{
		private byte _byte;
	}

	public const int FolioSize = 8 * 1024;
	public const int FolioDataSize = FolioSize - 24 - 24;
	private const int Sha1HashSize = 20;
}
