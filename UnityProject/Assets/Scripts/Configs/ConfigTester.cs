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

using System.Collections.Generic;

using Yamly;

namespace Ymaly.Tests
{
    [AssetDictionary("TestById")]
    [AssetDictionary("TestByFileName", true)]
    [AssetDictionary("TestByIdCustom1")]
    [AssetDictionary("TestByIdCustom2")]
    [AssetDictionary("TestByAsset //NameCustom2")]
    [AssetList("Tests")]
    [SingleAsset("Test")]
    public sealed class TestRoot
    {
        [DictionaryKeySource("TestByIdCustom1")]
        public static string CustomKeySelector_Id(TestRoot v)
        {
            return v.Id;
        }

        [DictionaryKeySource("TestByIdCustom2")]
        public static string CustomKeySelector_Id(TestRoot v, UnityEngine.TextAsset textAsset)
        {
            return v.Id;
        }

        [DictionaryKeySource("TestByAssetNameCustom2")]
        public static string CustomKeySelector_AssetName(UnityEngine.TextAsset textAsset, TestRoot v)
        {
            return textAsset.name;
        }

        [DictionaryKey]
        public string Id { get; set; }

        public byte Byte { get; set; }
        public byte? NByte { get; set; }
        public byte[] ByteArray { get; set; }
        public List<byte> ByteList { get; set; }
        public Dictionary<string, byte> ByteDictionary { get; set; }
        
        public sbyte SByte { get; set; }
        public sbyte? NSByte { get; set; }
        public sbyte[] SByteArray { get; set; }
        public List<sbyte> SByteList { get; set; }
        public Dictionary<string, sbyte> SByteDictionary { get; set; }
        
        public short Short { get; set; }
        public short? NShort { get; set; }
        public short[] ShortArray { get; set; }
        public List<short> ShortList { get; set; }
        public Dictionary<string, short> ShortDictionary { get; set; }

        public ushort UShort { get; set; }
        public ushort? NUShort { get; set; }
        public ushort[] UShortArray { get; set; }
        public List<ushort> UShortList { get; set; }
        public Dictionary<string, ushort> UShortDictionary { get; set; }
        
        public int Int { get; set; }
        public int? NInt { get; set; }
        public int[] IntArray { get; set; }
        public List<int> IntList{ get; set; }
        public Dictionary<string, int> IntDictionary { get; set; }
        
        public uint UInt { get; set; }
        public uint? NUInt { get; set; }
        public uint[] UIntArray { get; set; }
        public List<uint> UIntList{ get; set; }
        public Dictionary<string, uint> UIntDictionary { get; set; }


        public long Long { get; set; }
        public long? NLong { get; set; }
        public long[] LongArray { get; set; }
        public List<long> LongList { get; set; }
        public Dictionary<string, long> LongDictionary { get; set; }

        public ulong ULong { get; set; }
        public ulong? NULong { get; set; }
        public ulong[] ULongArray { get; set; }
        public List<ulong> ULongList { get; set; }
        public Dictionary<string, ulong> ULongDictionary { get; set; }

        public float Float { get; set; }
        public float? NFloat { get; set; }
        public float[] FloatArray { get; set; }
        public List<float> FloatList { get; set; }
        public Dictionary<string, float> FloatDictionary { get; set; }

        public double Double { get; set; }
        public double? NDouble { get; set; }
        public double[] DoubleArray { get; set; }
        public List<double> DoubleList{ get; set; }
        public Dictionary<string, double> DoubleDictionary { get; set; }

        public string String { get; set; }
        public string[] StringsArray { get; set; }
        public List<string> StringsList { get; set; }
        public Dictionary<string, string> StringDictionary { get; set; }

        public TestClass Class { get; set; }
        public TestClass[] ClassArray { get; set; }
        public List<TestClass> ClassList { get; set; }
        public Dictionary<string, TestClass> ClassDictionary { get; set; }
        
        public TestStruct Struct { get; set; }
        public TestStruct? NStruct { get; set; }
        public TestStruct[] StructArray { get; set; }
        public List<TestStruct> StructList { get; set; }
        public Dictionary<string, TestStruct> StructDictionary { get; set; }

        public TestEnum Enum { get; set; }
        public TestEnum? NEnum { get; set; }
        public TestEnum[] EnumArray { get; set; }
        public List<TestEnum> EnumList { get; set; }
        public Dictionary<string, TestEnum> EnumDictionary { get; set; }
    }

    public sealed class TestClass
    {
        public string Id { get; set; }
    }

    public enum TestEnum
    {
        Min,
        Max
    }
}
