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
    [CustomEditor(typeof(SingleSource))]
    public sealed class SingleSourceDefinitionEditor
        : Editor
    {
        private static string[] _groups;

        private TreeView _treeView;
        private SearchField _searchField;

        private string _searchText;
        private bool _reloadOnLayout;

        private void OnEnable()
        {
            if (_groups == null)
            {
                try
                {
                    Context.Init();
                    
                    _groups = Context.Attributes
                        .Where(r => r.IsSingle())
                        .Select(a => a.Group)
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

            _treeView = new TreeView(_groups, serializedObject);
            _treeView.Reload();

            _searchField = new SearchField();
        }

        public override void OnInspectorGUI()
        {
            if (_groups == null)
            {
                DrawEmpty();
                return;
            }
            
            var source = (SingleSource) target;
            if (source.Groups == null)
            {
                DrawEmpty();
                return;
            }
            
            if (source.Groups.Length != _groups.Length)
            {
                var assets = new TextAsset[_groups.Length];
                for (var i = 0; i < _groups.Length; i++)
                {
                    assets[i] = source.GetAsset(_groups[i]);
                }

                var groupsList = new SerializePropertyStringList(serializedObject, "_groups");
                groupsList.AddRange(_groups);
                groupsList.End();

                var assetsList = new SerializePropertyAssetList(serializedObject, "_assets");
                assetsList.AddRange(assets);
                assetsList.End();

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            if (Event.current.type == EventType.Layout)
            {
                if (_searchText != _treeView.state.searchString)
                {
                    _reloadOnLayout = false;
                    _treeView.state.searchString = _searchText;
                    _treeView.Reload();
                }

                if (_reloadOnLayout)
                {
                    _reloadOnLayout = false;
                    _treeView.Reload();
                }
            }

            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                _searchText = _searchField.OnGUI(_searchText);
                GUILayout.Space(8);

                EditorGUI.BeginChangeCheck();
                _treeView.OnGUILayout(GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    AssetDatabase.ImportAsset(target.GetAssetPath());

                    _reloadOnLayout = true;
                }
            }
        }

        private static void DrawEmpty()
        {
            EditorGUILayout.HelpBox("There is no groups defined in project", MessageType.Info);
        }

        private sealed class TreeView 
            : global::UnityEditor.IMGUI.Controls.TreeView
        {
            private const float Offset = 2;

            private readonly string[] _groups;

            private SerializePropertyAssetList _list;
            
            public TreeView(string[] groups, SerializedObject serializedObject) 
                : base(new TreeViewState())
            {
                _groups = groups;
                _list = new SerializePropertyAssetList(serializedObject, "_assets");

                rowHeight += Offset + 1;
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
                        root.AddChild(new TreeViewItem(group.GetHashCode()) { displayName = group });
                    }
                }

                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            protected override void BeforeRowsGUI()
            {
                base.BeforeRowsGUI();
                _list.Begin();
                while (_list.Count < _groups.Length)
                {
                    _list.Add(null);
                }
            }

            protected override void AfterRowsGUI()
            {
                base.AfterRowsGUI();
                _list.End();
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var rect = new Rect(args.rowRect);
                rect.yMin += Offset/2;
                rect.height -= Offset;
                TreeViewExtensions.DrawRowBackground(args.rowRect, args.row);
                _list[args.row] = EditorGUI.ObjectField(rect, new GUIContent(args.label), _list[args.row], typeof(TextAsset), false);
            }
        }
    }
}
