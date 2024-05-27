using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace ArcDb.Storage;

public static class OverlayHelpers
{
	public static uint Read(uint bigEndian) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(bigEndian) : bigEndian;
	public static void Write(out uint bigEndian, uint value) => bigEndian = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
	public static ulong Read(ulong bigEndian) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(bigEndian) : bigEndian;
	public static void Write(out ulong bigEndian, ulong value) => bigEndian = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

	public static ref readonly TTo UnsafeAsReadonly<TFrom, TTo>(ref readonly TFrom from) => ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in from));
}
