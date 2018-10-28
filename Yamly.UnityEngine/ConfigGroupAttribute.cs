using System;

using UnityEngine;

namespace Yamly.UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ConfigGroupAttribute
        : PropertyAttribute
    {
        public bool Editable { get; set; }
    }
}