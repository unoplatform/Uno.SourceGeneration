using System;
using System.Diagnostics;
using Uno.SourceGeneration;

namespace MyGenerator
{
	public class MyCustomSourceGenerator : SourceGenerator
	{
		public override void Execute(SourceGeneratorContext context)
		{
			Debugger.Launch();

			var project = context.GetProjectInstance();

			context.AddCompilationUnit("Test", "namespace MyGeneratedCode { class TestGeneration { } }");
		}
	}
}