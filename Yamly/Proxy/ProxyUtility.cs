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

using System;
using System.Collections.Generic;

namespace Yamly.Proxy
{
    public static class ProxyUtility
    {
        public static TOut[] Convert<TIn, TOut>(this TIn[] input, Func<TIn, TOut> convertElementFunc)
        {
            if (input == null)
            {
                return null;
            }

            var output = new TOut[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = convertElementFunc(input[i]);
            }
            return output;
        }

        public static List<TOut> Convert<TIn, TOut>(this IList<TIn> input, Func<TIn, TOut> convertElementFunc)
        {
            if (input == null)
            {
                return null;
            }

            var output = new List<TOut>(input.Count);
            for (int i = 0; i < input.Count; i++)
            {
                output.Add(convertElementFunc(input[i]));
            }
            return output;
        }

        public static TOut[] ConvertArray<TIn, TOut>(this IList<TIn> input, Func<TIn, TOut> convertElementFunc)
        {
            if (input == null)
            {
                return null;
            }

            return Convert(input, convertElementFunc).ToArray();
        }

        public static Dictionary<TKeyOut, TValueOut> Convert<TKeyIn, TValueIn, TKeyOut, TValueOut>(IList<TKeyIn> keys,
            IList<TValueIn> values,
            Func<TKeyIn, TKeyOut> convertKey,
            Func<TValueIn, TValueOut> convertValue)
        {
            if (keys == null)
            {
                return null;
            }

            if (values == null)
            {
                return null;
            }

            var dictionary = new Dictionary<TKeyOut, TValueOut>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                dictionary.Add(convertKey(keys[i]), convertValue(values[i]));
            }

            return dictionary;
        }

        public static Dictionary<TKeyOut, TValueOut> Convert<TKeyIn, TValueIn, TKeyOut, TValueOut>(this Dictionary<TKeyIn, TValueIn> dictionary,
            Func<TKeyIn, TKeyOut> convertKey,
            Func<TValueIn, TValueOut> convertValue)
        {
            if (dictionary == null)
            {
                return null;
            }

            var result = new Dictionary<TKeyOut, TValueOut>();
            foreach (var pair in dictionary)
            {
                result[convertKey(pair.Key)] = convertValue(pair.Value);
            }

            return result;
        }

        public static Dictionary<TKeyOut, TValueOut> Convert<TKeyIn, TValueIn, TKeyOut, TValueOut>(this DictionaryProxy<TKeyIn, TValueIn> dictionary,
            Func<TKeyIn, TKeyOut> convertKey,
            Func<TValueIn, TValueOut> convertValue)
        {
            if (dictionary == null)
            {
                return null;
            }

            var convert = new Dictionary<TKeyOut, TValueOut>();
            foreach (var pair in dictionary)
            {
                convert[convertKey(pair.Key)] = convertValue(pair.Value);
            }

            return new DictionaryProxy<TKeyOut, TValueOut>(convert);
        }

        public static void Convert<TKeyIn, TValueIn, TKeyOut, TValueOut>(Dictionary<TKeyIn, TValueIn> dictionary,
            Func<TKeyIn, TKeyOut> convertKey,
            Func<TValueIn, TValueOut> convertValue,
            out TKeyOut[] keys,
            out TValueOut[] values)
        {
            if (dictionary == null)
            {
                keys = null;
                values = null;
                return;
            }

            var k = new TKeyOut[dictionary.Count];
            var v = new TValueOut[dictionary.Count];
            var index = 0;

            foreach (var pair in dictionary)
            {
                k[index] = convertKey(pair.Key);
                v[index] = convertValue(pair.Value);
                index++;
            }

            keys = k;
            values = v;
        }

        public static void Convert<TKeyIn, TValueIn, TKeyOut, TValueOut>(Dictionary<TKeyIn, TValueIn> dictionary,
            Func<TKeyIn, TKeyOut> convertKey,
            Func<TValueIn, TValueOut> convertValue,
            out ListProxy<TKeyOut> keys,
            out ListProxy<TValueOut> values)
        {

            TKeyOut[] keysArray;
            TValueOut[] valuesArray;
            Convert(dictionary, convertKey, convertValue, out keysArray, out valuesArray);
            keys = keysArray;
            values = valuesArray;
        }

        public static void Convert<TKeyIn, TValueIn, TKeyOut, TValueOut, TKeysList, TValuesList>(Dictionary<TKeyIn, TValueIn> dictionary,
            Func<TKeyIn, TKeyOut> convertKey,
            Func<TValueIn, TValueOut> convertValue,
            out TKeysList keys,
            out TValuesList values)
                where TKeysList : ListProxy<TKeyOut>, new()
                where TValuesList : ListProxy<TValueOut>, new()
        {
            if (dictionary == null)
            {
                keys = null;
                values = null;
                return;
            }

            TKeyOut[] keysArray;
            TValueOut[] valuesArray;
            Convert(dictionary, convertKey, convertValue, out keysArray, out valuesArray);
            keys = new TKeysList { List = new List<TKeyOut>(keysArray) };
            values = new TValuesList { List = new List<TValueOut>(valuesArray) };
        }
    }
}
