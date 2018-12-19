using System;

namespace Yamly
{
    public static class TypeOf<T>
    {
        public static readonly Type Type = typeof(T);

        public static Type MakeGenericType(params Type[] typeArguments)
        {
            return Type.MakeGenericType(typeArguments);
        }
    }
}