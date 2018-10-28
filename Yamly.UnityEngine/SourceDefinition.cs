using UnityEngine;

namespace Yamly.UnityEngine
{
    public sealed class SourceDefinition
        : ScriptableObject
    {
        [SerializeField]
        [ConfigGroup]
        private string _group;

        [SerializeField]
        private bool _isRecursive;

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

        private void OnValidate()
        {
            
        }
    }
}