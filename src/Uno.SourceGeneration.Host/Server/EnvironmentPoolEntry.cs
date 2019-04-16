#if NETFRAMEWORK

using System.Linq;

namespace Uno.SourceGeneration.Host.Server
{
	public class EnvironmentPoolEntry
	{
		public EnvironmentPoolEntry(string ownerFile, string platform, string[] analyzers)
		{
			ProjectFile = ownerFile;
			SourceGenerators = analyzers;
			Platform = platform;
		}

		public string[] SourceGenerators { get; }

		public string ProjectFile { get; }

		/// <remarks>
		/// Platform segregation is required as domains may load assemblies that are 
		/// not redirected the same way. (e.g. Xamarin.Android vs. Xamarin.iOS).
		/// </remarks>
		public string Platform { get; }

		public override bool Equals(object o)
			=> o is EnvironmentPoolEntry other
			&& ProjectFile == other.ProjectFile
			&& Platform == other.Platform
			&& SourceGenerators.SequenceEqual(other.SourceGenerators);

		public override int GetHashCode() => ProjectFile.GetHashCode() ^ Platform.GetHashCode();
	}
}
#endif
