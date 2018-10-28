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

namespace Yamly.Proxy
{
    [Serializable]
    public class ListProxy<T> : IList<T>
    {
        public List<T> List;

        public ListProxy()
        {
            List = new List<T>();
        }

        public ListProxy(List<T> list)
        {
            List = list;
        }

        public ListProxy(IEnumerable<T> array)
        {
            List = array == null ? new List<T>() : new List<T>(array);
        }

        public static implicit operator List<T>(ListProxy<T> proxy)
        {
            return proxy != null
                ? proxy.List
                : null;
        }

        public static implicit operator T[] (ListProxy<T> proxy)
        {
            return proxy?.List.ToArray();
        }

        public static implicit operator ListProxy<T>(List<T> list)
        {
            return list != null
                ? new ListProxy<T>(list)
                : null;
        }

        public static implicit operator ListProxy<T>(T[] array)
        {
            return array != null
                ? new ListProxy<T>(array)
                : null;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)List).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            List.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            List.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return List.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            List.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            return List.Remove(item);
        }

        /// <inheritdoc />
        public int Count
        {
            get { return List.Count; }
        }

        /// <inheritdoc />
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return List.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            List.Insert(index, item);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get { return List[index]; }
            set { List[index] = value; }
        }
    }
}