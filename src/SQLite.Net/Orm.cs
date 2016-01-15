//
// Copyright (c) 2012 Krueger Systems, Inc.
// Copyright (c) 2013 Øystein Krog (oystein.krog@gmail.com)
// Copyright (c) 2014 Benjamin Mayrargue (softlion@softlion.com)
//   Fix for support of multiple primary keys
//   Support new types: TimeSpan, DateTimeOffset, XElement
//   (breaking change) Fix i18n string support: stored as i18n string (nvarchar) instead of language dependant string (varchar)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using JetBrains.Annotations;
using SQLite.Net.Attributes;
using NotNullAttribute = SQLite.Net.Attributes.NotNullAttribute;

namespace SQLite.Net
{
    internal static class Orm
    {
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        internal static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks, IBlobSerializer serializer,
            IDictionary<Type, string> extraTypeMappings)
        {
            //http://www.sqlite.org/lang_createtable.html
            return String.Format("\"{0}\" {1} {2} {3} {4} {5} ",
                p.Name,
                SqlType(p, storeDateTimeAsTicks, serializer, extraTypeMappings),
                p.IsAutoInc ? "primary key autoincrement" : null, //autoincrement can not be set with a multiple primary key
                !p.IsNullable ? "not null" : null,
                !string.IsNullOrEmpty(p.Collation) ? "collate " + p.Collation : null,
                p.DefaultValue != null ? "default('" + p.DefaultValue + "') " : null
                );
        }

        private static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks,
            IBlobSerializer serializer,
            IDictionary<Type, string> extraTypeMappings)
        {
            var clrType = p.ColumnType;
            var interfaces = clrType.GetTypeInfo().ImplementedInterfaces.ToList();

            string extraMapping;
            if (extraTypeMappings.TryGetValue(clrType, out extraMapping))
            {
                return extraMapping;
            }

            if (clrType == typeof (bool) || clrType == typeof (byte) || clrType == typeof (ushort) ||
                clrType == typeof (sbyte) || clrType == typeof (short) || clrType == typeof (int) ||
                clrType == typeof (uint) || clrType == typeof (long) ||
                interfaces.Contains(typeof (ISerializable<bool>)) ||
                interfaces.Contains(typeof (ISerializable<byte>)) ||
                interfaces.Contains(typeof (ISerializable<ushort>)) ||
                interfaces.Contains(typeof (ISerializable<sbyte>)) ||
                interfaces.Contains(typeof (ISerializable<short>)) ||
                interfaces.Contains(typeof (ISerializable<int>)) ||
                interfaces.Contains(typeof (ISerializable<uint>)) ||
                interfaces.Contains(typeof (ISerializable<long>)) ||
                interfaces.Contains(typeof (ISerializable<ulong>)))
            {
                return "integer";
            }
            if (clrType == typeof (float) || clrType == typeof (double) || 
                interfaces.Contains(typeof (ISerializable<float>)) ||
                interfaces.Contains(typeof (ISerializable<double>)))
            {
                return "real";
            }
            if (clrType == typeof (decimal) ||
                interfaces.Contains(typeof (ISerializable<decimal>)))
            {
                return "numeric";
            }
            if (clrType == typeof (string) || clrType == typeof(XElement)
            || interfaces.Contains(typeof (ISerializable<string>))
            || interfaces.Contains(typeof (ISerializable<XElement>))
            )
            {
             //SQLite ignores the length //See http://www.sqlite.org/datatype3.html
                return "text";
            }
            if (clrType == typeof (TimeSpan) || interfaces.Contains(typeof (ISerializable<TimeSpan>)))
            {
                return "integer";
            }
            if (clrType == typeof (DateTime) || interfaces.Contains(typeof (ISerializable<DateTime>)))
            {
                return storeDateTimeAsTicks ? "integer" : "numeric";
            }
            if (clrType == typeof (DateTimeOffset))
            {
                return "integer";
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof (byte[]) || interfaces.Contains(typeof (ISerializable<byte[]>)))
            {
                return "blob";
            }
            if (clrType == typeof (Guid) || interfaces.Contains(typeof (ISerializable<Guid>)))
            {
                return "text";
            }
            if (serializer != null && serializer.CanDeserialize(clrType))
            {
                return "blob";
            }
            throw new NotSupportedException("Don't know about " + clrType);
        }

        internal static bool IsPK(MemberInfo p)
        {
            return p.GetCustomAttributes<PrimaryKeyAttribute>().Any();
        }

        internal static string Collation(MemberInfo p)
        {
            foreach (var attribute in p.GetCustomAttributes<CollationAttribute>())
            {
                return attribute.Value;
            }
            return string.Empty;
        }

        internal static bool IsAutoInc(MemberInfo p)
        {
            return p.GetCustomAttributes<AutoIncrementAttribute>().Any();
        }

        internal static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            return p.GetCustomAttributes<IndexedAttribute>();
        }

        [CanBeNull]
        internal static int? MaxStringLength(PropertyInfo p)
        {
            foreach (var attribute in p.GetCustomAttributes<MaxLengthAttribute>())
            {
                return attribute.Value;
            }
            return null;
        }

        [CanBeNull]
        internal static object GetDefaultValue(PropertyInfo p)
        {
            foreach (var attribute in p.GetCustomAttributes<DefaultAttribute>())
            {
                try
                {
                    if (!attribute.UseProperty)
                    {
                        return Convert.ChangeType(attribute.Value, p.PropertyType);
                    }

                    var obj = Activator.CreateInstance(p.DeclaringType);
                    return p.GetValue(obj);
                }
                catch (Exception exception)
                {
                    throw new Exception("Unable to convert " + attribute.Value + " to type " + p.PropertyType, exception);
                }
            }
            return null;
        }

        internal static bool IsMarkedNotNull(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes<NotNullAttribute>(true);
            return attrs.Any();
        }
    }
}