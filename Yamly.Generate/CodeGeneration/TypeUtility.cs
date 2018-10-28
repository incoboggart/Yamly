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
    }
}
