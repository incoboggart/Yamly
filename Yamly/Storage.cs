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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Yamly
{
    [CreateAssetMenu(fileName = "New Storage", menuName = "Yamly/Storage", order = 20)]
    [Guid("A38CEF4F-BC9E-4073-AAA7-6554F4896C7F")]
    public sealed class Storage
        : ScriptableObject
    {
        [SerializeField]
        [ConfigGroup]
        private List<string> _excludeGroups = new List<string>();

        [SerializeField]
        [HideInInspector]
        private List<StorageBase> _storages = new List<StorageBase>();

        public List<StorageBase> Storages => _storages;

        public List<string> ExcludedGroups => _excludeGroups;

        public bool Includes(string group)
        {
            return !_excludeGroups.Contains(group);
        }

        public bool Contains(string group)
        {
            return _storages?.Exists(s => s.Group == group) ?? false;
        }

        public bool Contains<T>(string group = null)
        {
            return GetStorage<T>(group) != null;
        }

        public T Get<T>(string group = null)
        {
            var storage = GetStorage<T>(group);
            return storage != null ? storage.Get<T>() : default(T);
        }

        public void Cleanup()
        {
            _storages = _storages.Where(s => s != null)
                .ToList();
        }

        public void SetStorage(string group, StorageBase storage)
        {
            if (_excludeGroups.Contains(group))
            {
                _excludeGroups.Remove(group);
            }

            if (!_storages.Contains(storage))
            {
                _storages.Add(storage);
            }
        }

        private StorageBase GetStorage<T>(string group)
        {
            return string.IsNullOrEmpty(group)
                ? _storages?.Find(s => s.Is<T>())
                : _storages?.Find(s => s.Group == group && s.Is<T>());
        }

        private StorageBase GetStorage<T>()
            where T : StorageBase
        {
            return _storages.Find(s => s is T) as T;
        }
    }
}