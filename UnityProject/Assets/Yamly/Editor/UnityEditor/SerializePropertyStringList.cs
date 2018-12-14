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

using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace Yamly.UnityEditor
{
    public abstract class SerializePropertyListBase<T> : IList<T>
    {
        private readonly SerializedObject _serializedObject;
        private readonly string _propertyPath;
        private readonly List<T> _list = new List<T>();

        private SerializedProperty _serializedProperty;

        public SerializePropertyListBase(SerializedObject serializedObject, string propertyPath)
        {
            _serializedObject = serializedObject;
            _propertyPath = propertyPath;
        }

        protected abstract void SetValue(SerializedProperty serializedProperty, T value);
        protected abstract T GetValue(SerializedProperty serializedProperty);

        private int _startCount;

        public void Begin()
        {
            _serializedProperty = _serializedObject.FindProperty(_propertyPath);

            _list.Clear();
            while (_serializedProperty.propertyType != SerializedPropertyType.ArraySize)
            {
                _serializedProperty.Next(true);
            }

            var count = _serializedProperty.intValue;
            for (int i = 0; i < count; i++)
            {
                _serializedProperty.Next(false);
                _list.Add(GetValue(_serializedProperty));
            }

            _startCount = count;
        }

        public void End()
        {
            _serializedProperty = _serializedObject.FindProperty(_propertyPath);
            var deltaCount = _startCount - Count;
            if (deltaCount != 0)
            {
                var dest = deltaCount > 0 ? 1 : -1;
                for (int i = deltaCount; i > 0; i -= dest)
                {
                    if (deltaCount > 0)
                    {
                        _serializedProperty.InsertArrayElementAtIndex(0);
                    }
                    else
                    {
                        _serializedProperty.DeleteArrayElementAtIndex(0);
                    }
                }
            }

            while (_serializedProperty.propertyType != SerializedPropertyType.ArraySize)
            {
                _serializedProperty.Next(true);
            }

            _serializedProperty.intValue = Count;
            for (int i = 0; i < Count; i++)
            {
                _serializedProperty.Next(false);
                SetValue(_serializedProperty, this[i]);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _list).GetEnumerator();
        }

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public int Count => _list.Count;

        bool ICollection<T>.IsReadOnly => false;

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return _list[index]; }
            set { _list[index] = value; }
        }
    }

    public sealed class SerializePropertyAssetList 
        : SerializePropertyListBase<Object>
    {
        public SerializePropertyAssetList(SerializedObject serializedObject, string propertyPath) : base(serializedObject, propertyPath)
        {
        }

        protected override void SetValue(SerializedProperty serializedProperty, Object value)
        {
            serializedProperty.objectReferenceValue = value;
        }

        protected override Object GetValue(SerializedProperty serializedProperty)
        {
            return serializedProperty.objectReferenceValue;
        }
    }

    public sealed class SerializePropertyStringList : SerializePropertyListBase<string>
    {
        public SerializePropertyStringList(SerializedObject serializedObject, string propertyPath) : base(serializedObject, propertyPath)
        {
        }

        protected override void SetValue(SerializedProperty serializedProperty, string value)
        {
            serializedProperty.stringValue = value;
        }

        protected override string GetValue(SerializedProperty serializedProperty)
        {
            return serializedProperty.stringValue;
        }
    }
}
