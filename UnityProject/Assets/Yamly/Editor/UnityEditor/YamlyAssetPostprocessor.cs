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
using System.Text;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEditor.Callbacks;

using UnityEngine;

using YamlDotNet.Serialization;

using Yamly.CodeGeneration;
using Yamly.Proxy;

using Object = UnityEngine.Object;

namespace Yamly.UnityEditor
{
    [Serializable]
    internal class YamlyPostprocessAssetsContext
    {
        public string[] ImportedAssets;
        public string[] DeletedAssets;
        public string[] MovedAssets;
        public string[] MovedFromAssetPaths;

        public IEnumerable<string> ChangedAndDeletedAssets => ImportedAssets.Concat(DeletedAssets);

        public IEnumerable<string> All => ImportedAssets.Concat(DeletedAssets).Concat(MovedAssets).Concat(MovedFromAssetPaths);
    }

    internal class YamlyProjectAssemblies
    {
        public Assembly[] All;
        public Assembly MainRuntimeAssembly;
        public Assembly MainEditorAssembly;
        public Assembly ProxyAssembly;
        public bool IsProxyAssemblyInvalid;
        
        public Assembly[] TargetAssemblies => All
            .Except(new[] {MainRuntimeAssembly, MainEditorAssembly, ProxyAssembly})
            .ToArray();

        public Assembly[] IgnoreAssemblies => All
            .Where(a => a.Have<IgnoreAttribute>())
            .ToArray();
    }

    internal class YamlyProjectContext
    {
        public Assembly ProxyAssembly;
        public List<RootDefinition> Roots;
        public List<Storage> Storages;
        public List<SourceBase> Sources;

        private AssetProcessor _assetProcessor;
        private string[] _groups;

        public AssetProcessor AssetProcessor => _assetProcessor ?? (_assetProcessor = new AssetProcessor(ProxyAssembly));

        public string[] Groups => _groups ?? (_groups = Roots.SelectMany(r => r.Attributes)
                                      .Select(a => a.Group)
                                      .Where(CodeGenerationUtility.IsValidGroupName)
                                      .Distinct()
                                      .ToArray());

    }

    public sealed class YamlyAssetPostprocessor 
        : AssetPostprocessor
    {       
        private static List<RootDefinition> _roots;

        private const string NamespacePatternBase = @"((namespace){1}){1}[\s\S]+NamespaceName{1}(?![a-zA-Z\d])(?=[\s\S]+|{)";
        private const string ClassPatternBase = @"((internal|public|private|protected|sealed|abstract|static)?[\s\r\n\t]+){0,2}(class|struct){1}[\s\S]+ClassName{1}(?![a-zA-Z\d])(?=[\s\S]+|{)";

        private static bool _rebuildAssemblyAfterReloadScriptsPending;
        
        private static YamlySettings Settings => YamlySettings.Instance;
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            try
            {
                ProcessAssets(new YamlyPostprocessAssetsContext
                {
                    ImportedAssets = importedAssets,
                    DeletedAssets = deletedAssets,
                    MovedAssets = movedAssets,
                    MovedFromAssetPaths = movedFromAssetPaths
                });
            }
            catch (ReflectionTypeLoadException)
            {
                // This means we removed root and recompile the code
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // Можем импортнуть файл кода или библиотеку, в котором есть конфиг. Надо пересобрать библиотеку.
        // Можем удалить файл кода или библиотеку, в котором есть конфиг. Надо пересобрать библиотеку.

        
        // Можем переместить импортнуть ассет, который попадает в источник. Надо пересобрать все машруты с этим ассетом.
        // Можем удалить ассет, который попадает в источник. Надо пересобрать все машруты с этим ассетом.
        // Можем переместить ассет, который попадает в источник. Надо проверить, что исходный и конечный источник тот же. Если тот же - игнорировать. Если изменился - надо пересобрать все маршруты.
        // Можем импортнуть новый источник. Надо пересобрать все его маршруты.
        // Можем удалить источник. Надо пересобрать все маршруты, которые он затрагивал. 
        // Можем переместить источни. Надо пересобрать его маршруты.
        // Можем импортнуть новое хранилище. Надо пересобрать все его маршруты.

        // Можем переместить файл кода или библиотеку, в котором есть конфиг. Ничего не делать.
        // Можем удалить хранилище. Ничего не делать.
        // Можем переместить хранилище. Ничего не делать.
        private static void ProcessAssets(YamlyPostprocessAssetsContext ctx)
        {
            var assemblies = GetAssemblies();

            InitRoots(assemblies);

            if (!_rebuildAssemblyAfterReloadScriptsPending)
            {
                if (assemblies.ProxyAssembly == null
                    || assemblies.IsProxyAssemblyInvalid
                    || string.IsNullOrEmpty(assemblies.ProxyAssembly.Location))
                {
                    if (_roots.Any())
                    {
                        BuildAssembly(assemblies);
                    }
                    return;
                }
            }

            var codeAssets = AssetDatabase.FindAssets($"t:{nameof(TextAsset)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".cs"))
                .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
                .ToArray();

            var codeFilePaths = _roots.SelectMany(r => GetCodeFiles(r, codeAssets))
                .Distinct()
                .ToList();
            
            var exludedAssemblies = new List<string>
            {
                CodeGenerationUtility.GetUtilityAssemblySystemPath().ToAssetsPath()
            };

            var proxyAssemblyLocation = assemblies.ProxyAssembly?.Location.ToAssetsPath();
            if (!string.IsNullOrEmpty(proxyAssemblyLocation))
            {
                exludedAssemblies.Add(proxyAssemblyLocation);
            }

            var rebuildAssembly = false;
            foreach (var assetPath in ctx.ChangedAndDeletedAssets)
            {
                if (assetPath.EndsWith(".cs"))
                {
                    if (codeFilePaths.Contains(assetPath))
                    {
                        rebuildAssembly = true;
                    }
                }
                
                if (assetPath.EndsWith(".dll"))
                {
                    if (!exludedAssemblies.Contains(assetPath))
                    {
                        rebuildAssembly = true;
                    } 
                }
            }

            if (rebuildAssembly 
                || _rebuildAssemblyAfterReloadScriptsPending)
            {
                if (!_rebuildAssemblyAfterReloadScriptsPending)
                {
                    YamlyEditorPrefs.IsAssemblyBuildPending = true;
                }
                YamlyEditorPrefs.AssetImportContext = new YamlyPostprocessAssetsContext
                {
                    ImportedAssets = ctx.ImportedAssets.Where(p => !IsCodeFilePath(p)).ToArray(),
                    DeletedAssets = ctx.DeletedAssets.Where(p => !IsCodeFilePath(p)).ToArray(),
                    MovedAssets = ctx.MovedAssets.Where(p => !IsCodeFilePath(p)).ToArray(),
                    MovedFromAssetPaths = ctx.MovedFromAssetPaths.Where(p => !IsCodeFilePath(p)).ToArray()
                };
                return;
            }
            
            if (_roots.Count == 0)
            {
                return;
            }

            var sourceDefinitions = AssetUtility.LoadAll<FolderSource>()
                .Cast<SourceBase>()
                .Concat(AssetUtility.LoadAll<SingleSource>())
                .ToList();
            if (sourceDefinitions.Count == 0)
            {
                return;
            }

            var groups = _roots.SelectMany(r => r.Attributes)
                .Select(a => a.Group)
                .Distinct()
                .ToArray();
            var storageDefinitions = AssetUtility.LoadAll<Storage>().ToList();
            for (var i = 0; i < storageDefinitions.Count; i++)
            {
                var storage = storageDefinitions[i];
                storage.ExcludedGroups.RemoveAll(g => !groups.Contains(g));
                foreach (var s in storage.Storages)
                {
                    if (s == null 
                        || !storage.Includes(s.Group))
                    {
                        Object.DestroyImmediate(s, true);
                        
                        EditorUtility.SetDirty(storage);
                    }
                }

                storage.Storages.RemoveAll(s => s == null);

                var assets = AssetDatabase.LoadAllAssetsAtPath(storage.GetAssetPath());
                var haveNullAssets = false;
                foreach (var asset in assets)
                {
                    if (asset == storage)
                    {
                        continue;
                    }

                    if (storage.Storages.Contains(asset))
                    {
                        continue;
                    }

                    if (asset == null)
                    {
                        haveNullAssets = true;
                    }

                    Object.DestroyImmediate(asset, true);
                }

                if (haveNullAssets)
                {
                    var assetPath = storage.GetAssetPath();
                    var storages = storage.Storages.ToArray();
                    var storageName = storage.name;
                    storage = Object.Instantiate(storage);
                    storage.name = storageName;
                    storage.Storages.Clear();
                    foreach (var storageBase in storages)
                    {
                        var instance = Object.Instantiate(storageBase);
                        instance.name = storageBase.name;
                        
                        storage.Storages.Add(instance);
                    }
                    
                    if (Settings.VerboseLogs)
                    {
                        Debug.Log($"Storage at path ${assetPath} contains missing storage instances and will be overwritten.", storage);
                    }
                    
                    AssetDatabase.DeleteAsset(assetPath);
                    
                    AssetDatabase.StartAssetEditing();
                    {
                        AssetDatabase.CreateAsset(storage, assetPath);
                        foreach (var storageBase in storage.Storages)
                        {
                            AssetDatabase.AddObjectToAsset(storageBase, assetPath);
                        }
                    }
                    AssetDatabase.StopAssetEditing();
                    
                    AssetDatabase.ImportAsset(assetPath);

                    storageDefinitions[i] = storage;
                }
            }
            
            var routes = new List<DataRoute>();
            foreach (var d in _roots)
            {
                var codePaths = GetCodeFiles(d, codeAssets);
                routes.AddRange(d.Attributes.Select(a => CreateRoute(d, a, sourceDefinitions, storageDefinitions, codePaths)));
            }

            var routesToRebuild = new List<DataRoute>();
            
            foreach (var assetPath in ctx.All)
            {
                foreach (var route in routes)
                {
                    if (routesToRebuild.Contains(route))
                    {
                        continue;
                    }

                    if (route.ContainsAsset(assetPath)
                        || route.ContainsSource(assetPath)
                        || route.ContainsStorage(assetPath))
                    {
                        routesToRebuild.Add(route);
                    }
                }
            }

            var context = new YamlyProjectContext
            {
                ProxyAssembly = assemblies.ProxyAssembly,
                Storages = storageDefinitions,
                Sources = sourceDefinitions,
                Roots = _roots
            };
            foreach (var route in routesToRebuild)
            {
                Rebuild(route, context);
            }

            AssetDatabase.Refresh();
        }

        private static Assembly[] GetProjectAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(IsProjectAssembly).ToArray();
        }

        private static Assembly GetProxyAssembly(Assembly[] assemblies)
        {
            return assemblies.FirstOrDefault(a => a.Have<YamlyProxyAssemblyAttribute>());
        }

        private static bool IsCodeFilePath(string assetPath)
        {
            return assetPath.EndsWith(".cs") || assetPath.EndsWith(".dll");
        }

        [DidReloadScripts]
        private static void OnDidReloadScripts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling)
            {
                return;
            }

            var rebuildAssembly = false;
            var assemblies = GetAssemblies();
            
            try
            {
                InitRoots(assemblies);

                if (assemblies.ProxyAssembly == null
                    || assemblies.IsProxyAssemblyInvalid)
                {
                    rebuildAssembly = true;
                }
                else
                {
                    var proxyTypes = assemblies.ProxyAssembly.GetTypes().Where(t => t.Have<ProxyAttribute>()).ToList();
                    foreach (var root in _roots)
                    {
                        if (proxyTypes.Exists(t => root.Contains(t.GetSingle<ProxyAttribute>().OriginType)))
                        {
                            continue;
                        }

                        rebuildAssembly = true;
                        break;
                    }

                    if (!rebuildAssembly)
                    {
                        foreach (var proxyType in proxyTypes)
                        {
                            var originType = proxyType.GetSingle<ProxyAttribute>().OriginType;
                            if (originType != null
                                && _roots.Exists(r => r.Contains(originType)))
                            {
                                continue;
                            }

                            rebuildAssembly = true;
                            break;
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                rebuildAssembly = true;
            }
            
            if (rebuildAssembly || YamlyEditorPrefs.IsAssemblyBuildPending)
            {
                YamlyEditorPrefs.IsAssemblyBuildPending = false;
                _rebuildAssemblyAfterReloadScriptsPending = true;

                if (_roots.Any())
                {
                    BuildAssembly(assemblies);
                }
            }
            else if (YamlyEditorPrefs.IsAssetsImportPending)
            {
                ProcessAssets(YamlyEditorPrefs.AssetImportContext);

                YamlyEditorPrefs.AssetImportContext = null;
            }

            if (!_rebuildAssemblyAfterReloadScriptsPending)
            {
                CleanupStorages();
            }
        }

        private static void CleanupStorages()
        {
            var storageDefinitions = AssetUtility.LoadAll<Storage>().ToList();
            foreach (var storageDefinition in storageDefinitions)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(storageDefinition.GetAssetPath());
                var needsCleanup = false;
                foreach (var asset in assets)
                {
                    if (asset == null)
                    {
                        Object.DestroyImmediate(asset, true);
                        needsCleanup = true;
                    }
                }

                if (needsCleanup)
                {
                    storageDefinition.Cleanup();
                    EditorUtility.SetDirty(storageDefinition);
                }
            }
        }

        private static DataRoute CreateRoute(RootDefinition d, 
            AssetDeclarationAttributeBase a, 
            List<SourceBase> sources, 
            List<Storage> storages,
            List<string> codePaths)
        {
            var route = new DataRoute
            {
                Root = d,
                Attribute = a,
                Sources = sources.FindAll(s => s.Contains(a.Group)),
                Storages = storages.FindAll(s => s.Includes(a.Group)),
                Code = codePaths
            };

            if (route.Attribute.IsSingle())
            {
                var storage = route.Sources.Where(s1 => s1.IsSingle)
                    .Cast<SingleSource>()
                    .FirstOrDefault(s => s != null && s.GetAsset(route.Group) != null);
                var asset = storage != null ? storage.GetAsset(route.Group) : null;
                if (asset != null)
                {
                    route.FileAssetPaths.Add(asset.GetAssetPath());
                }
            }
            else
            {
                foreach (var source in route.Sources)
                {
                    if (source.IsSingle)
                    {
                        var asset = source.GetSingleAsset(route.Group);
                        if (asset != null)
                        {
                            route.FileAssetPaths.Add(asset.GetAssetPath());
                        }
                    }
                    else
                    {
                        var isRecursive = (source as FolderSource)?.IsRecursive ?? false;
                        var paths = isRecursive ? route.RootAssetPaths : route.FolderAssetPaths;
                        paths.Add(source.GetAssetPathFolder());
                    }
                }

                route.FileAssetPaths.AddRange(route.Sources.SelectMany(s => s.GetAssets(route.Attribute)).Select(textAsset => textAsset.GetAssetPath()));
            }

            foreach (var storage in route.Storages)
            {
                route.FileAssetPaths.Add(storage.GetAssetPath());
            }

            foreach (var source in route.Sources)
            {
                route.FileAssetPaths.Add(source.GetAssetPath());
            }

            return route;
        }

        private static DataRoute CreateRoute(RootDefinition d,
            AssetDeclarationAttributeBase a,
            List<SourceBase> sources,
            List<Storage> storages)
        {
            return CreateRoute(d, a, sources, storages, new List<string>());
        }

        private static DataRoute CreateRoute(RootDefinition d,
            AssetDeclarationAttributeBase a,
            YamlyProjectContext c)
        {
            return CreateRoute(d, a, c.Sources, c.Storages);
        }

        private static void BuildAssembly(YamlyProjectAssemblies assemblies, bool debug = false)
        {
            var outputAssetsPath = CodeGenerationUtility.GetProxyAssemblyOutputPath(assemblies.ProxyAssembly);

            if (Settings.VerboseLogs)
            {
                Debug.Log($"Build proxy assembly at path {outputAssetsPath}");
            }

            var assetPathsToReimport = new List<string>(2);

            var outputSystemPath = Application.dataPath.Replace("Assets", outputAssetsPath);
            var assemblyBuilder = new ProxyAssemblyBuilder
            {
                TargetAssemblies = assemblies.TargetAssemblies,
                IgnoreAssemblies = assemblies.IgnoreAssemblies,
                OutputAssembly = outputSystemPath,
                TreatWarningsAsErrors = true,
                IncludeDebugInformation = debug
            }.Build();

            if (assemblyBuilder.CompilerResults.Errors.Count != 0)
            {
                Debug.LogError($"Proxy assembly builder have {assemblyBuilder.CompilerResults.Errors.Count} errors!");
                foreach (var error in assemblyBuilder.CompilerResults.Errors)
                {
                    Debug.LogError(error);
                }

                return;
            }
            
            if(Settings.VerboseLogs)
            {
                Debug.Log("Proxy assembly builder have no errors.");
            }

            assetPathsToReimport.Add(outputAssetsPath);

            if (assemblies.ProxyAssembly != null)
            {
                outputSystemPath = CodeGenerationUtility.GetUtilityAssemblySystemPath();
                outputAssetsPath = outputSystemPath.ToAssetsPath();

                if (Settings.VerboseLogs)
                {
                    Debug.Log($"Build utility assembly at path {outputAssetsPath}");
                }

                var groups = _roots.SelectMany(r => r.Attributes).Select(a => a.Group).Distinct().ToArray();
                var utilityBuilder = new UtilityAssemblyBuilder(groups)
                {
                    OutputAssembly = outputSystemPath,
                    TargetNamespace = "Yamly.UnityEditor.Utility"
                }.Build();
                
                if (utilityBuilder.CompilerResults.Errors.Count != 0)
                {
                    Debug.LogError($"Utility assembly builder have {utilityBuilder.CompilerResults.Errors.Count} errors!");
                    foreach (var error in utilityBuilder.CompilerResults.Errors)
                    {
                        Debug.LogError(error);
                    }

                    return;
                }

                if (Settings.VerboseLogs)
                {
                    Debug.Log("Utility assembly builder have no errors.");
                }

                assetPathsToReimport.Add(utilityBuilder.OutputAssembly.ToAssetsPath());
            }
            else
            {
                if (Settings.VerboseLogs)
                {
                    Debug.Log("Delay building utility assembly until proxy assembly imported.");
                }
                
                YamlyEditorPrefs.IsAssemblyBuildPending = true;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var assetPath in assetPathsToReimport)
                {
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }

        private static List<string> GetCodeFiles(RootDefinition root, TextAsset[] textAssets)
        {
            var rootAssembly = root.Root.Assembly;
            if (IsProjectAssembly(rootAssembly) && IsProjectPath(rootAssembly.Location))
            {
                return new List<string>{rootAssembly.Location.ToAssetsPath()};
            }

            var namespaces = root.Namespaces.Select(n => new Regex(NamespacePatternBase.Replace("NamespaceName", n))).ToArray();
            var classes = root.Types.Select(t => new Regex(ClassPatternBase.Replace("ClassName", t.Name))).ToArray();
            return textAssets.Where(a => namespaces.Any(n => n.IsMatch(a.text)))
                .Where(a => classes.Any(c => c.IsMatch(a.text)))
                .Select(a => a.GetAssetPath())
                .ToList();
        }

        private static bool IsProjectPath(string path)
        {
            return path.StartsWith(Application.dataPath);
        }

        private static bool IsProjectAssembly(Assembly assembly)
        {
            string location;
            try
            {
                location = assembly.Location;
            }
            catch (Exception)
            {
                return false;
            }

            location = location.Replace("\\", "/");

            if (location.StartsWith(EditorApplication.applicationContentsPath))
            {
                return false;
            }

            return true;
        }

        [MenuItem("Yamly/Generate code")]
        public static void CompileAssembly()
        {
            var assemblies = GetAssemblies();
            InitRoots(assemblies);
            
            if (_roots.Any() 
                && assemblies.IsProxyAssemblyInvalid)
            {
                BuildAssembly(assemblies);
            }
            else
            {
                Debug.Log("Project have no asset types declared. Ignoring compile.");   
            }
        }

        [MenuItem("Yamly/Validate/All", priority = 100)]
        public static void ValidateAll()
        {
            var ctx = GetContext();
            if (ctx == null 
                || ctx.Groups.Length == 0)
            {
                return;
            }

            var p = 0f;
            var s = 1f / ctx.Groups.Length;

            foreach (var group in ctx.Groups)
            {
                EditorUtility.DisplayProgressBar("Yamly:Validate all groups", group, p);
                try
                {
                    Validate(group, ctx);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                p += s;
            }
            
            EditorUtility.ClearProgressBar();
        }

        internal static void Validate(string groupName, YamlyProjectContext ctx)
        {
            if (ctx == null)
            {
                ctx = GetContext();
            }
           
            if (ctx.Sources.Count == 0)
            {
                Debug.LogWarning("No source definitions exist! Validation canceled!");
                return;
            }

            var targetRoot = _roots.Find(r => r.Contains(groupName));
            var attribute = targetRoot.Attributes.Find(a => a.Group == groupName);
            var route = CreateRoute(targetRoot, attribute, ctx);

            if (route.Sources.Count == 0)
            {
                Debug.LogWarning($"Group {groupName} have no sources.");
                return;
            }
            
            Debug.Log($"Validate {groupName} with {ctx.Sources.Count} sources and {ctx.Storages.Count} storages");

            var result = ctx.AssetProcessor.Validate(route);
            if (result.Errors.Any())
            {
                foreach (var e in result.Errors)
                {
                    Debug.LogError(e.Error, e.TextAsset);
                }
            }
            else
            {
                Debug.Log($"Group {groupName} have no errors!");
            }
        }

        public static void Validate(string groupName)
        {
            Validate(groupName, null);
        }

        [MenuItem("Yamly/Rebuild/All", priority = 100)]
        public static void RebuildAll()
        {
            var ctx = GetContext();
            if (ctx == null 
                || ctx.Groups.Length == 0)
            {
                return;
            }

            var p = 0f;
            var s = 1f / ctx.Groups.Length;

            foreach (var group in ctx.Groups)
            {
                EditorUtility.DisplayProgressBar("Yamly:Validate all groups", group, p);
                try
                {
                    Rebuild(group, ctx);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                p += s;
            }

            EditorUtility.ClearProgressBar();
        }

        internal static void Rebuild(DataRoute route, YamlyProjectContext ctx)
        {
            var groupName = route.Group;
            if (route.Sources.Count == 0)
            {
                Debug.LogWarning($"Group {groupName} have no sources.");
                return;
            }

            if (route.Storages.Count == 0)
            {
                Debug.LogWarning($"Group {groupName} have no storages.");
                return;
            }

            if (Settings.VerboseLogs)
            {
                Debug.Log($"Rebuild group {groupName} from {route.Sources.Count} sources to {route.Storages.Count} storages.");
            }

            var result = ctx.AssetProcessor.Rebuild(route);
            foreach (var e in result.Errors)
            {
                Debug.LogError(e.Error, e.TextAsset);
            }
        }

        internal static void Rebuild(string groupName, YamlyProjectContext ctx)
        {
            if (ctx == null)
            {
                ctx = GetContext();
            }
            
            var root = ctx.Roots.Find(r => r.Contains(groupName));
            var attribute = root.Attributes.Find(a => a.Group == groupName);
            var route = CreateRoute(root, attribute, ctx.Sources, ctx.Storages);

            Rebuild(route, ctx);
        }

        public static void Rebuild(string groupName)
        {
            Rebuild(groupName, null);
        }
        
        public static void CreateDefaultAssetOnSelection(string group)
        {
            CreateDefaultAssetOnSelection(group, null);
        }

        internal static void CreateDefaultAssetOnSelection(string group, YamlyProjectContext ctx)
        {
            if (ctx == null)
            {
                ctx = GetContext();
            }

            var root = ctx.Roots.Find(r => r.Contains(group));
            var assetPath = Selection.activeObject.GetAssetPath();
            var folderPath = string.IsNullOrEmpty(System.IO.Path.GetExtension(assetPath))
                ? assetPath
                : assetPath.GetAssetPathFolder();
            var fileName = root.Root.Name;
            var targetPath = $"{folderPath}{fileName}.yaml";
            var index = 1;
            while (System.IO.File.Exists(targetPath.ToSystemPath()))
            {
                targetPath = $"{folderPath}{fileName}{index}.yaml";
                index++;
            }

            object instance;

            var constructors = root.Root.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic).ToList();
            if (constructors.Count == 0)
            {
                instance = Activator.CreateInstance(root.Root);
            }
            else
            {
                var parameterlessConstructor = constructors.Find(c => c.GetParameters().Length == 0);
                if (parameterlessConstructor != null)
                {
                    instance = Activator.CreateInstance(root.Root);
                }
                else
                {
                    var constructor = constructors.OrderBy(c => c.GetParameters().Length).First();
                    var parameters = constructor.GetParameters();
                    var parameterValues = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameterType = parameters[i].ParameterType;
                        if (parameterType.IsValueType)
                        {
                            parameterValues[i] = Activator.CreateInstance(parameterType);
                        }
                        else
                        {
                            parameterValues[i] = null;
                        }
                    }

                    instance = Activator.CreateInstance(root.Root, parameterValues);
                }
            }

            var serializer = new SerializerBuilder().EmitDefaults().Build();
            var text = serializer.Serialize(instance);

            System.IO.File.WriteAllText(targetPath.ToSystemPath(), text);
            AssetDatabase.Refresh();
        }

        private static void InitRoots(YamlyProjectAssemblies assemblies)
        {
            if (_roots != null)
            {
                return;
            }

            var gen = new ProxyCodeGenerator
            {
                TargetAssemblies = assemblies.All.Except(assemblies.IgnoreAssemblies).ToArray()
            };

            _roots = new List<RootDefinition>();
            foreach (var r in gen.GetRootDefinitions().ToList())
            {
                if (r.Assembly == assemblies.MainRuntimeAssembly ||
                    r.Assembly == assemblies.MainEditorAssembly)
                {
                    Debug.LogWarning($"Config root {r.Root.FullName} is defined in assembly {r.Assembly.FullName}. This is not supported - please put it into separate assembly with AssemblyDefinition or manually.");
                    continue;
                }

                _roots.Add(r);
            }

            var groups = _roots.SelectMany(r => r.Attributes)
                .Select(a => a.Group)
                .ToList();
            foreach (var group in groups.Distinct())
            {
                if (groups.Count(g => g == group) > 1)
                {
                    var roots = _roots.Where(r => r.Contains(group));

                    var log = new StringBuilder();
                    log.AppendFormat("Group name \"{0}\" is declared multiple times. This is not supported.", group).AppendLine();
                    log.AppendLine("Declarations found in these types:");
                    foreach (var root in roots)
                    {
                        log.AppendFormat("{0} ({1})", root.Root.Name, root.Root.AssemblyQualifiedName).AppendLine();
                    }

                    log.AppendLine("These group will be ignored until duplication is fixed.");

                    Debug.LogError(log);
                }
            }
        }

        private static bool IsValidGroupName(string group)
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

        private static YamlyProjectContext GetContext()
        {
            var assemblies = GetProjectAssemblies();
            var proxyAssembly = GetProxyAssembly(assemblies);
            if (proxyAssembly == null)
            {
                return null;
            }

            var mainRuntimeAssembly = assemblies.FirstOrDefault(a => "Assembly-CSharp".Equals(a.GetName().Name));
            var mainEditorAssembly = assemblies.FirstOrDefault(a => "Assembly-CSharp-Editor".Equals(a.GetName().Name));

            var gen = new ProxyCodeGenerator { TargetAssemblies = assemblies };
            var roots = new List<RootDefinition>();
            foreach (var r in gen.GetRootDefinitions().ToList())
            {
                if (r.Assembly == mainEditorAssembly ||
                    r.Assembly == mainRuntimeAssembly)
                {
                    Debug.LogWarning($"Config root {r.Root.FullName} is defined in assembly {r.Assembly.FullName}. This is not supported - please put it into separate assembly with AssemblyDefinition or manually.");
                    continue;
                }

                roots.Add(r);
            }

            var groups = roots.SelectMany(r => r.Attributes)
                .Select(a => a.Group)
                .Where(CodeGenerationUtility.IsValidGroupName)
                .ToList();
            foreach (var group in groups.Distinct())
            {
                if (groups.Count(g => g == group) > 1)
                {
                    var duplicateRoots = roots.Where(r => r.Contains(group));

                    var log = new StringBuilder();
                    log.AppendFormat("Group name \"{0}\" is declared multiple times. This is not supported.", group).AppendLine();
                    log.AppendLine("Declarations found in these types:");
                    foreach (var root in duplicateRoots)
                    {
                        log.AppendFormat("{0} ({1})", root.Root.Name, root.Root.AssemblyQualifiedName).AppendLine();
                    }

                    log.AppendLine("These group will be ignored until duplication is fixed.");

                    Debug.LogError(log);
                }
            }

            var sourceDefinitions = AssetUtility.LoadAll<FolderSource>()
                .Cast<SourceBase>()
                .Concat(AssetUtility.LoadAll<SingleSource>())
                .ToList();

            var storageDefinitions = AssetUtility.LoadAll<Storage>().ToList();
            return new YamlyProjectContext
            {
                ProxyAssembly = proxyAssembly,
                Roots = roots,
                Sources = sourceDefinitions,
                Storages = storageDefinitions
            };
        }

        private static YamlyProjectAssemblies GetAssemblies()
        {
            var assemblies = GetProjectAssemblies();
            var proxyAssembly = GetProxyAssembly(assemblies);

            var isProxyAssemblyInvalid = false;
            if (proxyAssembly != null)
            {
                isProxyAssemblyInvalid = !IsProxyAssemblyValid(proxyAssembly);
                
                if (isProxyAssemblyInvalid)
                {
                    assemblies = assemblies.Except(new[] {proxyAssembly}).ToArray();
                    proxyAssembly = null;
                }
            } 
            
            return new YamlyProjectAssemblies
            {
                All = assemblies,
                ProxyAssembly = proxyAssembly,
                IsProxyAssemblyInvalid = isProxyAssemblyInvalid,
                MainRuntimeAssembly = assemblies.FirstOrDefault(a => "Assembly-CSharp".Equals(a.GetName().Name)),
                MainEditorAssembly = assemblies.FirstOrDefault(a => "Assembly-CSharp-Editor".Equals(a.GetName().Name))
            };
        }

        private static bool IsProxyAssemblyValid(Assembly proxyAssembly)
        {
            try
            {
                var types = proxyAssembly.GetTypes();
                return types.Any();
            }
            catch (Exception)
            {
                
            }
            
            return false; 
        }
    }
}
