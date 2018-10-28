using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Yamly.UnityEngine
{
    public sealed class StorageDefinition
        : ScriptableObject,
            IEnumerable<string>
    {
        [SerializeField]
        [ConfigGroup]
        private List<string> _groups;

        public bool Contains(string group)
        {
            return _groups.Contains(group);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return _groups.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _groups.GetEnumerator();
        }
    }

    public abstract class StorageBase
        : ScriptableObject
    {
        [SerializeField]
        [ConfigGroup(Editable = false)]
        private string _group;

        public string Group
        {
            get { return _group; }
            protected set { _group = value; }
        }
    }
}
