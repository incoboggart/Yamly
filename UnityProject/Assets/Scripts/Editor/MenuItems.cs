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

using UnityEditor;

using UnityEngine;

using Yamly.UnityEditor;

public static class DevelopmentPipeline
{
    [MenuItem("Yamly/Dev/Export Package")]
    public static void ExportPackage()
    {
        var exportPath = Environment.GetEnvironmentVariable("exportPath");
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = EditorUtility.SaveFilePanel("Export location", string.Empty, "yamly", "unitypackage");
        }

        Func<string, string> pluginPathTo = s => $"Assets/Plugins/Yamly/{s}";


        var files = new[]
        {
            pluginPathTo("Yamly.dll"),
            pluginPathTo("Yamly.dll.meta"),
            pluginPathTo("Yamly.Attributes.dll"),
            pluginPathTo("Yamly.Attributes.dll.meta"),
            pluginPathTo("Editor/Yamly.Generate.dll"),
            pluginPathTo("Editor/Yamly.Generate.dll.meta"),
            pluginPathTo("Editor/YamlDotNet.dll"),
            pluginPathTo("Editor/YamlDotNet.dll.meta"),
        };
        
        AssetDatabase.ExportPackage(files, exportPath, ExportPackageOptions.IncludeDependencies);
    }
    
    [MenuItem("Yamly/Dev/Clear editor prefs")]
    public static void ClearEditorPrefs()
    {
        YamlyEditorPrefs.Clear();
    }
}
