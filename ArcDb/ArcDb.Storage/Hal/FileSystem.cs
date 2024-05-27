using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcDb.Storage.Hal;

public interface IFileSystem
{
	IFile OpenOrCreateFile(string path);
}

public static class PhysicalFileSystem
{
	public static IFileSystem Instance { get; } = new FileSystem();
}

internal sealed class FileSystem : IFileSystem
{
	public IFile OpenOrCreateFile(string path)
	{
		return File.OpenOrCreate(path);
	}
}