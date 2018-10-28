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
using System.Runtime.InteropServices;

using UnityEngine;

namespace Yamly
{
    /// <summary>
    /// Stores single group definitions
    /// </summary>
    [CreateAssetMenu(fileName = "New Single Source", menuName = "Yamly/Single source", order = 0)]
    [Guid("55DB46E9-9A71-4D77-8DBD-DE60BCD9671B")]
    public sealed class SingleSource
        : SourceBase
    {
        [SerializeField]
        [ConfigGroup]
        private string[] _groups;
        
        [SerializeField]
        private TextAsset[] _assets;
        
        public override bool IsSingle => true;

        public string[] Groups
        {
            get { return _groups; }
            set { _groups = value; }
        }

        public TextAsset[] Assets
        {
            get { return _assets;}
            set { _assets = value; }
        }

        public TextAsset this[string group]
        {
            get
            {
                var asset = GetAsset(group);
                if (asset == null)
                {
                    throw new KeyNotFoundException(group);
                }

                return asset;
            }
            set
            {
                SetAsset(group, value);
            }
        }

        public override bool Contains(string group)
        {
            return _groups.Any(g => g == group);
        }

        public TextAsset GetAsset(string group)
        {
            if (string.IsNullOrEmpty(group))
            {
                return null;
            }

            if (_groups == null || _groups.Length == 0)
            {
                return null;
            }

            if (_assets == null || _assets.Length == 0)
            {
                return null;
            }
            
            var index = Array.FindIndex(_groups, g => g == group);
            if (index >= 0 && index < _assets.Length)
            {
                return _assets[index];
            }

            return null;
        }

        public void SetAsset(string group, TextAsset textAsset)
        {
            var groupIndex = -1;
            for (int i = 0; i < _groups.Length; i++)
            {
                if (_groups[i] == group)
                {
                    groupIndex = i;
                }
            }

            if (groupIndex < 0)
            {
                groupIndex = _groups.Length;
                Array.Resize(ref _groups, _groups.Length + 1);
            }

            if (_assets.Length < _groups.Length)
            {
                Array.Resize(ref _assets, _groups.Length);
            }

            _assets[groupIndex] = textAsset;
        }
    }
}