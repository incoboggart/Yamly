using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Yamly.Proxy;
using Yamly.UnityEditor;

namespace Yamly.CodeGeneration
{
    internal sealed class RootDefinitonsProvider
        : IEnumerable<RootDefinition>
    {
        private const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Public;
        
        public string[] IgnoreAttributes { get; set; }

        public Type[] IgnoreAttributeTypes { get; set; }
        
        public List<RootDefinition> All { get; private set; }
        
        public List<RootDefinition> Valid { get; private set; }
        
        public int Count => Valid.Count;

        public RootDefinition Find(Predicate<RootDefinition> match)
        {
            return Valid.Find(match);
        }

        public RootDefinitonsProvider Init(YamlyAssembliesProvider assemblies)
        {
            All = GetRootDefinitions(assemblies.All.Except(assemblies.IgnoreAssemblies)).ToList();

            var validAssemblies = assemblies.All
                .Except(assemblies.IgnoreAssemblies)
                .Except(new[]
                {
                    assemblies.MainRuntimeAssembly,
                    assemblies.MainEditorAssembly,
                    assemblies.ProxyAssembly
                }).ToList();

            Valid = All.Where(d => validAssemblies.Contains(d.Root.Assembly)).ToList();
            
            return this;
        }
        
        public IEnumerable<RootDefinition> GetRootDefinitions(IEnumerable<Assembly> targetAssemblies)
        {
            var types = new List<Type>();
            foreach (var assembly in targetAssemblies)
            {
                foreach (var type in assembly.GetTypes()
                    .Where(IsRootApplicable))
                {
                    types.Clear();
                    types.Add(type);
                    types.AddRange(type.GetProperties(PropertyFlags)
                        .Where(IsApplicable)
                        .SelectMany(p => GetApplicableTypes(p.PropertyType))
                        .Distinct());
                    
                    yield return new RootDefinition
                    {
                        Root = type,
                        Attributes = type.Get<AssetDeclarationAttributeBase>().ToList(),
                        Types = types.ToArray(),
                        Namespaces = new[]
                            {
                                typeof(NullableProxy<>).Namespace,
                                typeof(ListProxy<>).Namespace
                            }.Concat(types.Select(t => t.Namespace).Where(s => !string.IsNullOrEmpty(s)))
                            .Distinct()
                            .ToArray()
                    };
                }
            }
        }

        public IEnumerable<Type> GetApplicableTypes(Type propertyType)
        {
            if (propertyType.IsNative())
            {
                yield break;
            }

            if (propertyType.IsNullableType())
            {
                yield break;
            }

            if (propertyType.IsArray ||
                propertyType.IsListType() ||
                propertyType.IsDictionaryType())
            {
                var genericArguments = propertyType.GetGenericArguments();
                foreach (var genericArgument in genericArguments)
                {
                    foreach (var t in GetApplicableTypes(genericArgument))
                    {
                        yield return t;
                    }
                }

                yield break;
            }

            if (!IsPropertyApplicable(propertyType))
            {
                yield break;
            }

            yield return propertyType;

            foreach (var property in GetApplicableProperties(propertyType))
            {
                foreach (var t in GetApplicableTypes(property.PropertyType))
                {
                    yield return t;
                }
            }
        }

        public IEnumerable<PropertyInfo> GetApplicableProperties(Type type)
        {
            return type.GetProperties(PropertyFlags)
                .Where(IsApplicable);
        }
        
        public bool IsApplicable(PropertyInfo p)
        {
            return p.CanRead
                   && p.CanWrite
                   && IsPropertyApplicable(p.PropertyType)
                   && !IsIgnored(p);
        }

        public bool IsIgnored(ICustomAttributeProvider provider)
        {
            return provider.GetCustomAttributes(true)
                .Any(a =>
                {
                    var t = a.GetType();
                    if (IgnoreAttributes != null &&
                        IgnoreAttributes.Any(s => s == t.Name || s == t.FullName))
                    {
                        return true;
                    }

                    if (IgnoreAttributeTypes != null &&
                        IgnoreAttributeTypes.Any(it => t == it))
                    {
                        return true;
                    }

                    return false;
                });
        }

        public bool IsPropertyApplicable(Type type)
        {
            if (type.IsInterface ||
                type.IsAbstract)
            {
                return false;
            }

            if (IsIgnored(type))
            {
                return false;
            }

            if (type.IsEnum
                || type.IsNative())
            {
                return true;
            }

            return true;
        }

        public bool IsRootApplicable(Type type)
        {
            if (type.IsInterface ||
                type.IsAbstract)
            {
                return false;
            }

            if (IsIgnored(type))
            {
                return false;
            }

            return type.Have<AssetDeclarationAttributeBase>(false);
        }

        IEnumerator<RootDefinition> IEnumerable<RootDefinition>.GetEnumerator()
        {
            return Valid.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Valid.GetEnumerator();
        }
    }
}