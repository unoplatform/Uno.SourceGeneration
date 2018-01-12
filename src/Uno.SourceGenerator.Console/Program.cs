// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host;

namespace Uno.SourceGeneratorTasks.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			//var path = Path.GetFullPath(@"..\..\..\Uno.SourceGeneratorTasks.Dev15.0\bin\Debug\Uno.SourceGeneratorTasks.v0.dll");
			// var asm = Assembly.LoadFile(path);

			Run();
		}

		private static void Run()
		{
			// var generator = Build();
			var generator = new SourceGeneratorHostWrapper();
			var output = generator.Generate(
				logger: null,
				environment: new BuildEnvironment(
					configuration: "Debug",
					platform: "x86",
					projectFile: @"C:\s\TuneInWin10\TuneIn.Core.Uwa\TuneIn.Core.Uwa.csproj",
					outputPath: @"C:\s\TuneInWin10\TuneIn.Core.Uwa\obj\g\test",
					targetFramework: null,
					visualStudioVersion: "15.0",
					targetFrameworkRootPath: Path.GetDirectoryName(new Uri(typeof(Microsoft.Build.Logging.ConsoleLogger).Assembly.CodeBase).LocalPath)
				)
			);

            System.Console.WriteLine(string.Join(", ", output));
        }

		private static SourceGeneratorHostWrapper Build()
		{
			var wrapperBasePath = Path.GetDirectoryName(new Uri(typeof(SourceGeneratorHostWrapper).Assembly.CodeBase).LocalPath);

			// We can create an app domain per OwnerFile and all Analyzers files
			// so that if those change, we can spin off another one, and still avoid
			// locking these assemblies.
			//
			// If the domain exists, keep it and continue generating content with it.

			var setup = new AppDomainSetup();
			setup.ApplicationBase = @"C:\Program Files (x86)\MSBuild\14.0\Bin";
			setup.ShadowCopyFiles = "true";
			setup.ShadowCopyDirectories = wrapperBasePath;
			setup.PrivateBinPath = setup.ShadowCopyDirectories;
			setup.LoaderOptimization = LoaderOptimization.SingleDomain;
			setup.ConfigurationFile = Path.Combine(wrapperBasePath, typeof(Program).Assembly.GetName().Name + ".exe.config");

			var domain = AppDomain.CreateDomain("Generators-" + Guid.NewGuid(), null, setup);

			return domain.CreateInstanceFromAndUnwrap(
				typeof(SourceGeneratorHostWrapper).Assembly.CodeBase,
				typeof(SourceGeneratorHostWrapper).FullName
			) as SourceGeneratorHostWrapper;

		}
	}
}
