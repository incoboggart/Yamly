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

using System.IO;

using YamlDotNet.Serialization;

namespace Yamly.UnityEditor
{
    public static class ConvertUtility
    {
        internal static Deserializer GetDeserializer(NamingConvention? namingConvention = null, 
            bool? ignoreUnmatchedProperties = null)
        {
            var settings = YamlySettings.Instance;
            return new DeserializerBuilder()
                .WithNamingConvention(namingConvention ?? settings.NamingConvention)
                .WithIgnoreUnmatchedProperties(ignoreUnmatchedProperties ?? settings.IgnoreUnmatchedProperties)
                .Build();
        }

        internal static Serializer GetSerializer(bool isJsonCompatible, 
            NamingConvention? namingConvention = null)
        {
            var settings = YamlySettings.Instance;
            return new SerializerBuilder()
                .WithNamingConvention(namingConvention ?? settings.NamingConvention)
                .WithJsonCompatible(isJsonCompatible)
                .Build();
        }
        
        public static string ToJson(string yaml, NamingConvention? targetNamingConvention, bool? ignoreUnmatchedProperties = null)
        {
            var d = GetDeserializer(null, ignoreUnmatchedProperties);
            using (var input = new StringReader(yaml))
            {
                var v = d.Deserialize(input);
                var s = GetSerializer(true, targetNamingConvention);
                return s.Serialize(v);
            }
        }

        public static string ToYaml(string json, NamingConvention? targetNamingConvention, bool? ignoreUnmatchedProperties = null)
        {
            var d = GetDeserializer(null, ignoreUnmatchedProperties);
            using (var input = new StringReader(json))
            {
                var v = d.Deserialize(input);
                var s = GetSerializer(false, targetNamingConvention);
                return s.Serialize(v);
            }
        }
    }
}