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

using UnityEditor;

using UnityEngine;

namespace Yamly.UnityEditor
{
    public static class YamlyEditorPrefs
    {
        private const string IsAssemblyBuildPendingKey = "{FBC14339-7B8D-4D00-9CAF-3EAAD8D2CD75}";
        private const string AssetsImportContextKey = "{912CECC2-21F9-4A5C-8C39-4F72867076C8}";
        
        public static bool IsAssemblyBuildPending
        {
            get { return EditorPrefs.GetBool(IsAssemblyBuildPendingKey, false); }
            internal set { EditorPrefs.SetBool(IsAssemblyBuildPendingKey, value); }
        }

        public static bool IsAssetsImportPending => !string.IsNullOrEmpty(EditorPrefs.GetString(AssetsImportContextKey, null));

        internal static YamlyPostprocessAssetsContext AssetImportContext
        {
            get
            {
                var json = EditorPrefs.GetString(AssetsImportContextKey, null);
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<YamlyPostprocessAssetsContext>(json);
            }
            set
            {
                var json = value == null ? null : JsonUtility.ToJson(value);
                EditorPrefs.SetString(AssetsImportContextKey, json);
            }
        }

        public static void Clear()
        {
            EditorPrefs.DeleteKey(IsAssemblyBuildPendingKey);
            EditorPrefs.DeleteKey(AssetsImportContextKey);
        }
    }
}
