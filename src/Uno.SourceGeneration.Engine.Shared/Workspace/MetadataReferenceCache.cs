// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Uno.SourceGeneration.Engine.Workspace
{
    /// <summary>
    /// A cache for metadata references.
    /// </summary>
    internal class MetadataReferenceCache
    {
        private ImmutableDictionary<string, ReferenceSet> _referenceSets
            = ImmutableDictionary<string, ReferenceSet>.Empty;

        private readonly Func<string, MetadataReferenceProperties, MetadataReference> _createReference;

		public static MetadataReferenceCache Default { get; } = new MetadataReferenceCache(null);

        public MetadataReferenceCache(Func<string, MetadataReferenceProperties, MetadataReference> createReference = null)
        {
            _createReference = createReference ?? DefaultCreateReference;
        }

		private static MetadataReference DefaultCreateReference(string path, MetadataReferenceProperties properties)
			=> MetadataReference.CreateFromFile(path, properties);

        public MetadataReference GetReference(string path, MetadataReferenceProperties properties)
        {
            if (!_referenceSets.TryGetValue(path, out var referenceSet))
            {
                referenceSet = ImmutableInterlocked.GetOrAdd(ref _referenceSets, path, new ReferenceSet(this));
            }

            return referenceSet.GetAddOrUpdate(path, properties);
        }

        /// <summary>
        /// A collection of references to the same underlying metadata, each with different properties.
        /// </summary>
        private class ReferenceSet
        {
            private readonly MetadataReferenceCache _cache;

            private readonly object _gate = new object();

            // metadata references are held weakly, so even though this is a cache that enables reuse, it does not control lifetime.
            private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>> _references
                = new Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>>();

            public ReferenceSet(MetadataReferenceCache cache)
            {
                _cache = cache;
            }

            public MetadataReference GetAddOrUpdate(string path, MetadataReferenceProperties properties)
            {
                lock (_gate)
                {
                    MetadataReference mref = null;
                    if (!(_references.TryGetValue(properties, out var weakref) && weakref.TryGetTarget(out mref)))
                    {
                        // try to base this metadata reference off of an existing one, so we don't load the metadata bytes twice.
                        foreach (var wr in _references.Values)
                        {
                            if (wr.TryGetTarget(out mref))
                            {
                                mref = mref.WithProperties(properties);
                                break;
                            }
                        }

                        if (mref == null)
                        {
                            mref = _cache._createReference(path, properties);
                        }

                        _references[properties] = new WeakReference<MetadataReference>(mref);
                    }

                    return mref;
                }
            }
        }
    }
}
