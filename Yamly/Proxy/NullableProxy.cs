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

namespace Yamly.Proxy
{
    [Serializable]
    public class NullableProxy<T> where T : struct
    {
        public bool HasValue;
        public T Value;

        public NullableProxy()
        {

        }

        public NullableProxy(T? value)
        {
            HasValue = value.HasValue;
            Value = value ?? default(T);
        }

        public NullableProxy(T value)
        {
            HasValue = true;
            Value = value;
        }

        public static implicit operator T(NullableProxy<T> proxy)
        {
            return proxy != null && proxy.HasValue 
                ? proxy.Value 
                : default(T);
        }

        public static implicit operator T? (NullableProxy<T> proxy)
        {
            return proxy != null && proxy.HasValue 
                ? new T?(proxy.Value) 
                : null;
        }

        public static implicit operator NullableProxy<T>(T value)
        {
            return new NullableProxy<T>(value);
        }

        public static implicit operator NullableProxy<T>(T? value)
        {
            return new NullableProxy<T>(value);
        }
    }
}