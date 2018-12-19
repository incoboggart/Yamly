namespace Yamly.UnityEditor
{
    public static class YamlyBuildPipeline
    {
        public static void RebuildAllData()
        {
            YamlyAssetPostprocessor.RebuildAll();
        }
    }
}