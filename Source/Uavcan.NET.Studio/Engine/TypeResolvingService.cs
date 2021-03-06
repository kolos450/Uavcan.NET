﻿using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Uavcan.NET.Studio.Engine
{
    [Export]
    sealed class TypeResolvingService : IUavcanTypeResolver
    {
        public IUavcanType ResolveType(string ns, string typeName)
        {
            return TryResolveType(ns, typeName) ??
                throw new InvalidOperationException($"Cannot resolve type '{ns}.{typeName}'.");
        }

        public IUavcanType ResolveType(int dataTypeId, UavcanTypeKind typeKind)
        {
            return TryResolveType(dataTypeId, typeKind) ??
                throw new InvalidOperationException($"Cannot resolve type with ID = {dataTypeId}.");
        }

        [ImportMany]
        IEnumerable<IDsdlDefinitionsDirectoryProvider> _directoryProviders = null;

        IEnumerable<IUavcanTypeResolver> _resolvers;
        bool _initialized = false;
        object _syncRoot = new object();

        void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_syncRoot)
            {
                if (_initialized)
                    return;

                _resolvers = _directoryProviders
                    .SelectMany(x => x.Directories)
                    .Select(x => new FileSystemUavcanTypeResolver(x))
                    .ToList();

                _initialized = true;
            }
        }

        public IUavcanType TryResolveType(string ns, string typeName)
        {
            EnsureInitialized();

            foreach (var resolver in _resolvers)
            {
                var result = resolver.TryResolveType(ns, typeName);
                if (result != null)
                    return result;
            }

            return null;
        }

        public IUavcanType TryResolveType(int dataTypeId, UavcanTypeKind typeKind)
        {
            EnsureInitialized();

            foreach (var resolver in _resolvers)
            {
                var result = resolver.TryResolveType(dataTypeId, typeKind);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
