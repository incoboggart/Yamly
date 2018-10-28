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

using System.IO;

using UnityEditor;

using UnityEngine;

using Yamly.CodeGeneration;

namespace Yamly.UnityEditor
{
    public sealed class YamlySettings
        : ScriptableObject
    {
        [Header("Syntax")]
        public bool IgnoreUnmatchedProperties = true;
        public NamingConvention NamingConvention = NamingConvention.Pascal;
        
        [Header("Build")]
        public int BuildPreprocessorCallbackOrder;
        
        [Header("Logs")]
        public bool VerboseLogs;
        public bool VerboseLogsOnBuild = true;
        
        private static YamlySettings _instance;
        public static YamlySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    var assetsPath = CodeGenerationUtility.GetSettingsPath();
                    _instance = AssetDatabase.LoadAssetAtPath<YamlySettings>(assetsPath);
                    if (_instance == null)
                    {
                        _instance = CreateInstance<YamlySettings>();

                        var directoryPath = Path.GetDirectoryName(assetsPath.ToSystemPath());
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);

                            AssetDatabase.Refresh();
                        }

                        AssetDatabase.CreateAsset(_instance, assetsPath);
                    }
                }

                return _instance;
            }
        }
    }
}
