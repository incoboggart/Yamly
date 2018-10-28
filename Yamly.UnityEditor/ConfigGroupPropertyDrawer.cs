using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;

using UnityEngine;

using Yamly.Proxy;
using Yamly.UnityEngine;

namespace Yamly.UnityEditor
{
    [CustomPropertyDrawer(typeof(ConfigGroupAttribute), true)]
    public sealed class ConfigGroupPropertyDrawer
        : PropertyDrawer
    {
        private static string[] DisplayOptions;
        private static int[] OptionValues;

        private static Dictionary<string, int> Index;
        public ConfigGroupPropertyDrawer()
        {
            if (DisplayOptions == null)
            {
                DisplayOptions = new[] {"None"}.Concat(
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .Where(t => t.Have<ConfigDeclarationAttributeBase>(true))
                            .Select(t => t.GetSingle<ConfigDeclarationAttributeBase>(true))
                            .Select(a => a.GroupName))
                    .Distinct()
                    .ToArray();
                OptionValues = new int[DisplayOptions.Length];
                for (int i = 0; i < DisplayOptions.Length; i++)
                {
                    OptionValues[i] = i - 1;
                }

                Index = new Dictionary<string, int>();
                for (var i = 0; i < DisplayOptions.Length; i++)
                {
                    Index[DisplayOptions[i]] = i;
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Log("Draw");

            if (property.propertyType == SerializedPropertyType.String)
            {
                int index;
                if (!Index.TryGetValue(property.stringValue, out index))
                {
                    index = -1;
                }

                Debug.Log(index);

                EditorGUI.BeginChangeCheck();
                index = EditorGUI.IntPopup(position, property.displayName, index, DisplayOptions, OptionValues);
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log(index);

                    property.stringValue = index < 0 ? null : DisplayOptions[index];
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }

    }
}
