using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.SourceGeneratorTasks.Shared.Helpers
{
	[Serializable]
	public class CodeSpan
	{
		public string FileName { get; private set; }
		public int StartLineNumber { get; private set; }
		public int StartColumn { get; private set; }
		public int EndLineNumber { get; private set; }
		public int EndColumn { get; private set; }

		public static readonly CodeSpan Empty = new CodeSpan();

		public static CodeSpan FromLocation(Location location)
		{
			if (!location.IsInSource)
			{
				return Empty;
			}

			var position = location.GetMappedLineSpan();

			if (!position.IsValid)
			{
				return Empty;
			}

			return new CodeSpan
			{
				FileName = position.Path,
				StartLineNumber = position.StartLinePosition.Line,
				StartColumn = position.StartLinePosition.Character,
				EndLineNumber = position.EndLinePosition.Line,
				EndColumn = position.EndLinePosition.Character
			};
		}
	}
}
