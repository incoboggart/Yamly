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

using UnityEngine;

using Yamly.Proxy;
using Yamly.UnityEditor;

namespace Yamly
{
    internal static class Utility
    {
        private static readonly Type ListType = typeof(List<>);
        private static readonly Type DictionaryType = typeof(Dictionary<,>);

        public static bool IsSingle(this DataRoute route)
        {
            return route.Attribute.IsSingle();
        }

        public static bool IsSingle(this AssetDeclarationAttributeBase attribute)
        {
            return attribute.GetIsSingleFile()
                   || attribute.GetDeclarationType() == DeclarationType.Single;
        }

        public static Type GetValueType(this DataRoute route)
        {
            switch (route.Attribute.GetDeclarationType())
            {
                case DeclarationType.Single:
                    return route.RootType;
                case DeclarationType.List:
                    return route.Attribute.GetIsSingleFile()
                        ? ListType.MakeGenericType(route.RootType)
                        : route.RootType;
                case DeclarationType.Dictionary:
                    if (!route.Attribute.GetIsSingleFile())
                    {
                        return route.RootType;
                    }
                    
                    var dictionaryAttribute = (AssetDictionaryAttribute) route.Attribute;                   
                    var keyType = dictionaryAttribute.KeyType;
                    if (keyType == null)
                    {
                        goto default;
                    }
                    
                    var valueType = route.RootType;
                    return DictionaryType.MakeGenericType(keyType, valueType);
                default:
                    return null;
            }
        }

        public static MethodInfo GetKeySourceMethodInfo(this Type rootType, AssetDictionaryAttribute attribute)
        {
            if (attribute.UseAssetFileNameAsKey)
            {
                return GetFileNameAsKey(rootType);
            }

            var methodInfo = GetPropertyKeySourceMethodInfo(rootType, attribute, allowGeneral: false);
            if (methodInfo != null)
            {
                return methodInfo;
            }

            methodInfo = GetCustomKeySourceMethodInfo(rootType, attribute, allowGeneral: false);
            if (methodInfo != null)
            {
                return methodInfo;
            }

            methodInfo = GetPropertyKeySourceMethodInfo(rootType, attribute, allowGeneral: true);
            if (methodInfo != null)
            {
                return methodInfo;
            }

            methodInfo = GetCustomKeySourceMethodInfo(rootType, attribute, allowGeneral: true);
            if (methodInfo != null)
            {
                return methodInfo;
            }

            return default(MethodInfo);
        }

        private static MethodInfo GetPropertyKeySourceMethodInfo(Type rootType, AssetDictionaryAttribute attribute, bool allowGeneral)
        {
            foreach (var propertyInfo in rootType.GetProperties())
            {
                var propertyAttributes = propertyInfo.Get<DictionaryKeyAttribute>().ToArray();
                if (propertyAttributes.Length == 0)
                {
                    continue;
                }

                foreach (var propertyAttribute in propertyAttributes)
                {
                    if (!propertyInfo.CanRead)
                    {
                        Debug.LogError($"Property {propertyInfo.Name} is not readable and have {nameof(DictionaryKeyAttribute)}. It will be ignored.");
                        continue;
                    }

                    if (allowGeneral && string.IsNullOrEmpty(propertyAttribute.GroupName))
                    {
                        return propertyInfo.GetGetMethod();
                    }

                    if (propertyAttribute.GroupName == attribute.Group)
                    {
                        return propertyInfo.GetGetMethod();
                    }
                }
            }

            return null;
        }

        private static MethodInfo GetCustomKeySourceMethodInfo(Type rootType, AssetDictionaryAttribute attribute, bool allowGeneral)
        {
            var keySourceType = attribute.KeySourceType ?? rootType;
            foreach (var methodInfo in keySourceType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                foreach (var methodAttribute in methodInfo.Get<DictionaryKeySourceAttribute>())
                {
                    if (methodAttribute == null)
                    {
                        continue;
                    }

                    if (!allowGeneral && methodAttribute.Group != attribute.Group)
                    {
                        continue;
                    }

                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length < 1 &&
                        parameters.Length > 2)
                    {
                        continue;
                    }

                    if (parameters.Length == 1
                        && parameters[0].ParameterType != rootType)
                    {
                        Debug.LogError($"Method {methodInfo.Name} is not valid for selecting keys! Type {rootType.Name} is not assignable from param type {parameters[0].ParameterType.Name}.");
                        continue;
                    }

                    var isValid = true;
                    foreach (var parameterInfo in parameters)
                    {
                        if (parameterInfo.ParameterType != rootType
                            && parameterInfo.ParameterType != typeof(TextAsset))
                        {
                            Debug.LogError($"Method {methodInfo.Name} is not valid for selecting keys! Method have invalid param type {parameterInfo.ParameterType}");
                            isValid = false;
                        }
                    }

                    if (!isValid)
                    {
                        continue;
                    }

                    return methodInfo;
                }
            }

            return null;
        }

        private static MethodInfo GetFileNameAsKey(Type type)
        {
            if (_getFileNameAsKeyGenericMethodInfo == null)
            {
                _getFileNameAsKeyGenericMethodInfo = typeof(Utility).GetMethod(nameof(GetFileNameAsKeyGeneric), BindingFlags.Static|BindingFlags.NonPublic);
            }

            return _getFileNameAsKeyGenericMethodInfo.MakeGenericMethod(type);
        }

        private static MethodInfo _getFileNameAsKeyGenericMethodInfo;
        
        private static string GetFileNameAsKeyGeneric<T>(TextAsset asset, T deserializedValue)
        {
            return asset.name;
        }
    }
}
