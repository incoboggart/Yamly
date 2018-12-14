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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Yamly.Proxy
{
    [Serializable]
    public class DictionaryProxy<TKey, TValue>
        : ISerializationCallbackReceiver,
            IDictionary<TKey, TValue>
    {
        [SerializeField]
        private TKey[] _keys;
        [SerializeField]
        private TValue[] _values;

        private Dictionary<TKey, TValue> _dictionary;

        public Dictionary<TKey, TValue> Dictionary
        {
            get { return _dictionary; }
        }

        public DictionaryProxy()
            : this(new Dictionary<TKey, TValue>())
        {
            
        }

        public DictionaryProxy(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public static implicit operator Dictionary<TKey, TValue>(DictionaryProxy<TKey, TValue> proxy)
        {
            return proxy?._dictionary;
        }

        public static implicit operator DictionaryProxy<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            return new DictionaryProxy<TKey, TValue>
            {
                _dictionary = new Dictionary<TKey, TValue>(dictionary)
            };
        }
        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_keys == null ||
                _values == null ||
                _keys.Length != _values.Length)
            {
                return;
            }

            _dictionary = new Dictionary<TKey, TValue>();
            for (int i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];
                var value = _values[i];
                _dictionary[key] = value;
            }

            _keys = null;
            _values = null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _keys = _dictionary.Keys.ToArray();
            _values = _dictionary.Values.ToArray();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _dictionary).GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_dictionary).Add(item);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dictionary).Remove(item);
        }

        public int Count => _dictionary.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_dictionary).IsReadOnly;

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _dictionary[key]; }
            set { _dictionary[key] = value; }
        }

        public ICollection<TKey> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;
    }
}
