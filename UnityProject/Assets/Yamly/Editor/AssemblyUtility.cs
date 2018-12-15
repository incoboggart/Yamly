using System;
using System.Linq;
using System.Reflection;

using UnityEditor;

using UnityEngine;

using Yamly.Proxy;
using Yamly.UnityEditor;

namespace Yamly
{
    internal static class AssemblyUtility
    {
        public static bool IsProjectAssembly(this Assembly assembly)
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
        
        public static YamlyAssembliesProvider GetAssemblies()
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
            
            return new YamlyAssembliesProvider
            {
                All = assemblies,
                ProxyAssembly = proxyAssembly,
                IsProxyAssemblyInvalid = isProxyAssemblyInvalid,
                MainRuntimeAssembly = assemblies.FirstOrDefault(a => "Assembly-CSharp".Equals(a.GetName().Name)),
                MainEditorAssembly = assemblies.FirstOrDefault(a => "Assembly-CSharp-Editor".Equals(a.GetName().Name))
            };
        }
        
        private static Assembly[] GetProjectAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(IsProjectAssembly).ToArray();
        }

        private static Assembly GetProxyAssembly(Assembly[] assemblies)
        {
            return assemblies.FirstOrDefault(a => a.Have<YamlyProxyAssemblyAttribute>());
        }
        
        public static bool IsProjectPath(string path)
        {
            return path.StartsWith(Application.dataPath);
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