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
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Yamly.UnityEditor
{
    public static class ConvertMenuItems
    {
        public const string JsonFileExtension = "." + AssetUtility.JsonFileExtension;
        public const string YamlFileExtension = "." + AssetUtility.YamlFileExtension;
        
        [MenuItem("Assets/Convert To/JSON", isValidateFunction: true)]
        private static bool ToJsonValidate()
        {
            return IsSelectionValid();
        }

        [MenuItem("Assets/Convert To/JSON")]
        private static void ToJsonExecute()
        {
            ConvertSelectionFormat(ConvertUtility.ToJson, 
                JsonFileExtension);
        }

        [MenuItem("Assets/Convert To/YAML", isValidateFunction: true)]
        private static bool ToYamlValidate()
        {
            return IsSelectionValid();
        }
        
        [MenuItem("Assets/Convert To/YAML")]
        private static void ToYamlExecute()
        {
            ConvertSelectionFormat(ConvertUtility.ToYaml, 
                YamlFileExtension);
        }
        
//        [MenuItem("Assets/Convert To/Pascal case", isValidateFunction: true)]
//        private static bool ToPascalCaseValidate()
//        {
//            return IsSelectionValid();
//        }
//        
//        [MenuItem("Assets/Convert To/Pascal case")]
//        private static void ToPascalCaseExecute()
//        {
//            ConvertSelectionCase(NamingConvention.Pascal);
//        }
//        
//        [MenuItem("Assets/Convert To/Camel case", isValidateFunction: true)]
//        private static bool ToCamelCaseValidate()
//        {
//            return IsSelectionValid();
//        }
//        
//        [MenuItem("Assets/Convert To/Camel case")]
//        private static void ToCamelCaseExecute()
//        {
//            ConvertSelectionCase(NamingConvention.Camel);
//        }
//        
//        [MenuItem("Assets/Convert To/Hyphenated case", isValidateFunction: true)]
//        private static bool ToHyphenatedCaseValidate()
//        {
//            return IsSelectionValid();
//        }
//        
//        [MenuItem("Assets/Convert To/Hyphenated case")]
//        private static void ToHyphenatedCaseExecute()
//        {
//            ConvertSelectionCase(NamingConvention.Hyphenated);
//        }
//        
//        [MenuItem("Assets/Convert To/Underscored case", isValidateFunction: true)]
//        private static bool ToUnderscoredCaseValidate()
//        {
//            return IsSelectionValid();
//        }
//        
//        [MenuItem("Assets/Convert To/Underscored case")]
//        private static void ToUnderscoredCaseExecute()
//        {
//            ConvertSelectionCase(NamingConvention.Underscored);
//        }

        private static void ConvertSelectionFormat(Func<string, NamingConvention?, bool?, string> convertFunc,
            string targetExtension,
            NamingConvention? namingConvention = null,
            bool? ignoreUnmatchedProperties = null)
        {
            var textAssets = Selection.objects.Where(AssetUtility.IsSupportedTextAsset)
                .Distinct()
                .Cast<TextAsset>()
                .ToArray();

            AssetDatabase.StartAssetEditing();
            {
                try
                {
                    var owerwriteAssets = new List<string>();
                    foreach (var textAsset in textAssets)
                    {
                        var assetPath = textAsset.GetAssetPath();
                        var currentExtension = Path.GetExtension(assetPath); 
                        var outputPath = assetPath
                            .Replace(currentExtension, targetExtension);

                        var asset = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
                        if (asset != null 
                            && asset != textAsset)
                        {
                            owerwriteAssets.Add(outputPath);
                        }
                    }

                    if (owerwriteAssets.Any())
                    {
                        var stringBuilder = new StringBuilder()
                            .AppendLine("These assets will be overwritten:");

                        foreach (var assetPath in owerwriteAssets)
                        {
                            stringBuilder.AppendLine(assetPath);
                        }

                        if (!EditorUtility.DisplayDialog("Confirm assets overwrite",
                            stringBuilder.ToString(),
                            "OK",
                            "Cancel"))
                        {
                            return;   
                        }
                    }

                    foreach (var textAsset in textAssets)
                    {
                        var assetPath = textAsset.GetAssetPath();
                        var currentExtension = Path.GetExtension(assetPath); 
                        var outputPath = assetPath.ToSystemPath()
                            .Replace(currentExtension, targetExtension);
                        
                        var output = convertFunc(textAsset.text, namingConvention, ignoreUnmatchedProperties);
                        File.WriteAllText(outputPath, output);
                        
                        EditorUtility.SetDirty(textAsset);
                    }  
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }         
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        private static void ConvertSelectionCase(NamingConvention? namingConvention = null,
            bool? ignoreUnmatchedProperties = null)
        {
            var textAssets = Selection.objects.Where(AssetUtility.IsSupportedTextAsset)
                .Distinct()
                .Cast<TextAsset>()
                .ToArray();

            AssetDatabase.StartAssetEditing();
            {
                try
                {
                    foreach (var textAsset in textAssets)
                    {
                        var assetPath = textAsset.GetAssetPath();
                        var currentExtension = Path.GetExtension(assetPath);
                        Func<string, NamingConvention?, bool?, string> convertFunc = null;
                        if (JsonFileExtension.Equals(currentExtension))
                        {
                            convertFunc = ConvertUtility.ToJson;
                        }

                        if (YamlFileExtension.Equals(currentExtension))
                        {
                            convertFunc = ConvertUtility.ToYaml;
                        }
                        
                        // To convert naming convention correctly its required to know data type and deserialize according to type
                        // Otherwise data is deserialized as Dictionary<string, string> and no conversion over keys is performed
                        var outputPath = assetPath.ToSystemPath();
                        var output = convertFunc(textAsset.text, namingConvention, ignoreUnmatchedProperties);
                        File.WriteAllText(outputPath, output);
                        
                        EditorUtility.SetDirty(textAsset);
                    }  
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }         
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
        
        private static bool IsSelectionValid()
        {
            return Selection.objects.Any(AssetUtility.IsSupportedTextAsset);
        }
    }
}