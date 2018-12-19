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
using UnityEditor.IMGUI.Controls;

using UnityEngine;

using Yamly.CodeGeneration;

namespace Yamly.UnityEditor
{
    [CustomEditor(typeof(Storage))]
    public sealed class StorageDefinitionEditor
        : Editor
    {
        private static string[] _groups;

        private GroupsTreeView _treeView;
        private TreeViewState _treeViewState;
        private SearchField _searchField;

        private string _searchText;
        private bool _reloadOnLayout;

        private Storage Target => (Storage) target;

        private void OnEnable()
        {
            if (_groups == null)
            {
                try
                {
                    Context.Init();
                    
                    _groups = Context.Groups
                        .Distinct()
                        .ToArray();
                }
                catch (Exception e)
                {
                    LogUtils.Verbose(e);
                    
                    _groups = null;
                }
            }

            if (_groups == null)
            {
                return;
            }
            
            _treeViewState = new TreeViewState();
            _treeView = new GroupsTreeView(_groups, serializedObject);
            _treeView.Reload();

            _searchField = new SearchField();
        }

        private static void DrawEmpty()
        {
            EditorGUILayout.HelpBox("There is no groups defined in project", MessageType.Info);
        }
        
        public override void OnInspectorGUI()
        {
            if (_groups == null
                || _groups.Length == 0)
            {
                DrawEmpty();
                return;
            }
            
            if (Event.current.type == EventType.Layout)
            {
                if (_searchText != _treeViewState.searchString)
                {
                    _reloadOnLayout = false;
                    _treeViewState.searchString = _searchText;
                    _treeView.Reload();
                }

                if (_reloadOnLayout)
                {
                    _reloadOnLayout = false;
                    _treeView.Reload();
                }
            }
            
            using(new GUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                _searchText = _searchField.OnGUI(_searchText);
                GUILayout.Space(8);

                EditorGUI.BeginChangeCheck();
                _treeView.OnGUILayout(GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(Target.GetAssetPath());

                    _reloadOnLayout = true;
                }
            }
        }
    }

    internal sealed class GroupsTreeView : TreeView
    {
        private readonly string[] _groups;
        private readonly bool[] _values;
        private readonly SerializePropertyStringList _list;

        public GroupsTreeView(string[] groups, SerializedObject serializedObject) : base(new TreeViewState())
        {
            _groups = groups;
            _values = new bool[groups.Length];
            rowHeight += 2;

            var storage = (Storage) serializedObject.targetObject;
            for (int i = 0; i < _groups.Length; i++)
            {
                _values[i] = storage.Includes(_groups[i]);
            }

            _list = new SerializePropertyStringList(serializedObject, "_excludeGroups");
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                depth = -1,
                id = 0,
                displayName = "Root",
                children = new List<TreeViewItem>()
            };
            foreach (var group in _groups)
            {
                if (string.IsNullOrEmpty(searchString) ||
                    group.Contains(searchString))
                {
                    root.AddChild(new TreeViewItem(group.GetHashCode()){displayName = group});
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            TreeViewExtensions.DrawRowBackground(args.rowRect, args.row);

            var value = _values[args.row];
            EditorGUI.BeginChangeCheck();
            GUI.Label(GetLabelRect(args.rowRect, 18), args.label);
            value = GUI.Toggle(args.rowRect, value, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                _values[args.row] = value;
                if (value)
                {
                    _list.Remove(_groups[args.row]);
                }
                else
                {
                    _list.Add(_groups[args.row]);
                }
            }
        }

        private Rect GetLabelRect(Rect rowRect, float offset)
        {
            var rect = new Rect(rowRect);
            rect.xMin += offset;
            return rect;
        }

        protected override void BeforeRowsGUI()
        {
            base.BeforeRowsGUI();
            _list.Begin();
        }

        protected override void AfterRowsGUI()
        {
            base.AfterRowsGUI();
            _list.End();
        }
    }
}
