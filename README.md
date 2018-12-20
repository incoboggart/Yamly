# General
Yamly is development pipeline plugin for Unity3d.
It allows using yaml or json text files for editing game data through development and use that data without runtime overhead for parsing yaml/json in runtime.
Yamly parse all the text in edit time, validates syntax and converts all the data into scriptable object storages for runtime access.
To make data accessible Yamly generates code in edit time, so no reflection or il emit is performed at runtime. 

## Do i need this?
There are several main problems Yamly can help you with:
- Long and hazy VCS updates (every time i need to fix one number i have to download VCS update, open Unity, wait LOTS of time until Unity imports all changed textures, meshes, code etc, find right asset, change one number and commit it back.)
- Simultaneous work (every time i work on data with my colleague, we cant do it on the same time because of the merge conflicts - we make copies of scriptable objects and then move changes between copies manually or lose our changes and reapply them. We make errors in the process and so on...)
- Handling lots of data fast (search one exact entity among hundreds defined is not possible - default scriptable object inspector have NO SEARCH and programmer is busy with more important things. So just CLICK CLICK CLICK. Find and replace? Nope, print'em all!)

All those problems mostly can be seen in big teams working on big projects.
This does not mean, that Yamly wont help you. 
But its main goal - simplify handling lots of data by using text as data source.
And this plugin requires coding due to code first approach.

## Typycal use cases.
Single text file for code constants or quick options.
Multiple text files, defining inventory items database, one item per file.

In general - any case where you need some plain data (without direct asset referencing) will do.

## Installation
Just extract asset [package](https://github.com/incoboggart/Yamly/blob/master/out/yamly-1.0.3.unitypackage?raw=true) anywhere in your project.
Yamly is tolerant on moving its assets around, so you can change default location freely.

# Compatibility  
Yamly requires your project to use .NET 4 equivalent scripting runtime version and API compatibility version.
No reflection used in runtime so no special requirements on code stripping level or scripting backend. 

## How to get started?
To get started you need to do the following:
* Define your C# class and decorate it with Yamly asset attributes.  
Yamly is using attributes to detect types, defining data structure.
These attributes also define how data will be stored.
There are three attributes available: SingleAsset, AssetList and AssetDictionary.  
**SingleAsset** defines a group with a single asset and contains only one instance of decorated type.  
**AssetList** defines a group of multiple assets, accessed by index, and contains multiple instances of decorated type.  
**AssetDictionary** defines a group of multiple assets, accessed by key, and contains multiple instances of decorated type. 
AssetDictionary requires defining keys source.
Dictionary keys can be selected from data dy decorating data property by DictionaryKey attribute or provided via edit time static method decorated with DictionaryKeySource attribute. 
Dictionary key type is defined with key source.  
Only root type requires attribute decoration. Yamly reflects all supported properties recursively and all property types are included into data structure.
Decorating type with any attribute defines an asset group.
Every asset group needs a unique name.
Asset group name should be valid code identifier and must be unique between all groups in project.
* Create and setup appropriate data source.   
Data source is editor only asset. It map text files locations to defined asset group.
Two types of data sources available: single source and folder source.
Single source specify exact files, related to single asset groups.
Folder source specify location of multiple asset group files.
Single location/file can be mapped to several groups by placing several sources to the same location.
* Create and setup data storage.  
Data storage is runtime asset, storing converted data and used to access it.
By default any data storage contains all asset groups. This behaviour can be ajusted through storage inspector.
Storages provide methods to access stored data by group name.

# Important limitations and requirements.
* Code first  
Yamly is using a code first approach. This means data structure is defined via C# code.
To make Yamly process some data type it have to be decorated with Yamly asset attribute.
* Assemblies  
To use Yamly code generation feature, all decorated types have to be placed in assemblies (not Assembly-CSharp or Assembly-CSharp-Editor etc).
This is required to remove dll cyclic dependency and remove utility source code from user space so you dont have to maintain it manually.
Both manually build dll and asmdef are supported.
* Code standard  
Yamly is processing only properties that have getters and setters. Fields and readonly/writeonly properties are ignored.  
This limitation is coming  from YamlDotNet, that is used for parsing text data.
* No polymorphic inheritance support
Typical yamly container class is plain property DTO. Every single property of data, that can be written - must be declared.
No support for serializing interfaces and abstract base classes - such properties will be ignored.
* No reference counting  
Yamly does not provide reference counting mechanics. This means same data in different groups is copied and reference equality wont work.
* Asset group names  
Asset group names must be unique between all groups.
Asset group names must be valid code identifiers, though the can contain whitespaces and - for convinience (the replaced to _ in code). 

## Sample code:
Declare type and decorate with asset attributes:
```
[SingleAsset("MyData")] // Will appear only in single source
[SingleAsset("MyOtherData")] // Will appear only in single source
[AssetList("MyDatas")] // Will appear only folder source
[AssetDictionary("DataById")] // Will appear only folder source
[AssetDictionary("DataByLowerId")] // Will appear only folder source
[AssetDictionary("DataByOtherValue")] // Will appear only folder source
public sealed class DataClass
{
	[DictionaryKey] // This key source is used by default.
	public string Id { get; set; }
	public OtherDataClass Val { get; set; }

	[DictionaryKey("DataByLowerId")] // Though LowerId is non serilized property, it can be used as dictionary key source
	public string LowerId { get {return Id.ToLower();} }

	[DictionaryKeySource("DataByOtherValue")] // This key source is used for DataByOtherValue group keys. 
	public static int GetDataByOtherValueKey(DataClass data) {
		return data.Val.Value;
	}
}

public sealed class OtherDataClass
{
	// This property will be serialized
	public int Value { get; set; }

	// This property will be ignored due to code style - it has no setter
	public bool IsNotZero { get { return Value != 0; } }

	[Ignore] // This property will be ignored due to Ignore attribute
	public int Square { get; set; }
}

// To enable polymorphyc behaviour in data declare every possible field in single class and check provided values at runtime.
// Or make PolymorphycData act as factory, basing on its data.
public sealed class PolymorphycData {
	public int? IntValue;
	public float? FloatValue;
	public string StringValue;
}
```

Load single asset.
```
var data = Resources.Load<Storage>(path).Get<DataClass>("MyData");
```

Load list of assets.
```
var data = Resources.Load<Storage>(path).Get<List<DataClass>>("MyDatas");
```

Load assets dictionary.
```
var data = Resources.Load<Storage>(path).Get<Dictionary<string, DataClass>>("DataByLowerId");
```

## Advanced.
Yamly have some advanced capabilities for automating data processing:
* Plugin settings.
Yamly is maintaining YamlySettings asset, that contains some project space settings to edit. This asset is used strictly in edit time.
* Utility code
Along with usefull code, yamly is generating loads of utility code to save time. All those code can be found in Yamly.Generated namespace. It includes asset groups enumeration and JSON convert utility.
* Build preprocessor.
Yamly shipped with integrated build preprocessor so you dont have to upgrade your data manually on builds.
* Data postprocessing.
Sometimes its required to postprocess raw data for it to be fully compatible. For this purpose Yamly implements post processing pipeline. To use it one have to define editor space type that inherits one of IPostProcessAssets interfaces.
```
public class MyDefaultPostProcessor 
    : IPostProcessAssetDictionary<string, MyData> 
{
    public void OnPostProcess(Dictionary<string, MyData> assets)
    {
        foreach (var pair in assets)
        {
            pair.Value.Byte = 253;
        }
    }

    public string Group => Yamly.Generated.Assets.MyDataById.ToString();
}
```
Postprocessors call order can be controlled with PostProcessOrderAttribute.
```
[PostProcessOrder(Group = 1, Order = 1)]
public class MyOrderedPostProcessor 
    : IPostProcessAssetDictionary<string, MyData> {
    public void OnPostProcess(Dictionary<string, MyData> assets)
    {
        foreach (var pair in assets)
            {
                pair.Value.Byte = 255 - pair.Value.Byte; // 2
            }
        }

        public string Group => Yamly.Generated.Assets.MyDataById.ToString();
    }
```
