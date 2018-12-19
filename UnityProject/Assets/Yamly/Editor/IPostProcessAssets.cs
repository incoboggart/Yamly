using System;
using System.Collections.Generic;

namespace Yamly
{
    public interface IPostProcessAssets
    {
        string Group { get; }
    }

    public interface IPostProcessSingleAsset<T>
        : IPostProcessAssets
    {
        void OnPostProcess(T asset);
    }

    public interface IPostProcessAssetList<T>
        : IPostProcessAssets
    {
        void OnPostProcess(List<T> assets);
    }

    public interface IPostProcessAssetDictionary<TKey, TValue>
        : IPostProcessAssets
    {
        void OnPostProcess(Dictionary<TKey, TValue> assets);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PostProcessOrderAttribute
        : Attribute
    {
        /// <summary>
        /// Call group. Use this to arrange dependencies.
        /// </summary>
        public int Group { get; set; }
        
        /// <summary>
        /// Order in group. Use this to arrange calls in same group.
        /// </summary>
        public int Order { get; set; }
    }
}