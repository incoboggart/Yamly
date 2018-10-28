using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Yamly.Proxy;

namespace Yamly.UnityEditor
{
    internal partial class StorageGenerator
        : ProxyCodeGenerator
    {
        public Assembly[] TargetAssemblies { get; set; }

        public string OutputNamespace { get; set; } = "Yamly.UnityEngine.Generated";

        private readonly TypesFilter _typesFilter = new TypesFilter();

        private void Initialize()
        {
            var roots = _typesFilter.GetApplicableTypes(TargetAssemblies).ToList();
            var namespaces = new List<string>();
            foreach (var root in roots)
            {
                namespaces.Add(_typesFilter.GetProxyNamespaceName(root.Root));
                namespaces.Add(root.Root.Namespace);
                var keyType = GetKeyType(root.Root);
                if (keyType != null)
                {
                    namespaces.Add(keyType.Namespace);
                }
            }

            WriteUsings(namespaces.Distinct());

            WriteNamespace(OutputNamespace, () =>
            {
                var generated = new HashSet<string>();
                foreach (var root in roots)
                {
                    var storageTypeName = GetStorageTypeName(root);
                    if (generated.Contains(storageTypeName))
                    {
                        continue;
                    }

                    WriteStorageType(root);

                    generated.Add(storageTypeName);
                }
            });
        }

        private string GetStorageTypeName(RootDefinition root)
        {
            return _typesFilter.GetStorageTypeName(root);
        }

        private MethodInfo GetKeySourceMethodInfo(Type rootType)
        {
            var attribute = rootType.GetSingle<ConfigDictionary>();
            if (attribute == null)
            {
                return default(MethodInfo);
            }

            var keySourceType = attribute.KeySourceType ?? rootType;
            foreach (var methodInfo in keySourceType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                var methodAttribute = methodInfo.GetSingle<ConfigDictionaryKeySource>();
                if (methodAttribute == null)
                {
                    continue;
                }

                var parameters = methodInfo.GetParameters();
                if (parameters.Length < 1 &&
                    parameters.Length > 2)
                {
                    continue;
                }

                var isValid = true;
                foreach (var parameterInfo in parameters)
                {
                    if (parameterInfo.ParameterType != rootType
                        && parameterInfo.ParameterType != typeof(string))
                    {
                        isValid = false;
                    }
                }

                if (!isValid)
                {
                    continue;
                }

                return methodInfo;
            }

            return default(MethodInfo);
        }

        private Type GetKeyType(Type rootType)
        {
            return GetKeySourceMethodInfo(rootType)
                ?.ReturnType;
        }
    }
}