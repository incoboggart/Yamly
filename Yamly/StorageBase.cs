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

using UnityEngine;

namespace Yamly
{
    public abstract class StorageBase
        : ScriptableObject
    {
        [SerializeField]
        [ConfigGroup(IsEditable = false)]
        private string _group;
        [NonSerialized]
        private bool _cached;
        [NonSerialized]
        private object _cache;

        public string Group
        {
            get { return _group; }
            protected set { _group = value; }
        }

        public bool Is<T>()
        {
            return Stored is T;
        }

        public T Get<T>()
        {
            if (Is<T>())
            {
                return (T) Stored;
            }
            return default(T);
        }

        protected abstract object GetStoredValue();

        protected object Stored
        {
            get
            {
                if (_cached)
                {
                    return _cache;
                }

                _cached = true;
                _cache = GetStoredValue();

                return _cache;
            }
        }
    }
}
