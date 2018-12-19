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

using UnityEditor;

using UnityEngine;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Yamly.Proxy;

namespace Yamly.UnityEditor
{
    public sealed class AssetsValidationResult
    {
        public readonly List<AssetValidationErrors> Errors = new List<AssetValidationErrors>();
        public object StoredValue;
    }
    
    public sealed class AssetValidationErrors
    {
        public string AssetPath;
        public TextAsset TextAsset;
        public string Error;
    }
    
    internal sealed class AssetProcessor
    {
        private readonly Dictionary<NamingConvention, Deserializer> _deserializer;
        private readonly List<Type> _storageTypes;
        private readonly Dictionary<Type, MethodInfo> _setStoragePropertyMethodInfo;
        private readonly MethodInfo _convertListMethodInfo;
        private readonly MethodInfo _convertDictionaryMethodInfo;
        private readonly MethodInfo _castDictionaryMethodInfo;
        private readonly MethodInfo _createStorageMethodInfo;
        private readonly MethodInfo _getStorageMethodInfo;

        public AssetProcessor(Assembly proxyAssembly)
        {
            _convertListMethodInfo = GetMethodInfo(nameof(ConvertList));
            _convertDictionaryMethodInfo = GetMethodInfo(nameof(ConvertDictionary));
            _castDictionaryMethodInfo = GetMethodInfo(nameof(CastDictionary));
            _createStorageMethodInfo = GetMethodInfo(nameof(CreateStorage));
            _getStorageMethodInfo = typeof(Storage).GetMethod("GetStorage", BindingFlags.Instance|BindingFlags.NonPublic, null, new Type[0], new ParameterModifier[0]);

            var types = proxyAssembly.GetTypes();
            _storageTypes = types.Where(t => t.Have<StorageAttribute>()).ToList();
            _setStoragePropertyMethodInfo = new Dictionary<Type, MethodInfo>();
            foreach (var storageType in _storageTypes)
            {
                _setStoragePropertyMethodInfo[storageType] = storageType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod();
            }

            Func<NamingConvention, Deserializer> build = namingConvention => new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .WithIgnoreUnmatchedProperties(YamlySettings.Instance.IgnoreUnmatchedProperties)
                .Build();
            _deserializer = new Dictionary<NamingConvention, Deserializer>
            {
                {NamingConvention.Camel, build(NamingConvention.Camel)},
                {NamingConvention.Hyphenated, build(NamingConvention.Hyphenated)},
                {NamingConvention.Pascal, build(NamingConvention.Pascal)},
                {NamingConvention.Underscored, build(NamingConvention.Underscored)},
                {NamingConvention.Null, build(NamingConvention.Null)}
            };
        }

        public AssetsValidationResult Rebuild(DataRoute route)
        {
            var result = Validate(route);
            if (route.Storages.Any()
                && GetStorageType(route) != null)
            {
                SetStoredValue(route, result.StoredValue);
            }
            return result;
        }
        
        public AssetsValidationResult Validate(DataRoute route)
        {
            var result = new AssetsValidationResult();
            var errors = result.Errors;
            var values = new Dictionary<TextAsset, object>();
            var type = route.GetValueType();
            
            foreach (var assetPath in route.GetAssetPaths().Distinct())
            {
                var textAsset = Context.GetAsset<TextAsset>(assetPath);
                if (textAsset != null)
                {
                    var namingConvention = route.Attribute.GetNamingConvention();
                    object value;
                    
                    try
                    {
                        value = Deserialize(textAsset.text, type, namingConvention);
                    }
                    catch (YamlException e)
                    {
                        var error = new AssetValidationErrors
                        {
                            AssetPath = assetPath,
                            TextAsset = textAsset,
                            Error = $"[{route.Group}]: {System.IO.Path.GetFileName(assetPath)} has syntax errors from {e.Start} to {e.End}"
                        };
                        if (e.InnerException != null)
                        {
                            error.Error += $"\n\r{e.InnerException.Message}";
                        }
                        errors.Add(error);
                        continue;
                    }

                    values[textAsset] = value;
                }
                else
                {
                    errors.Add(new AssetValidationErrors
                    {
                        AssetPath = assetPath,
                        Error = $"[{route.Group}]: Failed to load TextAsset on path {assetPath}"
                    });
                }
            }

            switch (route.Attribute.GetDeclarationType())
            {
                case DeclarationType.Single:
                    if (values.Count == 1)
                    {
                        result.StoredValue = values.SingleOrDefault().Value;
                    }
                    break;
                case DeclarationType.List:
                    if (route.IsSingleFile)
                    {
                        var list = values.Values.SingleOrDefault() as IEnumerable<object>;
                        if (list != null)
                        {
                            result.StoredValue = ConvertList(list, route.RootType);
                        }
                    }
                    else
                    {
                        result.StoredValue = ConvertList(values.Values, route.RootType);
                    }
                    
                    break;
                case DeclarationType.Dictionary:
                    var methodInfo = route.KeySourceMethodInfo;
                    var assetsByKey = new Dictionary<object, List<TextAsset>>();
                    var pairs = new List<KeyValuePair<object, object>>();
                    var getKeyFunc = GetGetKeyFunc(route, methodInfo);
                    
                    if (route.IsSingleFile)
                    {
                        var dictionaryAttribute = (AssetDictionaryAttribute)route.Attribute;
                        var dictionary = values.SingleOrDefault().Value;
                        if (dictionary != null)
                        {
                            result.StoredValue = ConvertDictionary(dictionary, dictionaryAttribute.KeyType, route.RootType);
                        }
                    }
                    else
                    {
                        foreach (var p in values)
                        {
                            var v = p.Value;
                            var k = getKeyFunc(methodInfo, v, p.Key);
                            if (k == null)
                            {
                                errors.Add(new AssetValidationErrors
                                {
                                    TextAsset = p.Key,
                                    AssetPath = p.Key.GetAssetPath(),
                                    Error = $"[{route.Group}]: Asset {p.Key.name} dictionary key is invalid!"
                                });
                                continue;
                            }

                            Add(assetsByKey, k, p.Key);
                            pairs.Add(new KeyValuePair<object, object>(k, v));
                        }
                    
                        result.StoredValue = ConvertDictionary(pairs, methodInfo.ReturnType, route.RootType);
                    }

                    foreach (var pair in assetsByKey)
                    {
                        if (pair.Value == null
                            || pair.Value.Count <= 1)
                        {
                            continue;
                        }
                        
                        foreach (var asset in pair.Value)
                        {
                            errors.Add(new AssetValidationErrors
                            {
                                TextAsset = asset,
                                AssetPath = asset.GetAssetPath(),
                                Error = $"[{route.Group}]: {asset.name} contains duplicate dictionary key {pair.Key}! This will result in keys override and errors!"
                            });
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private static Func<MethodInfo, object, TextAsset, object> GetGetKeyFunc(DataRoute route, 
            MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return null;
            }
            
            var parameters = methodInfo.GetParameters();
            
            Func<MethodInfo, object, TextAsset, object> getKeyFunc = null;
            if (parameters.Length == 0 &&
                !methodInfo.IsStatic)
            {
                getKeyFunc = (m, v, a) => m.Invoke(v, null);
            }

            if (!methodInfo.IsStatic)
            {
                return getKeyFunc;
            }

            if (parameters.Length == 1)
            {
                getKeyFunc = (m, v, a) => m.Invoke(null, new[] {v});
            }
            else if (parameters.Length == 2)
            {
                if (parameters[0].ParameterType == typeof(TextAsset))
                {
                    getKeyFunc = (m, v, a) => m.Invoke(null, new[] {a, v});
                }

                if (parameters[0].ParameterType == route.RootType)
                {
                    getKeyFunc = (m, v, a) => m.Invoke(null, new[] {v, a});
                }
            }

            return getKeyFunc;
        }

        public void SetStoredValue(DataRoute route, object storedValue)
        {
            var storageType = GetStorageType(route);
            
            foreach (var storageDefinition in route.Storages)
            {
                var getStorage = Create<Func<StorageBase>>(_getStorageMethodInfo.MakeGenericMethod(storageType), storageDefinition);

                var storage = getStorage?.Invoke();
                if (storage == null)
                {
                    storage = CreateStorage(storageType);
                    storage.name = storage.Group;
                    AssetDatabase.AddObjectToAsset(storage, storageDefinition);

                    storageDefinition.SetStorage(route.Group, storage);
                }

                SetValue(storedValue, storage);
                
                EditorUtility.SetDirty(storageDefinition);
            }
        }
        
        private static void Add<TKey, TValue>(IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            List<TValue> list;
            if (!dictionary.TryGetValue(key, out list))
            {
                list = new List<TValue> {value};
                dictionary[key] = list;
            }
            else
            {
                list.Add(value);
            }
        }

        private Type GetStorageType(DataRoute dataRoute)
        {
            return GetStorageType(dataRoute.RootType, dataRoute.Group);
        }

        private Type GetStorageType(Type type, string group)
        {
            return _storageTypes.Find(t =>
            {
                var a = t.GetSingle<StorageAttribute>();
                return a != null && a.Group == group && a.Type == type;
            });
        }

        private void SetValue(object value, StorageBase storage)
        {
            var storageType = storage.GetType();
        
            MethodInfo setMethodInfo;
            if (_setStoragePropertyMethodInfo.TryGetValue(storageType, out setMethodInfo))
            {
                setMethodInfo.Invoke(storage, new[] {value});
            }
        }

        private object Deserialize(string input, Type type, NamingConvention namingConvention)
        {
            Deserializer deserializer;
            if (!_deserializer.TryGetValue(namingConvention, out deserializer))
            {
                deserializer = _deserializer[NamingConvention.Null];
            }

            return deserializer.Deserialize(input, type);
        }

        private StorageBase CreateStorage(Type type)
        {
            var methodInfo = _createStorageMethodInfo.MakeGenericMethod(type);
            var createFunc = Create<Func<StorageBase>>(methodInfo);
            return createFunc?.Invoke();
        }

        private object ConvertList(IEnumerable<object> objects, Type type)
        {
            var methodInfo = _convertListMethodInfo.MakeGenericMethod(type);
            var convertFunc = Create<Func<IEnumerable<object>, object>>(methodInfo);
            return convertFunc?.Invoke(objects);
        }

        private object ConvertDictionary(IEnumerable<KeyValuePair<object, object>> objects, Type keyType, Type valueType)
        {
            var methodInfo = _convertDictionaryMethodInfo.MakeGenericMethod(keyType, valueType);
            var convertFunc = Create<Func<IEnumerable<KeyValuePair<object, object>>, object>>(methodInfo);
            return convertFunc?.Invoke(objects);
        }

        private object ConvertDictionary(object dictionary, Type keyType, Type valueType)
        {
            var methodInfo = _convertDictionaryMethodInfo.MakeGenericMethod(keyType, valueType);
            var convertFunc = Create<Func<IEnumerable<KeyValuePair<object, object>>, object>>(methodInfo);
            return convertFunc?.Invoke(CastDictionary(dictionary, keyType, valueType));
        }
        
        private IEnumerable<KeyValuePair<object, object>> CastDictionary(object dictionary, Type keyType, Type valueType)
        {
            var methodInfo = _castDictionaryMethodInfo.MakeGenericMethod(keyType, valueType);
            var convertFunc = Create<Func<object, IEnumerable<KeyValuePair<object, object>>>>(methodInfo);
            return convertFunc?.Invoke(dictionary);
        }

        private static StorageBase CreateStorage<T>()
            where T : StorageBase
        {
            return ScriptableObject.CreateInstance<T>();
        }

        private static object ConvertList<T>(IEnumerable<object> objects)
        {
            return objects.Cast<T>()
                .ToList();
        }

        private static object ConvertDictionary<TKey, TValue>(IEnumerable<KeyValuePair<object, object>> pairs)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var pair in pairs)
            {
                result[(TKey) pair.Key] = (TValue) pair.Value;
            } 

            return result;
        }

        private static IEnumerable<KeyValuePair<object, object>> CastDictionary<TKey, TValue>(object obj)
        {
            var dictionary = (IDictionary<TKey, TValue>)obj;
            foreach (var pair in dictionary)
            {
                yield return new KeyValuePair<object, object>(pair.Key, pair.Value);
            }
        }

        private static MethodInfo GetMethodInfo(string name, BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic)
        {
            return typeof(AssetProcessor)
                .GetMethods(bindingFlags)
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == name);
        }

        private static T Create<T>(MethodInfo methodInfo, object target = null)
            where T : class
        {
            if (target != null)
            {
                return Delegate.CreateDelegate(typeof(T), target, methodInfo) as T;
            }

            return Delegate.CreateDelegate(typeof(T), methodInfo) as T;
        }
    }

    internal static class NamingExtensions
    {
        public static NamingConvention GetNamingConvention(this AssetDeclarationAttributeBase attribute)
        {
            return attribute.ExplicitNamingConvention ?? YamlySettings.Instance.NamingConvention;
        }

        public static SerializerBuilder WithNamingConvention(this SerializerBuilder serializerBuilder,
            NamingConvention namingConvention)
        {
            var target = GetNamingConvention(namingConvention);
            return serializerBuilder.WithNamingConvention(target);
        }

        public static SerializerBuilder WithJsonCompatible(this SerializerBuilder serializerBuilder, bool isJsonCompatible)
        {
            return isJsonCompatible 
                ? serializerBuilder.JsonCompatible() 
                : serializerBuilder;
        }

        public static DeserializerBuilder WithNamingConvention(this DeserializerBuilder deserializerBuilder, NamingConvention namingConvention)
        {
            var target = GetNamingConvention(namingConvention);
            return deserializerBuilder.WithNamingConvention(target);
        }

        private static INamingConvention GetNamingConvention(NamingConvention namingConvention)
        {
            INamingConvention target;
            switch (namingConvention)
            {
                case NamingConvention.Camel:
                    target = new CamelCaseNamingConvention();
                    break;
                case NamingConvention.Hyphenated:
                    target = new HyphenatedNamingConvention();
                    break;
                case NamingConvention.Pascal:
                    target = new PascalCaseNamingConvention();
                    break;
                case NamingConvention.Underscored:
                    target = new UnderscoredNamingConvention();
                    break;
                case NamingConvention.Null:
                    target = new NullNamingConvention();
                    break;
                default:
                    throw new NotImplementedException(namingConvention.ToString());
            }

            return target;
        }

        public static DeserializerBuilder WithIgnoreUnmatchedProperties(this DeserializerBuilder deserializerBuilder, bool ignoreUnmatchedProperties)
        {
            return ignoreUnmatchedProperties 
                ? deserializerBuilder.IgnoreUnmatchedProperties() 
                : deserializerBuilder;
        }
    }
}