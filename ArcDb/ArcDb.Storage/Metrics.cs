using System.Diagnostics.Metrics;

namespace ArcDb.Storage;

internal sealed class Metrics
{
	public Metrics(IMeterFactory meterFactory)
	{
		// TODO: Set tag for database file.
		var meter = meterFactory.Create(new MeterOptions(typeof(Metrics).Assembly.GetName().Name!)
		{
			Version = typeof(Metrics).Assembly.GetName().Version!.ToString(),
		});
		FoliosRead = meter.CreateCounter<int>("arcdb.storage.folios_read", "{folios}", "The number of 8192-byte units read from disk.");
		FoliosWritten = meter.CreateCounter<int>("arcdb.storage.folios_written", "{folios}", "The number of 8192-byte units written to disk.");
	}

	public Counter<int> FoliosRead { get; }
	public Counter<int> FoliosWritten { get; }
}
