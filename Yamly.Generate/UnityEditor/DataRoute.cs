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
using System.Reflection;

using UnityEngine;

using Yamly.CodeGeneration;

namespace Yamly.UnityEditor
{
    internal class DataRoute
    {
        public RootDefinition Root;
        public AssetDeclarationAttributeBase Attribute;
        public List<Storage> Storages;
        public List<SourceBase> Sources;

        public string Group => Attribute.Group;
        public Type RootType => Root.Root;
        public MethodInfo KeySourceMethodInfo => RootType.GetKeySourceMethodInfo(Attribute as AssetDictionaryAttribute);

        public bool ContainsCodeFile(string assetPath)
        {
            return Code.Contains(assetPath);
        }

        public IEnumerable<string> GetAssetPaths()
        {
            foreach (var assetPath in FileAssetPaths.Where(AssetUtility.IsSupportedTextAssetPath))
            {
                yield return assetPath;
            }

            foreach (var folderAssetPath in FolderAssetPaths)
            {
                foreach (var assetPath in AssetUtility.GetAssetPaths<TextAsset>(folderAssetPath))
                {
                    if (assetPath.GetAssetPathFolder() == folderAssetPath)
                    {
                        yield return assetPath;
                    }
                }
            }

            foreach (var folderAssetPath in RootAssetPaths)
            {
                foreach (var assetPath in AssetUtility.GetAssetPaths<TextAsset>(folderAssetPath))
                {
                    yield return assetPath;
                }
            }
        }

        public bool ContainsStorage(string assetPath)
        {
            return Storages.Exists(s => s.GetAssetPath() == assetPath);
        }

        public bool ContainsSource(string assetPath)
        {
            return Sources.Exists(s => s.GetAssetPath() == assetPath);
        }

        public bool ContainsAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (FileAssetPaths.Contains(assetPath))
            {
                return true;
            }

            if (FolderAssetPaths.Exists(p => p == System.IO.Path.GetDirectoryName(assetPath)))
            {
                return true;
            }

            if (RootAssetPaths.Exists(assetPath.StartsWith))
            {
                return true;
            }

            return false;
        }

        public List<string> Code;

        // Exact file
        public List<string> FileAssetPaths { get; } = new List<string>();

        // Exact folder
        public List<string> FolderAssetPaths { get; } = new List<string>();

        // Recursive
        public List<string> RootAssetPaths { get; } = new List<string>();

    }
}