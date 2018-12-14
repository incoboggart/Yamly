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

namespace Yamly
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = true)]
    public abstract class AssetDeclarationAttributeBase 
        : Attribute
    {
        private NamingConvention _namingConvention = NamingConvention.Null;
        public string Group { get; }

        public abstract bool GetIsSingleFile();

        public abstract DeclarationType GetDeclarationType();

        public NamingConvention NamingConvention
        {
            get => _namingConvention;
            set
            {
                _namingConvention = value;
                ExplicitNamingConvention = value;
            }
        }

        public NamingConvention? ExplicitNamingConvention { get; private set; }

        protected AssetDeclarationAttributeBase(string group)
        {
            Group = group;
        }
    }

    public enum DeclarationType
    {
        Single, List, Dictionary
    }
}
