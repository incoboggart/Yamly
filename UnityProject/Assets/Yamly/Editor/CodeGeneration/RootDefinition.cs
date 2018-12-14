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

namespace Yamly.CodeGeneration
{
    internal class RootDefinition
    {
        public Type Root;
        public List<AssetDeclarationAttributeBase> Attributes;
        public Type[] Types;
        public string[] Namespaces;

        public Assembly Assembly => Root.Assembly;

        public bool Contains(string group)
        {
            return Attributes.Exists(a => a.Group == group);
        }

        public bool Contains(Type type)
        {
            return Root == type || Types.Any(t => t == type);
        }

        public void Remove(string group)
        {
            Attributes.RemoveAll(a => a.Group == group);
        }

        public override string ToString()
        {
            return $"Types: {Types.Length}, Root: {Root.FullName}, Groups: {Attributes.Count}";
        }
    }
}