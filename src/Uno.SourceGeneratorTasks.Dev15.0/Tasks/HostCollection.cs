using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.SourceGeneration.Host.GenerationServer
{
	public class HostCollection
	{
		private readonly DateTime _hostOwnerFileTimeStamp;
		private readonly DateTime[] _analyzersTimeStamps;
		private readonly DomainEntry _entry;

		public ConcurrentBag<(string Wrapper, AppDomain Domain)> Hosts { get; } = new ConcurrentBag<(string, AppDomain)>();

		public HostCollection(DomainEntry entry)
		{
			_entry = entry;
			_hostOwnerFileTimeStamp = File.GetLastWriteTime(entry.OwnerFile);
			_analyzersTimeStamps = entry.Analyzers.Select(e => File.GetLastWriteTime(e)).ToArray();
		}

		public bool IsInvalid =>
			File.GetLastWriteTime(Entry.OwnerFile) != _hostOwnerFileTimeStamp
			|| !Entry.Analyzers.Select(e => File.GetLastWriteTime(e)).SequenceEqual(_analyzersTimeStamps);

		public DomainEntry Entry => _entry;
	}

	public class DomainEntry
	{
		public DomainEntry(string ownerFile, string platform, string[] analyzers)
		{
			OwnerFile = ownerFile;
			Analyzers = analyzers;
			Platform = platform;
		}

		public string[] Analyzers { get; }

		public string OwnerFile { get; }

		/// <remarks>
		/// Platform segregation is required as domains may load assemblies that are 
		/// not redirected the same way. (e.g. Xamarin.Android vs. Xamarin.iOS).
		/// </remarks>
		public string Platform { get; }

		public override bool Equals(object o)
			=> o is DomainEntry other
			&& OwnerFile == other.OwnerFile
			&& Platform == other.Platform
			&& Analyzers.SequenceEqual(other.Analyzers);

		public override int GetHashCode() => OwnerFile.GetHashCode() ^ Platform.GetHashCode();
	}
}
