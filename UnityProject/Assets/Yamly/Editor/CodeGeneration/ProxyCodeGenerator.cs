// Copyright (c) 2018 Alexander Bogomoletz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Yamly.CodeGeneration
{
    internal class ProxyCodeGenerator
        : CodeGeneratorBase
    {
        /// <summary>
        /// Assemblies to analyze
        /// </summary>
        public Assembly[] TargetAssemblies { get; set; }

        /// <summary>
        /// Roots in this assemlies will be ignored
        /// </summary>
        public Assembly[] IgnoreAssemblies { get; set; }

        public Assembly[] ReferencedAssemblies { get; private set; }

        public RootDefinition[] RootDefinitions { get; private set; }

        public IEnumerable<RootDefinition> GetRootDefinitions()
        {
            return GetRootDefinitions(TargetAssemblies);
        }

        protected override void Generate(StringBuilder sourceCode)
        {
            if (IgnoreAssemblies == null)
            {
                IgnoreAssemblies = new Assembly[0];
            }
            var roots = GetRootDefinitions()
                .Where(r => r.Types.Length >= 1)
                .Where(r => IgnoreAssemblies.All(a => r.Assembly != a)).ToList();

            foreach (var root in roots)
            {
                Log(root.ToString());
            }

            var namespaces = new List<string>
            {
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Runtime.InteropServices",

                "Yamly",
                "Yamly.Proxy",

                "UnityEngine"
            };
            foreach (var root in roots)
            {
                namespaces.Add(GetProxyNamespaceName(root.Root));
                namespaces.AddRange(root.Namespaces);
                foreach (var attribute in root.Attributes)
                {
                    var dictionaryAttribute = attribute as AssetDictionaryAttribute;
                    if (dictionaryAttribute == null)
                    {
                        continue;
                    }

                    var keyType = GetKeyType(root.Root, dictionaryAttribute);
                    if (keyType != null && keyType.Namespace != null)
                    {
                        namespaces.Add(keyType.Namespace);
                    }
                }
            }

            Include(namespaces.Distinct());

            foreach (var root in roots)
            {
                Namespace(GetProxyNamespaceName(root.Root), () =>
                {
                    ListProxyGenericArguments.Clear();
                    DictionaryProxyGenericArguments.Clear();
                    NullableProxyGenericArguments.Clear();

                    foreach (var type in root.Types)
                    {
                        ProxyType(type);
                    }

                    foreach (var listTypeName in ListProxyGenericArguments)
                    {
                        ListType(listTypeName);
                    }

                    foreach (var pair in DictionaryProxyGenericArguments)
                    {
                        DictionaryType(pair.Key, pair.Value);
                    }

                    foreach (var valueTypeName in NullableProxyGenericArguments)
                    {
                        NullableType(valueTypeName);
                    }
                });
            }

            Namespace(StorageOutputNamespace, () =>
            {
                ListProxyGenericArguments.Clear();
                DictionaryProxyGenericArguments.Clear();
                NullableProxyGenericArguments.Clear();

                var generated = new HashSet<string>();
                foreach (var root in roots)
                {
                    foreach (var attribute in root.Attributes)
                    {
                        if (!CodeGenerationUtility.IsValidGroupName(attribute.Group))
                        {
                            continue;
                        }

                        var storageTypeName = GetStorageTypeName(root.Root, attribute);
                        if (generated.Contains(storageTypeName))
                        {
                            continue;
                        }

                        StorageType(root, attribute);

                        generated.Add(storageTypeName);
                    }
                }
            });

            ReferencedAssemblies = roots.SelectMany(r => r.Types)
                .Select(t => t.Assembly)
                .Distinct()
                .ToArray();

            RootDefinitions = roots.ToArray();
        }

        public new string GetTypeName(Type type, bool isProxy = false)
        {
            return base.GetTypeName(type, isProxy);
        }
    }
}