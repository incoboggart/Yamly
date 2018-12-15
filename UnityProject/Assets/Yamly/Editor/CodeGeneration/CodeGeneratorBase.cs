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

using Yamly.Proxy;

namespace Yamly.CodeGeneration
{
    internal abstract class CodeGeneratorBase
    {
        private readonly Dictionary<Type, Type> _needsUpcast = new Dictionary<Type, Type>
        {
            {typeof(uint), typeof(long)},
            {typeof(ushort), typeof(int)},
            {typeof(byte), typeof(int)},
            {typeof(sbyte), typeof(int)}
        };
        
        public RootDefinitonsProvider Roots { get; private set; }

        public string StorageOutputNamespace { get; set; } = "Yamly.Generated.Storages";
        public string ProxyOutputNamespace { get; set; } = "Yamly.Generated.Proxy";

        public ICollection<string> Logs { get; } = new List<string>();

        protected List<string> ListProxyGenericArguments = new List<string>();

        protected List<string> NullableProxyGenericArguments = new List<string>();

        protected List<KeyValuePair<string, string>> DictionaryProxyGenericArguments = new List<KeyValuePair<string, string>>();

        protected string GetStorageTypeName(Type rootType, AssetDeclarationAttributeBase attribute)
        {
            var groupName = CodeGenerationUtility.GetGroupName(attribute.Group);
            var typeName = GetShortTypeName(GetTypeName(rootType, false));
            return $"{groupName}{typeName}Storage";
        }

        protected string GetProxyTypeName(Type t)
        {
            return t.IsNative()
                ? GetTypeName(t, false)
                : $"{t.FullName}Proxy";
        }

        protected string GetProxyNamespaceName(Type type)
        {
            return string.IsNullOrEmpty(type.Namespace) 
                ? ProxyOutputNamespace 
                : $"{ProxyOutputNamespace}.{type.Namespace}";
        }

        protected string GetListTypeName(string elementTypeName, bool isProxy)
        {
            return isProxy
                ? $"ListProxy{GetGluedTypeName(elementTypeName)}"
                : $"List<{elementTypeName}>";
        }

        protected string GetDictionaryTypeName(string keyTypeName, string valueTypeName, bool proxy)
        {
            return proxy
                ? $"DictionaryProxy{GetGluedTypeName(keyTypeName)}{GetGluedTypeName(valueTypeName)}"
                : $"Dictionary<{keyTypeName}, {valueTypeName}>";
        }

        protected string GetTypeName(Type t, bool proxy)
        {
            if (t.IsArray)
            {
                var elementType = t.GetElementType();
                var elementTypeName = GetTypeName(elementType, proxy);
                if (proxy && !elementType.IsNative())
                {
                    elementTypeName = GetGluedTypeName(elementTypeName);
                }
                if (proxy && !ListProxyGenericArguments.Contains(elementTypeName))
                {
                    ListProxyGenericArguments.Add(elementTypeName);
                }

                return GetArrayTypeName(proxy, elementTypeName);
            }

            if (t.IsListType())
            {
                var elementType = t.GetGenericArguments()[0];
                var elementTypeName = GetTypeName(elementType, proxy);
                if (proxy && !elementType.IsNative())
                {
                    elementTypeName = GetGluedTypeName(elementTypeName);
                }
                if (proxy && !ListProxyGenericArguments.Contains(elementTypeName))
                {
                    ListProxyGenericArguments.Add(elementTypeName);
                }

                return GetListTypeName(elementTypeName, proxy);
            }

            if (t.IsDictionaryType())
            {
                var genericArguments = t.GetGenericArguments();
                var keyType = genericArguments[0];
                var valueType = genericArguments[1];

                var keyTypeName = GetTypeName(keyType, proxy);
                if (proxy && !keyType.IsNative())
                {
                    keyTypeName = GetGluedTypeName(keyTypeName);
                }

                var valueTypeName = GetTypeName(valueType, proxy);
                if (proxy && !valueType.IsNative())
                {
                    valueTypeName = GetGluedTypeName(valueTypeName);
                }

                if (proxy && !DictionaryProxyGenericArguments.Exists(p => p.Key == keyTypeName && p.Value == valueTypeName))
                {
                    DictionaryProxyGenericArguments.Add(new KeyValuePair<string, string>(keyTypeName, valueTypeName));
                }

                return GetDictionaryTypeName(keyTypeName, valueTypeName, proxy);
            }

            if (t.IsNullableType())
            {
                var valueType = t.GetGenericArguments()[0];
                var valueTypeName = GetTypeName(valueType, proxy);
                if (proxy 
                    && valueType.IsNative()
                    && !NullableProxyGenericArguments.Contains(valueTypeName))
                {
                    NullableProxyGenericArguments.Add(valueTypeName);
                }

                string result;
                if (valueType.IsNative())
                {
                    result =  proxy
                        ? $"NullableProxy{GetGluedTypeName(valueTypeName)}"
                        : $"{valueTypeName}?";
                }
                else
                {
                    result = GetTypeName(valueType, proxy);
                }

                return result;
            }

            if (proxy && _needsUpcast.ContainsKey(t))
            {
                t = _needsUpcast[t];
            }

            return proxy 
                ? GetProxyTypeName(t) 
                : $"global::{t.FullName}";
        }

        protected string GetShortTypeName(string fullTypeName)
        {
            const string separator = ".";
            const string global = "global::";
            return fullTypeName
                .Replace(global, string.Empty)
                .Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
        }

        protected string GetGluedTypeName(string fullTypeName)
        {
            return fullTypeName
                .Replace("global::", string.Empty)
                .Replace(".", "_");
        }

        private string GetArrayTypeName(bool proxy, string elementTypeName)
        {
            return proxy
                ? $"ListProxy{GetGluedTypeName(elementTypeName)}"
                : $"{elementTypeName}[]";
        }

        protected static MethodInfo GetKeySourceMethodInfo(Type rootType, AssetDictionaryAttribute attribute)
        {
            foreach (var propertyInfo in rootType.GetProperties())
            {
                if (!propertyInfo.CanRead)
                {
                    continue;
                }

                var propertyAttributes = propertyInfo.Get<DictionaryKeyAttribute>().ToArray();
                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                if (propertyAttributes.Length == 1)
                {
                    var propertyAttribute = propertyAttributes[0];
                    if (propertyAttribute.GroupName == null 
                        || propertyAttribute.GroupName == attribute.Group)
                    {
                        return propertyInfo.GetGetMethod();
                    }
                }
                else
                {
                    if (propertyAttributes.Any(a => a.GroupName == attribute.Group))
                    {
                        return propertyInfo.GetGetMethod();
                    }
                }
            }

            var keySourceType = attribute.KeySourceType ?? rootType;
            foreach (var methodInfo in keySourceType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                var methodAttribute = methodInfo.GetSingle<DictionaryKeySourceAttribute>();
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

        protected Type GetKeyType(Type rootType, AssetDictionaryAttribute attribute)
        {
            if (attribute.IsSingleFile)
            {
                return attribute.KeyType;
            }
            
            return GetKeySourceMethodInfo(rootType, attribute)
                ?.ReturnType;
        }

        protected string GetTypeConversion(Type t, 
            string propertyName, 
            bool proxy, 
            int depth, 
            string propertyPrefix)
        {
            const string Utility = nameof(ProxyUtility);

            const char a = 'a';
            if (t.IsArray)
            {
                const string Convert = nameof(ProxyUtility.ConvertArray);

                var elementType = t.GetElementType();
                var list = depth == 0
                    ? propertyPrefix + propertyName
                    : ((char)(a + (depth - 1))).ToString();
                var element = ((char)(a + depth)).ToString();
                var typeName = GetTypeName(t, proxy);
                var typeConversion = GetTypeConversion(elementType, element, proxy, depth + 1, propertyPrefix);

                return $"({typeName}){Utility}.{Convert}({list}, {element} => {typeConversion})";
            }

            if (t.IsListType())
            {
                const string Convert = nameof(ProxyUtility.Convert);

                var elementType = t.GetGenericArguments()[0];
                var list = depth == 0
                    ? propertyPrefix + propertyName
                    : ((char)(a + (depth - 1))).ToString();
                var element = ((char)(a + depth)).ToString();

                var typeName = GetTypeName(t, proxy);
                var typeConversion = GetTypeConversion(elementType, element, proxy, depth + 1, propertyPrefix);

                return $"({typeName}){Utility}.{Convert}({list}, {element} => {typeConversion})";
            }

            if (t.IsDictionaryType())
            {
                const string Convert = nameof(ProxyUtility.Convert);
                const string k = "k";
                const string v = "v";

                var genericArguments = t.GetGenericArguments();
                var keyType = genericArguments[0];
                var valueType = genericArguments[1];
                var dictionary = depth == 0
                    ? propertyPrefix + propertyName
                    : ((char)(a + (depth - 1))).ToString();
                var key = k + depth;
                var value = v + depth;

                var typeName = GetTypeName(t, proxy);
                var keyTypeConversion = GetTypeConversion(keyType, key, proxy, depth + 1, propertyPrefix);
                var valueTypeConversion = GetTypeConversion(valueType, value, proxy, depth + 1, propertyPrefix);

                return $"({typeName}){Utility}.{Convert}({dictionary}, {key} => {keyTypeConversion}, {value} => {valueTypeConversion})";
            }

            if (depth == 0 && !string.IsNullOrEmpty(propertyPrefix))
            {
                propertyName = propertyPrefix + propertyName;
            }

            {
                var typeName = GetTypeName(t, proxy);
                if (proxy && !t.IsNative())
                {
                    typeName = GetGluedTypeName(typeName);
                }
                return $"({typeName}){propertyName}";
            }
        }

        protected string GetTypeConversion(Type t,
            string propertyName,
            bool proxy)
        {
            return GetTypeConversion(t, propertyName, proxy, 0, null);
        }

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        protected CodeGeneratorBase(RootDefinitonsProvider roots)
        {
            Roots = roots;
        }

        public string TransformText()
        {
            _stringBuilder.Clear();
            Logs.Clear();
            Generate(_stringBuilder);
            return _stringBuilder.ToString();
        }

        protected abstract void Generate(StringBuilder sourceCode);

        public void GenericFieldDeclaration(string propertyName,
            string typeName,
            bool isPublic,
            bool isSerializable)
        {
            var scope = isPublic ? "public" : "private";
            if (isSerializable)
            {
                _stringBuilder.AppendLine("[SerializeField]");
            }

            _stringBuilder
                .AppendFormat("{0} {1} {2};", scope, typeName, propertyName)
                .AppendLine();
        }

        public void FieldDeclaration(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var propertyName = propertyInfo.Name;

            var typeName = GetTypeName(propertyType, true);
            GenericFieldDeclaration(propertyName, typeName, true, false);
        }

        private void FieldAssignment(string variableName, string propertyName, string typeConversion)
        {
            _stringBuilder
                .AppendFormat("{0}.{1} = {2};", variableName, propertyName, typeConversion)
                .AppendLine();
        }

        private void OriginValueFieldAssignment(string propertyName, string typeConversion)
        {
            const string origin = "origin";
            FieldAssignment(origin, propertyName, typeConversion);
        }

        public void CopyFromProxyToOrigin(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var propertyName = propertyInfo.Name;
            var typeConversion = GetTypeConversion(propertyType, propertyName, false, 0, "proxy.");
            OriginValueFieldAssignment(propertyName, typeConversion);
        }

        private void ProxyGenericFieldAssignment(string propertyName, string typeConversion)
        {
            const string proxy = "proxy";
            FieldAssignment(proxy, propertyName, typeConversion);
        }

        public void CopyFromOriginToProxy(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var propertyName = propertyInfo.Name;

            var typeConversion = GetTypeConversion(propertyType, propertyName, true, 0, "origin.");
            ProxyGenericFieldAssignment(propertyName, typeConversion);
        }

        public void NullableType(string valueTypeName)
        {
            var className = "NullableProxy" + GetGluedTypeName(valueTypeName);
            const string format = @"
	    [Serializable]
        public class <#=className#> 
		    : NullableProxy<<#=valueTypeName#>>
        {
            public <#=className#>()
			    : base()
            {

            }

            public <#=className#>(<#=valueTypeName#>? value)	
			    : base(value)
            {

            }

            public <#=className#>(<#=valueTypeName#> value)
			    : base(value)
            {
                HasValue = true;
                Value = value;
            }

            public static implicit operator <#=valueTypeName#>(<#=className#> proxy)
            {
                return proxy != null && proxy.HasValue
                    ? proxy.Value
				    : default(<#=valueTypeName#>);
            }

            public static implicit operator <#=valueTypeName#>?(<#=className#> proxy)
            {
                return proxy != null && proxy.HasValue
                    ? new <#=valueTypeName#>?(proxy.Value) 
				    : null;
            }

            public static implicit operator <#=className#>(<#=valueTypeName#> value)
            {
                return new <#=className#>(value);
            }

            public static implicit operator <#=className#>(<#=valueTypeName#>? value)
            {
                return new <#=className#>(value);
            }
        }
";
            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=className#>", className)
                    .Replace("<#=valueTypeName#>", valueTypeName))
                .AppendLine();
        }

        public void ListType(string elementTypeName)
        {
            const string format = @"
        [Serializable]
		public sealed class <#=proxyTypeName#>
			: ListProxy<<#=elementTypeName#>>
		{
			public <#=proxyTypeName#>()
				: base()
			{
			    
			}

			public <#=proxyTypeName#>(List<<#=elementTypeName#>> list)
				: base(list)
			{
			    
			}

			public <#=proxyTypeName#>(IEnumerable<<#=elementTypeName#>> array) 
			    : base(array)
			{
			    
			}

			public static implicit operator List<<#=elementTypeName#>>(<#=proxyTypeName#> proxy)
			{
				return proxy != null
					? proxy.List
					: null;
			}

			public static implicit operator <#=elementTypeName#>[](<#=proxyTypeName#> proxy)
			{
				return proxy != null
					? proxy.List.ToArray()
					: null;
			}

			public static implicit operator <#=proxyTypeName#>(List<<#=elementTypeName#>> list)
			{
			    return list != null
			        ? new <#=proxyTypeName#>(list)
			        : null;
			}

			public static implicit operator <#=proxyTypeName#>(<#=elementTypeName#>[] array)
			{
			    return array != null
			        ? new <#=proxyTypeName#>(array)
			        : null;
			}
		}
";
            var proxyTypeName = GetListTypeName(GetGluedTypeName(elementTypeName), true);
            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=proxyTypeName#>", proxyTypeName)
                    .Replace("<#=elementTypeName#>", elementTypeName))
                .AppendLine();
        }
        
        public void DictionaryType(string keyTypeName, string valueTypeName)
        {
            const string format = @"
    [Serializable]
    public sealed class <#=dicProxyType#>
	   : DictionaryProxy<<#=keyTypeName#>, <#=valueTypeName#>>
	{
	    public <#=dicProxyType#>()
		    : base() { }

		public <#=dicProxyType#>(<#=dicOriginType#> dictionary)
		    : base(dictionary) { }

		public static implicit operator <#=dicOriginType#>(<#=dicProxyType#> proxy)
		{
		    return proxy != null 
			    ? proxy.Dictionary
				: null;
		}

		public static implicit operator <#=dicProxyType#>(<#=dicOriginType#> origin)
		{
		    return origin != null
			    ? new <#=dicProxyType#>(origin)
			    : null;
		}
	}
";
            var dicProxyType = GetDictionaryTypeName(GetGluedTypeName(keyTypeName), GetGluedTypeName(valueTypeName), true);
            var dicOriginType = GetDictionaryTypeName(keyTypeName, valueTypeName, false);

            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=dicProxyType#>", dicProxyType)
                    .Replace("<#=dicOriginType#>", dicOriginType)
                    .Replace("<#=keyTypeName#>", keyTypeName)
                    .Replace("<#=valueTypeName#>", valueTypeName))
                .AppendLine();
        }

        public void ProxyType(Type type)
        {
            Log($"Generate proxy for {type.FullName}");

            const string format1 = @"
    [Serializable]
	[Proxy(typeof(<#=originTypeName#>))]
	public sealed class <#=proxyTypeName#>
	{
		public bool NotNull;

        public static bool IsNull(<#=proxyTypeName#> proxy)
		{
			if(proxy == null)
			{
				return true;
			}

			return !proxy.NotNull;
		}
";
            const string format2 = @"
		public static implicit operator <#=originTypeName#>(<#=proxyTypeName#> proxy)
		{
			if(IsNull(proxy))
			{
				return default(<#=originTypeName#>);
			}

			var origin = new <#=originTypeName#>();
";
            const string format3 = @"
			return origin;
		}

		public static implicit operator <#=proxyTypeName#>(<#=originTypeName#> origin)
		{
			if(Equals(origin, default(<#=originTypeName#>)))
			{
				return null;
			}

			var proxy = new <#=proxyTypeName#>();
			proxy.NotNull = true;
";
            const string format4 = @"
			return proxy;
		}
";
            const string format5 = @"
        public static implicit operator <#=originTypeName#>?(<#=proxyTypeName#> proxy)
		{
		    if(IsNull(proxy))
			{
			    return default(<#=originTypeName#>?);
			}

			return new <#=originTypeName#>?((<#=originTypeName#>)proxy);
		}

		public static implicit operator <#=proxyTypeName#>(<#=originTypeName#>? origin)
		{
		    if(origin == null)
			{
                return null;
			}

			return (<#=proxyTypeName#>)origin.Value;
		}
";
            var originTypeName = GetTypeName(type, false);
            var proxyTypeName = GetGluedTypeName(GetProxyTypeName(type));
            var properties = type.GetProperties()
                .Where(Roots.IsApplicable)
                .ToArray();

            Func<string, string> replacePlaceholders = f => new StringBuilder(f)
                .Replace("<#=proxyTypeName#>", proxyTypeName)
                .Replace("<#=originTypeName#>", originTypeName)
                .ToString();

            _stringBuilder
                .AppendLine()
                .Append(replacePlaceholders(format1))
                .AppendLine();

            foreach (var propertyInfo in properties)
            {
                var propertyType = propertyInfo.PropertyType;
                var propertyName = propertyInfo.Name;
                var propertyTypeName = GetTypeName(propertyType, true);
                if (!propertyType.IsNative())
                {
                    propertyTypeName = GetGluedTypeName(propertyTypeName);
                }

                GenericFieldDeclaration(propertyName, propertyTypeName, true, false);
            }

            _stringBuilder
                .Append(replacePlaceholders(format2))
                .AppendLine();

            foreach (var propertyInfo in properties)
            {
                CopyFromProxyToOrigin(propertyInfo);
            }

            _stringBuilder
                .Append(replacePlaceholders(format3))
                .AppendLine();

            foreach (var propertyInfo in properties)
            {
                CopyFromOriginToProxy(propertyInfo);
            }

            _stringBuilder
                .Append(format4)
                .AppendLine();

            if (type.IsValueType)
            {
                _stringBuilder
                    .AppendLine()
                    .Append(replacePlaceholders(format5))
                    .AppendLine();
            }

            _stringBuilder
                .Append("	}")
                .AppendLine();
        }

        public void Namespace(string ns, Action generateContent)
        {
            const string format = "namespace {0}";

            _stringBuilder.AppendLine()
                .AppendFormat(format, ns)
                .AppendLine()
                .Append('{').AppendLine();
            generateContent();
            _stringBuilder.Append('}').AppendLine();
        }

        const string proxy = "_proxy";
        const string cache = "_cache";

        public void Include(IEnumerable<string> namespaces)
        {
            foreach (var ns in namespaces)
            {
                _stringBuilder.AppendFormat("using {0};", ns).AppendLine();
            }
        }
        
        public void DictionaryStorageTypeContent(RootDefinition root, string storageTypeName, AssetDictionaryAttribute attribute)
        {
            var keyType = GetKeyType(root.Root, attribute);
            var valueType = root.Root;
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

            var proxyDictionaryTypeName = GetTypeName(dictionaryType, true);
            var originDictionaryTypeName = GetTypeName(dictionaryType, false);

            GenericFieldDeclaration(proxy, proxyDictionaryTypeName, false, true);
            GenericFieldDeclaration(cache, originDictionaryTypeName, false, false);

            var originToProxy = GetTypeConversion(dictionaryType, "value", true);
            var proxyToOrigin = GetTypeConversion(dictionaryType, proxy, false);

            const string format = @"
        public <#=originDictionaryTypeName#> Value
		{
		    get 
			{ 
			    <#=cache#> = <#=proxyToOrigin#>;		
			    return <#=cache#>; 
			}
			set 
			{ 
			    <#=proxy#> = <#=originToProxy#>;
				<#=cache#> = value;
			}
		}

		public static implicit operator <#=originDictionaryTypeName#>(<#=storageTypeName#> storage)
		{
		    return storage == null ? default(<#=originDictionaryTypeName#>) : storage.Value;
		}
";

            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=originDictionaryTypeName#>", originDictionaryTypeName)
                    .Replace("<#=storageTypeName#>", storageTypeName)
                    .Replace("<#=cache#>", cache)
                    .Replace("<#=proxy#>", proxy)
                    .Replace("<#=proxyToOrigin#>", proxyToOrigin)
                    .Replace("<#=originToProxy#>", originToProxy))
                .AppendLine();

            foreach (var pair in DictionaryProxyGenericArguments)
            {
                DictionaryType(pair.Key, pair.Value);
            }
        }

        private void SingleStorageTypeContent(RootDefinition root, string storageTypeName)
        {
            var originTypeName = GetTypeName(root.Root, false);
            var proxyTypeName = GetGluedTypeName(GetProxyTypeName(root.Root));
            GenericFieldDeclaration(proxy, proxyTypeName, false, true);
            GenericFieldDeclaration(cache, originTypeName, false, false);

            const string format = @"
        public <#=originTypeName#> Value
		{
		    get 
			{ 
			    <#=cache#> = <#=proxy#>;		
			    return <#=cache#>; 
			}
			set 
			{ 
			    <#=proxy#> = value;
				<#=cache#> = value;
			}
		}

		public static implicit operator <#=originTypeName#>(<#=storageTypeName#> storage)
		{
		    return storage == null ? default(<#=originTypeName#>) : storage.Value;
		}
";

            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=proxy#>", proxy)
                    .Replace("<#=cache#>", cache)
                    .Replace("<#=originTypeName#>", originTypeName)
                    .Replace("<#=storageTypeName#>", storageTypeName))
                .AppendLine();

        }

        private void ListStorageTypeContent(RootDefinition root, string storageTypeName)
        {
            var listType = typeof(List<>).MakeGenericType(root.Root);

            var listOriginTypeName = GetTypeName(listType, false);
            var listProxyTypeName = GetTypeName(listType, true);
            GenericFieldDeclaration(proxy, listProxyTypeName, false, true);
            GenericFieldDeclaration(cache, listOriginTypeName, false, false);

            var proxyToOrigin = GetTypeConversion(listType, proxy, false);
            var originToProxy = GetTypeConversion(listType, "value", true);

            const string format = @"
        public <#=listOriginTypeName#> Value
		{
		    get 
			{ 
				<#=cache#> = <#=proxyToOrigin#>;
			    return <#=cache#>; 
			}
			set
			{ 
			    <#=proxy#> = <#=originToProxy#>;
				<#=cache#> = value;
			}
		}

		public static implicit operator <#=listOriginTypeName#>(<#=storageTypeName#> storage)
		{
		    return storage == null ? default(<#=listOriginTypeName#>) : storage.Value;
		}
";

            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=proxy#>", proxy)
                    .Replace("<#=cache#>", cache)
                    .Replace("<#=proxyToOrigin#>", proxyToOrigin)
                    .Replace("<#=originToProxy#>", originToProxy)
                    .Replace("<#=listOriginTypeName#>", listOriginTypeName)
                    .Replace("<#=storageTypeName#>", storageTypeName))
                .AppendLine();

            foreach (var listTypeName in ListProxyGenericArguments)
            {
                ListType(listTypeName);
            }
        }

        public void StorageType(RootDefinition root, AssetDeclarationAttributeBase attribute)
        {
            var storageTypeName = GetStorageTypeName(root.Root, attribute);
            var rootTypeName = GetTypeName(root.Root, false);
            var typeGuid = Guid.NewGuid().ToString();
            var attributeGroup = attribute.Group;

            const string format = @"

        [Storage(""<#=attributeGroup#>"", typeof(<#=rootTypeName#>))]
        [GuidAttribute(""<#=typeGuid#>"")]
        public sealed class <#=storageTypeName#>
            : StorageBase
        {

        public <#=storageTypeName#>()
        {
            Group = ""<#=attributeGroup#>"";
        }

        protected override object GetStoredValue()
        {
            return Value;
        }
";
            _stringBuilder
                .AppendLine()
                .Append(new StringBuilder(format)
                    .Replace("<#=attributeGroup#>", attributeGroup)
                    .Replace("<#=rootTypeName#>", rootTypeName)
                    .Replace("<#=storageTypeName#>", storageTypeName)
                    .Replace("<#=typeGuid#>", typeGuid))
                .AppendLine();

            if (attribute is SingleAssetAttribute)
            {
                SingleStorageTypeContent(root, storageTypeName);
            }
            else if (attribute is AssetListAttribute)
            {
                ListStorageTypeContent(root, storageTypeName);
            }
            else if (attribute is AssetDictionaryAttribute)
            {
                DictionaryStorageTypeContent(root, storageTypeName, attribute as AssetDictionaryAttribute);
            }

            _stringBuilder.AppendLine("    }");
        }

        public void Log(string text)
        {
            Logs.Add(text);
        }

        public void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }
    }
}
