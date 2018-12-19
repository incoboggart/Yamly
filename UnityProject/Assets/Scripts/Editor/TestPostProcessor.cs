using System.Collections.Generic;

using Yamly;

using Ymaly.Tests;


public class TestPostProcessor 
	: IPostProcessAssetDictionary<string, TestRoot> {
	public void OnPostProcess(Dictionary<string, TestRoot> assets)
	{
		foreach (var pair in assets)
		{
			pair.Value.Byte = 253;
		}
	}

	public string Group => Yamly.Generated.Assets.TestById.ToString();
}

