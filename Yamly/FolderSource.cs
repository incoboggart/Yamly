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

using System.Runtime.InteropServices;

using UnityEngine;

namespace Yamly
{
    /// <summary>
    /// Stores collection group definitions
    /// </summary>
    [CreateAssetMenu(fileName = "New Folder Source", menuName = "Yamly/Folder source", order = 0)]
    [Guid("31944913-D16D-41FA-B130-3B4E80E25772")]
    public sealed class FolderSource
        : SourceBase
    {
        [SerializeField]
        [ConfigGroup]
        private string _group;

        [SerializeField]
        private bool _isRecursive;

        public override bool IsSingle => false;
        public override bool Contains(string group)
        {
            return _group == group;
        }

        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }

        public bool IsRecursive
        {
            get { return _isRecursive; }
            set { _isRecursive = value; }
        }
    }
}