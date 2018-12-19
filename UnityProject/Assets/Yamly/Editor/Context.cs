using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;

using UnityEngine;

using Yamly.CodeGeneration;
using Yamly.Proxy;
using Yamly.UnityEditor;

using Object = UnityEngine.Object;

namespace Yamly
{
    internal static class Context
    {
        private static readonly Dictionary<string, Object> _assets = new Dictionary<string, Object>();
        private static List<Storage> _storages;
        private static List<SourceBase> _sources;
        
        private static RootDefinitonsProvider _roots;
        private static List<string> _groups;
        private static YamlyAssembliesProvider _assemblies;
        private static AssetProcessor _assetProcessor;
        
        private static bool _init;
        
        public static readonly RegexCache RegexCache = new RegexCache();
        
        public static RootDefinitonsProvider Roots
        {
            get
            {
                if (_roots == null)
                {
                    _roots = new RootDefinitonsProvider();
                    try
                    {
                        _roots.Init(Assemblies);
                    }
                    catch (Exception e)
                    {
                        LogUtils.Verbose(e);
                    }
                }

                return _roots;
            }
        }

        public static List<string> Groups
        {
            get
            {
                if (_groups == null)
                {
                    _groups = Roots.Valid
                        .SelectMany(r => r.Attributes)
                        .Select(a => a.Group)
                        .Where(CodeGenerationUtility.IsValidGroupName)
                        .ToList();
                }
                
                return _groups;
            }
        }

        /// <summary>
        /// Valid group attributes
        /// </summary>
        public static List<AssetDeclarationAttributeBase> Attributes { get; private set; }
        
        public static List<IPostProcessAssets> PostProcessors { get; private set; }
        
        public static IPostProcessAssets[][] OrderedPostProcessors { get; private set; }
        
        public static YamlyAssembliesProvider Assemblies
        {
            get
            {
                if (_assemblies == null)
                {
                    _assemblies = AssemblyUtility.GetAssemblies();
                }

                return _assemblies;
            }
        }
        
        

        public static AssetProcessor AssetProcessor
        {
            get
            {
                if (_assetProcessor == null)
                {
                    _assetProcessor = new AssetProcessor(Assemblies.ProxyAssembly);
                }

                return _assetProcessor;
            }
        }

        public static List<Storage> Storages
        {
            get
            {
                if (_storages == null)
                {
                    _storages = AssetUtility.LoadAll<Storage>().ToList();
                }

                return _storages;
            }
        }

        public static List<SourceBase> Sources
        {
            get
            {
                if (_sources == null)
                {
                    _sources = AssetUtility.LoadAll<FolderSource>()
                        .Cast<SourceBase>()
                        .Concat(AssetUtility.LoadAll<SingleSource>())
                        .ToList();
                }

                return _sources;
            }
        }

        public static T GetAsset<T>(string assetPath)
            where T : Object
        {
            Object asset;
            if (_assets.TryGetValue(assetPath, out asset))
            {
                return asset as T;
            }

            var t = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            _assets[assetPath] = t;
            return t;
        }

        public static void ClearAssetsCache()
        {
            LogUtils.Verbose("Context.ClearAssetsCache");
            
            _assets.Clear();
            _sources = null;
            _storages = null;
        }

        public static void Reset()
        {
            LogUtils.Verbose("Context.Reset");
            
            ClearAssetsCache();
            _init = false;
            _roots = null;
            _groups = null;
            Attributes = null;
            _assetProcessor = null;
            _assemblies = null;
        }

        public static bool IsValid(this AssetDeclarationAttributeBase attribute)
        {
            Init();

            if (attribute == null)
            {
                return false;
            }

            if (Attributes == null)
            {
                return false;
            }
            
            return Attributes.Exists(a => a != null 
                                          && Equals(a, attribute) 
                                          && a.Group == attribute.Group);
        }
        
        public static void Init()
        {
            if (_init)
            {
                return;
            }
            
            LogUtils.Verbose("Context.Init");

            _init = true;

            foreach (var source in Sources)
            {
                _assets[source.GetAssetPath()] = source;
            }

            foreach (var storage in Storages)
            {
                _assets[storage.GetAssetPath()] = storage;
            }
            
            var assemblies = Assemblies;
            if (assemblies.ProxyAssembly == null)
            {
                return;
            }

            try
            {
                var roots = Roots;
                var wellLocatedRoots = new List<RootDefinition>();
                foreach (var r in roots.All)
                {
                    if (!assemblies.IsAssemblyValidForRoot(r.Assembly))
                    {
                        LogUtils.Warning($"Config root {r.Root.FullName} is defined in assembly {r.Assembly.FullName}. This is not supported - please put it into separate assembly with AssemblyDefinition or manually.");
                        continue;
                    }

                    wellLocatedRoots.Add(r);
                }

                var validGroups = new List<string>();
                var validAttributes = new List<AssetDeclarationAttributeBase>();

                var attributes = wellLocatedRoots.SelectMany(r => r.Attributes)
                    .ToList();
                var groups = attributes.Select(a => a.Group)
                    .ToList();
                foreach (var attribute in attributes)
                {
                    if (groups.Count(g => g == attribute.Group) > 1)
                    {
                        var duplicateRoots = wellLocatedRoots.Where(r => r.Contains(attribute.Group));

                        var log = new StringBuilder();
                        log.AppendFormat("Group name \"{0}\" is declared multiple times. This is not supported.",
                                attribute.Group)
                            .AppendLine();
                        log.AppendLine("Declarations found in these types:");
                        foreach (var root in duplicateRoots)
                        {
                            log.AppendFormat("{0} ({1})", root.Root.Name, root.Root.AssemblyQualifiedName)
                                .AppendLine();
                        }

                        log.AppendLine("These group will be ignored until duplication is fixed.");

                        LogUtils.Error(log);
                        continue;
                    }

                    if (!CodeGenerationUtility.IsValidGroupName(attribute.Group))
                    {
                        LogUtils.Error($"Group {attribute.Group} name is not valid! Group will be ignored.");
                        continue;
                    }

                    validAttributes.Add(attribute);
                    validGroups.Add(attribute.Group);
                }

                _groups = validGroups;
                Attributes = validAttributes;
            }
            catch (Exception e)
            {
                LogUtils.Verbose(e);
            }

            try
            {
                var postprocessorType = typeof(IPostProcessAssets);
                var postprocessorTypes = assemblies.All
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && !t.IsInterface)
                    .Where(t => postprocessorType.IsAssignableFrom(t))
                    .ToList();

                PostProcessors = new List<IPostProcessAssets>();
                var ordered = new Dictionary<int, List<IPostProcessAssets>>();
                var orders = new Dictionary<IPostProcessAssets, PostProcessOrderAttribute>();
                var defaultOrder = new PostProcessOrderAttribute();
                Func<int, List<IPostProcessAssets>> getGroup = i =>
                {
                    List<IPostProcessAssets> g;
                    if (ordered.TryGetValue(i, out g))
                    {
                        return g;
                    }
                    g = new List<IPostProcessAssets>();

                    ordered[i] = g;

                    return g;
                };
                Func<IPostProcessAssets, PostProcessOrderAttribute> getOrder = a =>
                {
                    PostProcessOrderAttribute o;
                    if (!orders.TryGetValue(a, out o)
                        || o == null)
                    {
                        o = defaultOrder;
                    }

                    return o;
                };
                foreach (var type in postprocessorTypes)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(type, type.IsNotPublic) as IPostProcessAssets;
                        PostProcessors.Add(instance);

                        var order = type.GetSingle<PostProcessOrderAttribute>()
                            ?? defaultOrder;
                        
                        orders[instance] = order;
                        
                        getGroup(order.Group).Add(instance);
                    }
                    catch (Exception e)
                    {
                        LogUtils.Verbose(e);
                    }
                }

                foreach (var p in ordered)
                {
                    p.Value.Sort((a1, a2) => getOrder(a1).Order.CompareTo(getOrder(a1).Order));
                }

                OrderedPostProcessors = new IPostProcessAssets[ordered.Count][];
                var index = 0;
                foreach (var groupIndex in ordered.Keys.OrderBy(k => k))
                {
                    var g = ordered[groupIndex];
                    if (g == null || g.Count == 0)
                    {
                        continue;
                    }
                    OrderedPostProcessors[index] = g.ToArray();
                    index++;
                }
            }
            catch (Exception e)
            {
                LogUtils.Verbose(e);
            }
        }
    }
}