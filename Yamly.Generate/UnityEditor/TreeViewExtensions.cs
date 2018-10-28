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

using System.Linq;

using UnityEditor.IMGUI.Controls;

using UnityEngine;

namespace Yamly.UnityEditor
{
    public static class TreeViewExtensions
    {
        private static GUIStyle[] _rowBackgroundStyles;

        private static void InitializeStyles()
        {
            if (_rowBackgroundStyles == null ||
                _rowBackgroundStyles.Length == 0 ||
                _rowBackgroundStyles.Any(s => s == null))
            {
                _rowBackgroundStyles = new[]
                {
                    new GUIStyle("CN EntryBackEven"),
                    new GUIStyle("CN EntryBackOdd")
                };
            }
        }

        public static GUIStyle GetRowStyle(int rowIndex)
        {
            InitializeStyles();
            var style = GUIStyle.none;

            var styleIndex = rowIndex % _rowBackgroundStyles.Length;
            if (_rowBackgroundStyles != null &&
                styleIndex >= 0 &&
                styleIndex < _rowBackgroundStyles.Length)
            {
                style = _rowBackgroundStyles[styleIndex];
            }

            return style;
        }

        public static void DrawRowBackground(Rect rowRect, int row)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var style = GetRowStyle(row);
            style.Draw(rowRect, GUIContent.none, false, false, false, false);
        }

        public static void OnGUILayout(this TreeView treeView, params GUILayoutOption[] guiLayoutOptions)
        {
            var treeViewRect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, treeView.totalHeight, guiLayoutOptions);
            treeView.OnGUI(treeViewRect);
        }
    }
}
