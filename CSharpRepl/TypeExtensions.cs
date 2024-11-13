using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSDiscordService
{
    public static partial class TypeExtensions
    {
        public static string ParseGenericArgs(this Type type)
        {
            var args = type.GetGenericArguments();

            if (args.Length == 0)
            {
                return GetPrimitiveTypeName(type);
            }

            var returnTypeName = type.Name;
            var returnArgs = args.Select(a => a.ParseGenericArgs());
            return returnTypeName.Replace($"`{args.Length}", $"<{string.Join(", ", returnArgs)}>");
        }

        [GeneratedRegex(@"\[,*\]")]
        private static partial Regex ArrayRegex { get; }

        private static string GetPrimitiveTypeName(Type type)
        {
            var typeName = type.Name;
            if (type.IsArray)
            {
                typeName = ArrayRegex.Replace(typeName, "");
            }

            var returnValue = typeName switch
            {
                "Boolean" => "bool",
                "Byte" => "byte",
                "Char" => "char",
                "Decimal" => "decimal",
                "Double" => "double",
                "Int16" => "short",
                "Int32" => "int",
                "Int64" => "long",
                "SByte" => "sbyte",
                "Single" => "float",
                "String" => "string",
                "UInt16" => "ushort",
                "UInt32" => "uint",
                "UInt64" => "ulong",
                _ => typeName
            };

            if (type.IsArray)
            {
                returnValue = $"{returnValue}[{new string(',', type.GetArrayRank() - 1)}]";
            }
            return returnValue;
        }
    }
}
