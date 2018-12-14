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

using UnityEditor;

using UnityEngine;

using Yamly.CodeGeneration;

namespace Yamly.UnityEditor
{
    [CustomPropertyDrawer(typeof(ConfigGroupAttribute), true)]
    public sealed class ConfigGroupPropertyDrawer
        : PropertyDrawer
    {
        private const string None = "None";

        private static readonly Type StorageBaseType = typeof(StorageBase);
        private static readonly Type SingleSourceDefinitionType = typeof(SingleSource);
        private static readonly Type FolderSourceDefinitionType = typeof(FolderSource);

        private static RootDefinition[] _roots;
        private static string[] _allGroups;
        private static string[] _singleGroups;
        private static string[] _multiGroups;

        private string[] _groups;
        private string[] _displayOptions;
        private int[] _optionValues;
        private int _index = int.MinValue;
        private int _indexOffset;

        private new ConfigGroupAttribute attribute => base.attribute as ConfigGroupAttribute;
        public ConfigGroupPropertyDrawer()
        {
            if (_roots != null)
            {
                return;
            }

            try
            {
                _roots = new ProxyCodeGenerator
                {
                    TargetAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                }.GetRootDefinitions().ToArray();
                
                var attributes = _roots.SelectMany(r => r.Attributes).ToArray();
                _allGroups = attributes
                    .Select(a => a.Group)
                    .Distinct()
                    .ToArray();
                _singleGroups = attributes
                    .Where(a => a.IsSingle())
                    .Select(a => a.Group)
                    .Distinct()
                    .ToArray();
                _multiGroups = attributes
                    .Where(a => !a.IsSingle())
                    .Select(a => a.Group)
                    .Distinct()
                    .ToArray();
            }
            catch (Exception e)
            {
                // We can crash here when user deletes one of root types. 
                if (YamlySettings.Instance.VerboseLogs)
                {
                    Debug.LogException(e);                    
                }
                
                _roots = null;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_roots == null)
            {
                return;
            }
            
            if (property.propertyType == SerializedPropertyType.String)
            {
                if (_index == int.MinValue && attribute.IsEditable)
                {
                    _indexOffset = attribute.IsEditable ? 100 : 0;
                    
                    if (SingleSourceDefinitionType.IsAssignableFrom(fieldInfo.DeclaringType))
                    {
                        _groups = _singleGroups;
                    }
                    else if (FolderSourceDefinitionType.IsAssignableFrom(fieldInfo.DeclaringType))
                    {
                        _groups = _multiGroups;
                    }
                    else
                    {
                        _groups = _allGroups;
                    }

                    var displayOptions = new List<string> { None };
                    var displayValues = new List<int>{-1};
                    for (int i = 0; i < _groups.Length; i++)
                    {
                        displayOptions.Add(_groups[i]);
                        displayValues.Add(_indexOffset + i);
                    }

                    _displayOptions = displayOptions.ToArray();
                    _optionValues = displayValues.ToArray();

                    _index = Array.IndexOf(_groups, property.stringValue);
                    if (_index >= 0)
                    {
                        _index += _indexOffset;
                    }
                }

                if (attribute.IsEditable)
                {
                    EditorGUI.BeginChangeCheck();
                    _index = EditorGUI.IntPopup(position, property.displayName, _index, _displayOptions, _optionValues);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.stringValue = _index < 0 ? null : _groups[_index - _indexOffset];
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    var displayValue = string.IsNullOrEmpty(property.stringValue) ? None : property.stringValue;
                    EditorGUI.LabelField(position, property.displayName, displayValue);
                }
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }

    }
}
