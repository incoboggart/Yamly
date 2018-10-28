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

namespace Yamly.Proxy
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public sealed class ProxyAttribute
        : Attribute
    {
        public Type OriginType { get; }
        
        public ProxyAttribute(Type originType)
        {
            OriginType = originType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StorageAttribute
        : Attribute
    {
        public StorageAttribute(string group, Type type)
        {
            Group = group;
            Type = type;
        }

        public string Group { get; }
        public Type Type { get; }
    }
}