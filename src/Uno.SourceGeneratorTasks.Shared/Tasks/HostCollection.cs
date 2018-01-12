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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.SourceGeneratorTasks
{

	public class HostCollection
	{
		private readonly DateTime _hostOwnerFileTimeStamp;
		private readonly DateTime[] _analyzersTimeStamps;
		private readonly DomainEntry _entry;

		public ConcurrentBag<(SourceGeneratorHostWrapper Wrapper, AppDomain Domain)> Hosts { get; } = new ConcurrentBag<(SourceGeneratorHostWrapper, AppDomain)>();

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
}
