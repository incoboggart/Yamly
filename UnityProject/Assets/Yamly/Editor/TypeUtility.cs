﻿// Copyright (c) 2018 Alexander Bogomoletz
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

namespace Yamly.CodeGeneration
{
    public static class TypeUtility
    {
        public static readonly Type DictionaryType = typeof(Dictionary<,>);
        public static readonly Type ListType = typeof(List<>);
        public static readonly Type NullableType = typeof(Nullable<>);

        public static bool IsDictionaryType(this Type t)
        {
            return t.IsGenericType &&
                   t.GetGenericTypeDefinition() == DictionaryType;
        }

        public static bool IsListType(this Type t)
        {
            return t.IsGenericType &&
                   t.GetGenericTypeDefinition() == ListType;
        }

        public static bool IsNullableType(this Type t)
        {
            return t.IsGenericType
                   && t.GetGenericTypeDefinition() == NullableType;
        }

        public static readonly ICollection<Type> NativeTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(short),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(string)
        };

        public static bool IsNative(this Type t)
        {
            return t.IsEnum
                   || NativeTypes.Contains(t);
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

        public static Type GetDictionaryKeyType(this Type rootType, AssetDictionaryAttribute a)
        {
            if (a == null)
            {
                return null;
            }

            return a.IsSingleFile 
                ? a.KeyType 
                : rootType.GetKeySourceMethodInfo(a)?.ReturnType;
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
                        LogUtils.Error($"Property {propertyInfo.Name} is not readable and have {nameof(DictionaryKeyAttribute)}. It will be ignored.");
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
                        LogUtils.Error($"Method {methodInfo.Name} is not valid for selecting keys! Type {rootType.Name} is not assignable from param type {parameters[0].ParameterType.Name}.");
                        continue;
                    }

                    var isValid = true;
                    foreach (var parameterInfo in parameters)
                    {
                        if (parameterInfo.ParameterType != rootType
                            && parameterInfo.ParameterType != typeof(TextAsset))
                        {
                            LogUtils.Error($"Method {methodInfo.Name} is not valid for selecting keys! Method have invalid param type {parameterInfo.ParameterType}");
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
        
        private static MethodInfo _getFileNameAsKeyGenericMethodInfo;
        
        private static MethodInfo GetFileNameAsKey(Type type)
        {
            if (_getFileNameAsKeyGenericMethodInfo == null)
            {
                _getFileNameAsKeyGenericMethodInfo = typeof(TypeUtility).GetMethod(nameof(GetFileNameAsKeyGeneric), BindingFlags.Static|BindingFlags.NonPublic);
            }

            return _getFileNameAsKeyGenericMethodInfo.MakeGenericMethod(type);
        }
        
        private static string GetFileNameAsKeyGeneric<T>(TextAsset asset, T deserializedValue)
        {
            return asset.name;
        }
    }
}
