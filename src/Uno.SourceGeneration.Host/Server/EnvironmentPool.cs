#if NETFRAMEWORK
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Uno.SourceGeneratorTasks;

namespace Uno.SourceGeneration.Host.Server
{
	class EnvironmentPool
	{
		private readonly DateTime _hostOwnerFileTimeStamp;
		private readonly DateTime[] _analyzersTimeStamps;

		public ConcurrentBag<(RemoteSourceGeneratorEngine Engine, AppDomain Domain)> Hosts { get; }
			= new ConcurrentBag<(RemoteSourceGeneratorEngine, AppDomain)>();

		public EnvironmentPool(EnvironmentPoolEntry entry)
		{
			Entry = entry;
			_hostOwnerFileTimeStamp = File.GetLastWriteTime(entry.ProjectFile);
			_analyzersTimeStamps = entry.SourceGenerators.Select(e => File.GetLastWriteTime(e)).ToArray();
		}

		public bool IsInvalid =>
			File.GetLastWriteTime(Entry.ProjectFile) != _hostOwnerFileTimeStamp
			|| !Entry.SourceGenerators.Select(e => File.GetLastWriteTime(e)).SequenceEqual(_analyzersTimeStamps);

		internal EnvironmentPoolEntry Entry { get; }
	}
}
#endif
