
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;

using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Yamly.Proxy;
using Yamly.UnityEngine;

using Object = UnityEngine.Object;

namespace Yamly.UnityEditor
{
    public sealed class YamlyAssetPostprocessor 
        : AssetPostprocessor
    {
        private static Dictionary<string, string> Single;
        private static Dictionary<string, string> Multiple;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            PostprocessConfigs(importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths));
        }

        private static void PostprocessConfigs(IEnumerable<string> assetPaths)
        {
            var roots = new TypesFilter().GetApplicableTypes(AppDomain.CurrentDomain.GetAssemblies());
            foreach (var root in roots)
            {
                var attribute = root.Root.Get<ConfigDeclarationAttributeBase>();
            }
            var sources = LoadAll<SourceDefinition>().ToList();
            var storages = LoadAll<StorageDefinition>().ToList();

            Dictionary<string, string> single;
            Dictionary<string, string> rooted;
            Dictionary<string, SourceDefinition> definition;
            Dictionary<string, StorageDefinition> storage;

            foreach (var assetsPath in assetPaths)
            {
                // Check new source
                // Check new definition

                // Check changed config
                //if (single.Keys.Any(k => k == assetsPath))
                //{
                //    // assemble single config
                //    var group = single[assetsPath];
                //}

                //if (rooted.Keys.Any(k => assetsPath.StartsWith(k)))
                //{
                //    // Assemble multiple group and select keys if this is dictionary
                //}
            }
        }

        private static IEnumerable<T> LoadAll<T>()
            where T : Object
        {
            return AssetDatabase.FindAssets($"t:{nameof(T)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(s => s != null);
        }
    }

    public sealed class AssetProcessor
    {
        private readonly Deserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .Build();

        private readonly TypesFilter _typesFilter = new TypesFilter();

        private void TransformRoot(RootDefinition def,
            SourceDefinition[] sources,
            StorageDefinition[] storages)
        {
            var assets = sources.SelectMany(s => s.GetConfigs(def));
            var storage = CreateStorage(GetStorageType(def.Root));
            foreach (var asset in assets)
            {
                // Validate against root
                // If valid - add to root.
                // If invalid - add to error log and skip
                // Write transformed to all storages
            }
        }

        private static Type GetProxyType(Type type)
        {
            throw new NotImplementedException();
        }

        private Type GetStorageType(Type type)
        {
            var typeName = _typesFilter.GetStorageTypeName(type);
            throw new NotImplementedException();
        }

        private T Deserialize<T>(string input)
        {
            return _deserializer.Deserialize<T>(input);
        }

        private static StorageBase CreateStorage(Type type)
        {
            var createFunc =
                Delegate.CreateDelegate(typeof(Func<StorageBase>), typeof(YamlyAssetPostprocessor), nameof(CreateStorage)) as Func<StorageBase>;
            return createFunc();
        }

        private static StorageBase CreateStorage<T>()
            where T : StorageBase
        {
            return ScriptableObject.CreateInstance<T>();
        }
    }


    public static class Extensions
    {
        public static IEnumerable<TextAsset> GetConfigs(this SourceDefinition source, RootDefinition root)
        {
            var folderPath = GetFolder(source.GetAssetPath());
            var assetPaths = AssetDatabase.FindAssets($"t:{nameof(TextAsset)}", new[] {folderPath})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(assetPath => assetPath.EndsWith(".yaml"))
                .ToArray();

            var attribute = root.Root.GetSingle<ConfigDeclarationAttributeBase>();
            if (attribute is SingleConfig)
            {
                var assetPath = assetPaths.FirstOrDefault();
                if (!string.IsNullOrEmpty(assetPath))
                {
                    yield return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                }
            }

            if (attribute is ConfigList ||
                attribute is ConfigDictionary)
            {
                foreach (var assetPath in assetPaths)
                {
                    if (source.IsRecursive ||
                        GetFolder(assetPath) == folderPath)
                    {
                        yield return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                    }
                }
            }
        }

        private static string GetFolder(string assetPath)
        {
            return System.IO.Path.GetDirectoryName(assetPath);
        }

        public static string GetAssetPath<T>(this T asset)
            where T : Object
        {
            return AssetDatabase.GetAssetPath(asset);
        }
    }
}
