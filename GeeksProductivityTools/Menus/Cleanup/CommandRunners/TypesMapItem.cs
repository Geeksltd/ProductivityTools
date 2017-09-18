using System;
using System.Linq;
using System.CodeDom;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    internal class TypesMapItem
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string BuiltInName { get; set; }

        Type _builtInType = null;
        public Type BuiltInType
        {
            get
            {
                if (_builtInType != null) return _builtInType;
                if (string.IsNullOrEmpty(BuiltInName)) return _builtInType;

                _builtInType = Type.GetType(FullName);

                GetTypeDefaultValue();

                return _builtInType;
            }
        }

        object _defaultValue = null;
        public object DefaultValue
        {
            get
            {
                return GetTypeDefaultValue();
            }
        }

        private object GetTypeDefaultValue()
        {
            if (BuiltInType == null) return null;
            if (_defaultValue != null) return _defaultValue;

            if (BuiltInType.IsValueType)
            {
                _defaultValue = Activator.CreateInstance(BuiltInType);
            }

            return _defaultValue;
        }

        public TypeSyntax NewNode { get; set; }


        internal static TypesMapItem GetBuiltInTypes(Type type, TypeSyntax node, CSharpCodeProvider provider)
        {
            return new TypesMapItem
            {
                Name = type.Name,
                FullName = type.FullName,
                BuiltInName = provider.GetTypeOutput(new CodeTypeReference(type)),
                NewNode = node
            };
        }
        internal static TypeSyntax GetPredefineType(SyntaxKind keyword)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(keyword));
        }

        static Dictionary<string, TypesMapItem> BuiltInTypesDic;
        internal static Dictionary<string, TypesMapItem> GetBuiltInTypesDic()
        {
            if (BuiltInTypesDic != null) return BuiltInTypesDic;

            var output = new Dictionary<string, TypesMapItem>();

            using (var provider = new CSharpCodeProvider())
            {
                var typesList = new TypesMapItem[]
                {
                    GetBuiltInTypes(typeof(Boolean), GetPredefineType(SyntaxKind.BoolKeyword), provider),
                    GetBuiltInTypes(typeof(Byte),GetPredefineType(SyntaxKind.ByteKeyword), provider),
                    GetBuiltInTypes(typeof(SByte),GetPredefineType(SyntaxKind.SByteKeyword), provider),
                    GetBuiltInTypes(typeof(Char),GetPredefineType(SyntaxKind.CharKeyword), provider),
                    GetBuiltInTypes(typeof(Decimal),GetPredefineType(SyntaxKind.DecimalKeyword), provider),
                    GetBuiltInTypes(typeof(Double),GetPredefineType(SyntaxKind.DoubleKeyword), provider),
                    GetBuiltInTypes(typeof(Single),GetPredefineType(SyntaxKind.FloatKeyword), provider),
                    GetBuiltInTypes(typeof(Int32),GetPredefineType(SyntaxKind.IntKeyword), provider),
                    GetBuiltInTypes(typeof(UInt32),GetPredefineType(SyntaxKind.UIntKeyword), provider),
                    GetBuiltInTypes(typeof(Int64),GetPredefineType(SyntaxKind.LongKeyword), provider),
                    GetBuiltInTypes(typeof(UInt64),GetPredefineType(SyntaxKind.ULongKeyword), provider),
                    GetBuiltInTypes(typeof(Object),GetPredefineType(SyntaxKind.ObjectKeyword), provider),
                    GetBuiltInTypes(typeof(Int16),GetPredefineType(SyntaxKind.ShortKeyword), provider),
                    GetBuiltInTypes(typeof(UInt16),GetPredefineType(SyntaxKind.UShortKeyword), provider),
                    GetBuiltInTypes(typeof(String),GetPredefineType(SyntaxKind.StringKeyword), provider),
                };

                foreach (var item in typesList)
                {
                    output.Add(item.Name, item);
                    output.Add(item.FullName, item);
                }

                return BuiltInTypesDic = output;
            }
        }


        static Dictionary<string, TypesMapItem> _predefinedTypesDic;
        internal static Dictionary<string, TypesMapItem> GetAllPredefinedTypesDic()
        {
            if (_predefinedTypesDic != null) return _predefinedTypesDic;

            var output = GetBuiltInTypesDic();

            using (var provider = new CSharpCodeProvider())
            {
                var oldValues = output.Values.GroupBy(x => x.BuiltInName).ToList();
                foreach (var item0 in oldValues)
                {
                    var item = item0.First();

                    output.Add(item.BuiltInName, new TypesMapItem { BuiltInName = item.BuiltInName, Name = item.BuiltInName, FullName = item.FullName });
                }


                return _predefinedTypesDic = output;
            }
        }
    }

}