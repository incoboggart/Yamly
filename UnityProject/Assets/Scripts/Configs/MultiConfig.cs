using Yamly;

namespace Configs
{
    [AssetList("Multi", IsSingleFile = true)]
    [AssetDictionary("MultiDic", IsSingleFile = true, KeyType = typeof(string))]
    public class MultiConfig
    {
        public string Key { get; set; }
        public int Value { get; set; }
    }
}