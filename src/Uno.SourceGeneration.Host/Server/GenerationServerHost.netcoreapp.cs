#if NETCOREAPP
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Uno.SourceGeneration.Helpers;
using Uno.SourceGeneration.Host.Messages;
using Uno.SourceGeneratorTasks;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host.Server
{
	internal abstract partial class GenerationServerHost : IGenerationServerHost
	{
		private string[] Generate(BuildEnvironment environment)
		{
			var host = new SourceGeneratorEngine(environment, AssemblyReferenceProvider);

			var generatedFiles = host.Generate();
			return generatedFiles;
		}
	}
}
#endif
