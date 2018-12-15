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

using Yamly.CodeGeneration;
using Yamly.Proxy;
using Yamly.UnityEditor;

namespace Yamly
{
    internal static class DataRouteUtility
    {
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
                        ? TypeUtility.ListType.MakeGenericType(route.RootType)
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
                    return TypeUtility.DictionaryType.MakeGenericType(keyType, valueType);
                default:
                    return null;
            }
        }
    }
}
