/*
 * Copyright (c) 2026 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Tizen.Applications
{
    internal class TeamAssemblyLoadContext : AssemblyLoadContext
    {
        public TeamAssemblyLoadContext() : base(isCollectible: true) { }

        protected override Assembly Load(AssemblyName name) => null;
    }

    internal class AssemblyInfo
    {
        public Assembly Assembly { get; }
        public string Path { get; }
        public WeakReference LoadContextRef { get; }

        public AssemblyInfo(Assembly assembly, string path, WeakReference contextRef)
        {
            Assembly = assembly;
            Path = path;
            LoadContextRef = contextRef;
        }
    }

    public static class TeamManager
    {
        private const string LogTag = "DN_TAM";
        private static readonly Dictionary<IntPtr, AssemblyInfo> _assemblies = new Dictionary<IntPtr, AssemblyInfo>();
        private static readonly Dictionary<string, IntPtr> _assembliesByPath = new Dictionary<string, IntPtr>();
        private static readonly Dictionary<IntPtr, IntPtr> _argHandles = new Dictionary<IntPtr, IntPtr>();
        private static readonly object _lock = new object();
        private static int _assemblyId = 1;

        internal static IntPtr RegisterAssemblyInfo(AssemblyInfo info)
        {
            lock (_lock)
            {
                IntPtr id = new IntPtr(_assemblyId);
                _assemblies[id] = info;
                _assembliesByPath[info.Path] = id;

                Log.Info(LogTag, $"Assembly registered - ID: {_assemblyId}, Path: {info.Path}");

                _assemblyId++;
                return id;
            }
        }

        internal static void UnregisterAssembly(IntPtr id)
        {
            lock (_lock)
            {
                if (_assemblies.TryGetValue(id, out var info))
                {
                    _assembliesByPath.Remove(info.Path);
                }
                _assemblies.Remove(id);
            }
        }

        internal static AssemblyInfo GetAssembly(IntPtr id)
        {
            lock (_lock)
            {
                if (_assemblies.TryGetValue(id, out var info))
                {
                    return info;
                }
            }
            return null;
        }

        public static void Init(string[] args)
        {
            TeamLoop.Run(args);
        }

        public static bool IsInit()
        {
            return TeamLoop.IsRunning();
        }

        internal static IntPtr GetAssemblyIdByPath(string path)
        {
            lock (_lock)
            {
                if (_assembliesByPath.TryGetValue(path, out var id))
                {
                    return id;
                }
            }
            return IntPtr.Zero;
        }

        internal static void RegisterArgHandle(IntPtr id, IntPtr argHandle)
        {
            lock (_lock)
            {
                _argHandles[id] = argHandle;
                Log.Info(LogTag, $"ArgHandle registered - ID: {id}, ArgHandle: {argHandle}");
            }
        }

        internal static void UnregisterArgHandle(IntPtr id)
        {
            lock (_lock)
            {
                _argHandles.Remove(id);
                Log.Info(LogTag, $"ArgHandle unregistered - ID: {id}");
            }
        }

        internal static IntPtr GetArgHandle(IntPtr id)
        {
            lock (_lock)
            {
                if (_argHandles.TryGetValue(id, out var argHandle))
                {
                    return argHandle;
                }
            }
            return IntPtr.Zero;
        }
    }
}
