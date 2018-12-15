using System.Linq;
using System.Reflection;

using Yamly.Proxy;

namespace Yamly.UnityEditor
{
    internal class YamlyAssembliesProvider
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

        public bool IsAssemblyValidForRoot(Assembly assembly)
        {
            return assembly != MainRuntimeAssembly 
                   && assembly != MainEditorAssembly 
                   && assembly != ProxyAssembly;
        }
    }
}