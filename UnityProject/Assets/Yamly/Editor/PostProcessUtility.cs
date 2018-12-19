using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Yamly
{
    public static class PostProcessUtility
    {
        private static readonly Type PostProcessSingleType = typeof(IPostProcessSingleAsset<>);
        private static readonly Type PostProcessListType = typeof(IPostProcessAssetList<>);
        private static readonly Type PostProcessDictionaryType = typeof(IPostProcessAssetDictionary<,>);

        private static readonly MethodInfo PostProcessSingleMethodInfo = GetMethod(nameof(PostProcessSingle));
        private static readonly MethodInfo PostProcessListMethodInfo = GetMethod(nameof(PostProcessList));
        private static readonly MethodInfo PostProcessDictionaryMethodInfo = GetMethod(nameof(PostProcessDictionary));
        
        public static DeclarationType GetDeclarationType(this IPostProcessAssets postprocessor)
        {
            var interfaces = postprocessor.GetType().GetInterfaces();

            Func<Type, bool> check = b =>
                b.IsInstanceOfType(postprocessor)
                || interfaces.Any(i => b == i 
                                       || b.IsAssignableFrom(i) 
                                       || (i.IsGenericType && i.GetGenericTypeDefinition() == b));

            if (check(PostProcessSingleType))
            {
                return DeclarationType.Single;
            }
            
            if (check(PostProcessListType))
            {
                return DeclarationType.List;
            }

            if (check(PostProcessDictionaryType))
            {
                return DeclarationType.Dictionary;
            }
            
            throw new NotImplementedException(postprocessor.GetType().FullName);
        }

        public static bool InvokeSingle(object storedValue, IPostProcessAssets postProcessor, Type singleType)
        {
            return Invoke(PostProcessSingleMethodInfo, 
                storedValue, 
                postProcessor, 
                singleType);
        }

        public static bool InvokeList(object storedValue, IPostProcessAssets postProcessor, Type elementType)
        {
            return Invoke(PostProcessListMethodInfo, 
                storedValue, 
                postProcessor, 
                elementType);
        }
        
        public static bool InvokeDictionary(object storedValue, IPostProcessAssets postProcessor, Type keyType, Type valueType)
        {
            return Invoke(PostProcessDictionaryMethodInfo,
                storedValue, 
                postProcessor, 
                keyType, 
                valueType);
        }

        private static bool Invoke(MethodInfo genericMethodInfo, 
            object storedValue, 
            IPostProcessAssets postProcessor,
            params Type[] typeArguments)
        {
            try
            {
                Get(genericMethodInfo, typeArguments)(storedValue, postProcessor);
                return true;
            }
            catch (Exception e)
            {
                LogUtils.Verbose(e);
            }

            return false;
        }

        private static Action<object, IPostProcessAssets> Get(MethodInfo genericMethodInfo, params Type[] typeArguments)
        {
            var methodInfo = genericMethodInfo.MakeGenericMethod(typeArguments);
            return (Action<object, IPostProcessAssets>)Delegate.CreateDelegate(typeof(Action<object, IPostProcessAssets>), methodInfo);
        }

        private static MethodInfo GetMethod(string methodName)
        {
            const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic;
            return typeof(PostProcessUtility).GetMethod(methodName, bindingFlags);
        }

        private static void PostProcessSingle<T>(object storedValueObject, IPostProcessAssets postProcessAssets)
        {
            var pp = (IPostProcessSingleAsset<T>)postProcessAssets;
            var v = (T)storedValueObject;
            pp.OnPostProcess(v);
        }

        private static void PostProcessList<T>(object storedValueObject, IPostProcessAssets postProcessAssets)
        {
            var pp = (IPostProcessAssetList<T>)postProcessAssets;
            var v = (List<T>)storedValueObject;
            pp.OnPostProcess(v);
        }
        
        private static void PostProcessDictionary<TKey, TValue>(object storedValueObject, IPostProcessAssets postProcessAssets)
        {
            var pp = (IPostProcessAssetDictionary<TKey, TValue>)postProcessAssets;
            var v = (Dictionary<TKey, TValue>)storedValueObject;
            pp.OnPostProcess(v);
        }
    }
}