using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

_ = BenchmarkRunner.Run<Benchmarks>();


public class Benchmarks
{
	const int N = 1000;
	private int[] data;
	
	public Benchmarks()
	{
		data = new int[N];
		for (int i = 0; i < N; i++)
			data[i] = Random.Shared.Next();
	}

	[Benchmark]
	public int Branch()
	{
		var result = 0;
		for (int i = 0; i < N; i++)
			result += BitConverter.IsLittleEndian? BinaryPrimitives.ReverseEndianness(data[i]) : data[i];
		return result;
	}

	[Benchmark]
	public int OldSchool()
	{
		var result = 0;
		for (int i = 0; i < N; i++)
			result += BinaryPrimitives.ReadInt32BigEndian(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data[i], 1)));
		return result;
	}

	[Benchmark]
	public int NewConstructor()
	{
		var result = 0;
		for (int i = 0; i < N; i++)
			result += BinaryPrimitives.ReadInt32BigEndian(MemoryMarshal.AsBytes(new ReadOnlySpan<int>(in data[i])));
		return result;
	}

	[Benchmark]
	public unsafe int Unsafe1()
	{
		var result = 0;
		for (int i = 0; i < N; i++)
			result += BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref data[i]), sizeof(int)));
		return result;
	}
}