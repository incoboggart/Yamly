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

using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace Yamly.UnityEditor
{
    public static class AssetUtility
    {
        private static string _dataPath;
        
        public static string WithReplacedFilename(this string path, string filename)
        {
            var fileName = Path.GetFileName(path);
            return path.Replace(fileName, filename);
        }
        
        public static bool IsProjectPath(this string path)
        {
            return path.StartsWith(Application.dataPath);
        }

        public static string ToSystemPath(this string assetsPath)
        {
            if (string.IsNullOrEmpty(_dataPath))
            {
                _dataPath = Application.dataPath.ToUnityPath();
                if (!_dataPath.EndsWith("/"))
                {
                    _dataPath += "/";
                }
            }
            
            if (assetsPath.StartsWith("Assets/"))
            {
                return assetsPath.Replace("Assets/", _dataPath);
            }

            return assetsPath;
        }

        public static string ToUnityPath(this string path)
        {
            return path.Replace("\\", "/");
        }

        public static string ToAssetsPath(this string systemPath)
        {
            systemPath = systemPath.Replace("\\", "/");
            return systemPath.Replace(Application.dataPath, "Assets");
        }

        public static string GetAssetPath<T>(this T asset)
            where T : Object
        {
            return AssetDatabase.GetAssetPath(asset);
        }

        public static string GetAssetPathFolder<T>(this T asset)
            where T : Object
        {
            return GetAssetPathFolder(GetAssetPath(asset));
        }

        public static IEnumerable<string> GetAssetPaths<T>(string search = null)
        {
            var t = typeof(T).Name;
            var filter = string.IsNullOrEmpty(search)
                ? $"t:{t}"
                : $"t:{t} {search}";

            return AssetDatabase.FindAssets(filter)
                .Select(AssetDatabase.GUIDToAssetPath);
        }

        public static IEnumerable<T> LoadAll<T>(string search = null)
            where T : Object
        {
            return GetAssetPaths<T>(search)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(s => s != null);
        }

        internal static TextAsset GetSingleAsset(this SourceBase source, string group)
        {
            if (source == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(group))
            {
                return null;
            }

            var s = source as SingleSource;
            return s == null ? null : s.GetAsset(group);
        }

        public const string JsonFileExtension = "json";
        public const string YamlFileExtension = "yaml";
        
        internal static bool IsSupportedTextAsset(Object asset)
        {
            return asset is TextAsset
                   && IsSupportedTextAssetPath(asset.GetAssetPath());
        }

        internal static bool IsSupportedTextAssetPath(string assetPath)
        {
            return assetPath.EndsWith(JsonFileExtension)
                   || assetPath.EndsWith(YamlFileExtension);
        }

        internal static IEnumerable<TextAsset> GetAssets(this SourceBase source, AssetDeclarationAttributeBase attribute)
        {
            if (attribute.GetDeclarationType() == DeclarationType.Single
                || attribute.GetIsSingleFile())
            {
                if (!source.IsSingle)
                {
                    yield break;
                }

                var singleSource = source as SingleSource;
                if (singleSource == null)
                {
                    yield break;
                }

                var asset = singleSource.GetAsset(attribute.Group);
                if (asset != null)
                {
                    yield return asset;
                }
            }
            else
            {
                var singleSource = source as SingleSource;
                if (singleSource != null)
                {
                    var asset = singleSource.GetAsset(attribute.Group);
                    if (asset != null)
                    {
                        yield return asset;
                    }
                }
                else
                {
                    var folderPath = GetAssetPathFolder(source.GetAssetPath());
                    var assetPaths = AssetDatabase.FindAssets($"t:{nameof(TextAsset)}", new[] {folderPath})
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(IsSupportedTextAssetPath)
                        .ToArray();

                    var multiSource = source as FolderSource;
                    if (multiSource == null)
                    {
                        yield break;
                    }

                    foreach (var assetPath in assetPaths)
                    {
                        if (multiSource.IsRecursive ||
                            GetAssetPathFolder(assetPath) == folderPath)
                        {
                            yield return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                        }
                    }
                }
            }
        }

        internal static string GetAssetPathFolder(this string assetPath)
        {
            return Path.GetDirectoryName(assetPath);
        }
    }
}