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
using System.Text;
using System.Threading.Tasks;

namespace Uno.SourceGeneratorTasks
{
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
            &&Analyzers.SequenceEqual(other.Analyzers) ;

		public override int GetHashCode() => OwnerFile.GetHashCode() ^ Platform.GetHashCode();
	}
}
