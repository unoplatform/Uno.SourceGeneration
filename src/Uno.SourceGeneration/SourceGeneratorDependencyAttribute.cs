using System;

namespace Uno.SourceGeneration
{
	/// <summary>
	/// Define a dependency between source generators
	/// </summary>
	/// <remarks>
	/// Can be define more than once for a generator.
	/// No effect when generator is not found.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class SourceGeneratorDependencyAttribute : Attribute
	{
		/// <summary>
		/// Fully Qualified Name (FQN: namespace + class name) of the generator to execute before.
		/// </summary>
		/// <remarks>
		/// No effect if the generator is not found.
		/// </remarks>
		public string DependsOn { get; }

		public SourceGeneratorDependencyAttribute(string dependsOn)
		{
			DependsOn = dependsOn;
		}
	}
}
