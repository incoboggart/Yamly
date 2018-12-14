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

using System.Reflection;

using UnityEditor;

using Yamly.UnityEditor;

namespace Yamly.CodeGeneration
{
    internal static class CodeGenerationUtility
    {
        public static string GetUnityEditorAssemblyPath()
        {
            var p = GetApplicationContentsPath();
            return p + "Managed/UnityEditor.dll";
        }

        public static string GetUnityEngineAssemblyPath()
        {
            var p = GetApplicationContentsPath();
            return p + "Managed/UnityEngine.dll";
        }

        public static string GetProxyAssemblyOutputPath(Assembly proxyAssembly = null)
        {
            var outputPath = proxyAssembly?.Location?.ToAssetsPath();
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = typeof(AssetDeclarationAttributeBase).Assembly.Location.ToAssetsPath().WithReplacedFilename("Yamly.Generated.dll");
            }

            return outputPath;
        }

        public static string GetUtilityAssemblySystemPath()
        {
            return typeof(AssetDeclarationAttributeBase).Assembly.Location.Replace("Yamly.Attributes.dll", "Yamly.Utility.dll");
        }

        public static string GetSettingsPath()
        {
            return typeof(AssetDeclarationAttributeBase).Assembly.Location.ToAssetsPath().WithReplacedFilename("YamlySettings.asset");
        }

        private static string GetApplicationContentsPath()
        {
            var p = EditorApplication.applicationContentsPath;
            p = p.Replace("\\", "/");
            if (!p.EndsWith("/"))
            {
                p += "/";
            }

            return p;
        }


        public static bool IsValidGroupName(string group)
        {
            if (string.IsNullOrEmpty(group))
            {
                return false;
            }

            if (char.IsDigit(group[0]))
            {
                return false;
            }

            foreach (var c in group)
            {
                if (char.IsLetterOrDigit(c)
                    || char.IsWhiteSpace(c)
                    || c == '_'
                    || c == '-')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static string GetGroupName(string group)
        {
            return group.Replace(" ", "_")
                .Replace("-", "_");
        }
    }
}
