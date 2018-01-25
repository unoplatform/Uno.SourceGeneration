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
using System.Linq;

namespace Uno.SourceGeneration.host.Helpers
{
	internal static class TopologicalSorting
	{
		// Custom implementation of a "Grouped Topological Sorting",
		// using a "Dept-first search" (DFS) algorithm https://en.wikipedia.org/wiki/Depth-first_search modified for grouping
		// Code is inspired by https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
		internal static IReadOnlyCollection<IReadOnlyCollection<T>> GroupSort<T>(
			this IReadOnlyCollection<T> nodes,
			IReadOnlyCollection<(T incoming, T outgoing)> edges,
			IEqualityComparer<T> comparer = null)
		{
			var dependencyGraph = nodes.ToDictionary(
				e => e,
				e => edges.Where(edge => edge.incoming.Equals(e)).Select(edge => edge.outgoing).ToList()
			);

			comparer = comparer ?? EqualityComparer<T>.Default;

			var sorted = new List<List<T>>();
			var visited = new Dictionary<T, int>(comparer);

			int Visit(T node)
			{
				const int inProcess = -1;

				var alreadyVisited = visited.TryGetValue(node, out var level);
				if (!alreadyVisited)
				{
					visited[node] = (level = inProcess);

					foreach (var dependency in dependencyGraph[node])
					{
						var depLevel = Visit(dependency);
						if (depLevel == inProcess)
						{
							return depLevel; // Cyclic dependency detected in recursive call
						}

						level = Math.Max(level, depLevel);
					}

					visited[node] = ++level;
					while (sorted.Count <= level)
					{
						sorted.Add(new List<T>());
					}
					sorted[level].Add(node);
				}

				return level;
			}

			foreach (var node in nodes)
			{
				if (Visit(node) == -1)
				{
					return null; // cyclic dependency detected
				}
			}

			return sorted;
		}
	}
}
