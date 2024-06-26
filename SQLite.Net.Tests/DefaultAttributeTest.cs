﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SQLite.Net2.Tests
{
    [TestFixture]
    public class DefaultAttributeTest : BaseTest
    {
        private class WithDefaultValue
        {
			public const string CustomAttributeDefaultValue = "12345";
            public const int IntVal = 666;
            public static decimal DecimalVal = 666.666m;
            public static string StringVal = "Working String";
            public static DateTime DateTimegVal = new DateTime(2014, 2, 13);

            public WithDefaultValue()
            {
                TestInt = IntVal;
                TestDateTime = DateTimegVal;
                TestDecimal = DecimalVal;
                TestString = StringVal;
            }
            
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }


            [Default]
            public int TestInt { get; set; }

            [Default]
            public decimal TestDecimal { get; set; }

            [Default]
            public DateTime TestDateTime { get; set; }

            [Default]
            public string TestString { get; set; }


            [Default(value: IntVal, usePropertyValue: false)]
            public int DefaultValueInAttributeTestInt { get; set; }

            public class Default666Attribute : DefaultAttribute
            {
                public Default666Attribute() :base(usePropertyValue:false, value:IntVal)
                {
                    
                }
            }

            [Default666]
            public int TestIntWithSubtypeOfDefault { get; set; }

        }

		private class TestDefaultValueAttribute : Attribute
		{
			public string DefaultValue { get; private set; }

			public TestDefaultValueAttribute(string defaultValue)
			{
				DefaultValue = defaultValue;
			}
		}

		public class TestColumnInformationProvider : IColumnInformationProvider
		{
			public string GetColumnName(Type containedType, MemberInfo p, int tupleElementIndex)
			{
				var colAttr = p.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault();
				return colAttr == null ? p.Name : colAttr.Name;
			}

			public Type GetMemberType(MemberInfo m)
			{
				return m switch
				{
					PropertyInfo p => p.PropertyType,
					FieldInfo f => f.FieldType,
					_ => throw new NotSupportedException($"{m.GetType()} is not supported.")
				};
			}

			public object GetValue(MemberInfo m, object obj)
			{
				return m switch
				{
					PropertyInfo p => p.GetValue(obj),
					FieldInfo f => f.GetValue(obj),
					_ => throw new NotSupportedException($"{m.GetType()} is not supported.")
				};
			}

			public bool TryBindParameter(ISQLiteApi isqLite3Api, IDbStatement stmt, int index, object value)
			{
				return false;
			}

			public bool TryGetSqliteColumnType(Type type, out string sqliteType)
			{
				sqliteType = string.Empty;
				return false;
			}

			public bool TryReadCol(ISQLiteApi isqLite3Api, IDbStatement stmt, int index, Type clrType, out object? value)
			{
				value = null;
				return false;
			}

			public bool IsIgnored(MemberInfo p)
			{
				return false;
			}

			public IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
			{
				return p.GetCustomAttributes<IndexedAttribute>();
			}

			public bool IsPK(MemberInfo m)
			{
				return m.GetCustomAttributes<PrimaryKeyAttribute>().Any();
			}
			public string Collation(MemberInfo m)
			{
				return string.Empty;
			}
			public bool IsAutoInc(Type containedType, MemberInfo m, int tupleElementIndex)
			{
				return false;
			}
			public int? MaxStringLength(MemberInfo p)
			{
				return null;
			}
			public object GetDefaultValue(MemberInfo p)
			{
				var defaultValueAttributes = p.GetCustomAttributes<TestDefaultValueAttribute> ();
				if (!defaultValueAttributes.Any())
				{
					return null;
				}

				return defaultValueAttributes.First().DefaultValue;
			}
			public bool IsMarkedNotNull(MemberInfo p)
			{
				return false;
			}
		}

		public abstract class TestObjBase<T>
		{
			[AutoIncrement, PrimaryKey]
			public int Id { get; set; }

			public T Data { get; set; }

		}

		public class TestObjIntWithDefaultValue : TestObjBase<int>
		{
			[TestDefaultValue("12345")]
			public string SomeValue { get; set; }
		}

		public class TestDbWithCustomAttributes : SQLiteConnection
		{
			public TestDbWithCustomAttributes(String path)
				: base(path)
			{
				ColumnInformationProvider = new TestColumnInformationProvider();
				CreateTable<TestObjIntWithDefaultValue>();
			}
		}

        [Test]
        public void TestColumnValues()
        {
            using (TestDb db = new TestDb())
            {
                db.CreateTable<WithDefaultValue>();
               

                string failed = string.Empty;
                foreach (var col in db.GetMapping<WithDefaultValue>().Columns)
                {
                    if (col.PropertyName == "TestInt" && !col.DefaultValue.Equals(WithDefaultValue.IntVal))
                        failed += " , TestInt does not equal " + WithDefaultValue.IntVal;


                    if (col.PropertyName == "TestDecimal" && !col.DefaultValue.Equals(WithDefaultValue.DecimalVal))
                        failed += "TestDecimal does not equal " + WithDefaultValue.DecimalVal;

                    if (col.PropertyName == "TestDateTime" && !col.DefaultValue.Equals(WithDefaultValue.DateTimegVal))
                        failed += "TestDateTime does not equal " + WithDefaultValue.DateTimegVal;

                    if (col.PropertyName == "TestString" && !col.DefaultValue.Equals(WithDefaultValue.StringVal))
                        failed += "TestString does not equal " + WithDefaultValue.StringVal;

                    if (col.PropertyName == "DefaultValueInAttributeTestInt" && !col.DefaultValue.Equals(WithDefaultValue.IntVal))
                        failed += " , DefaultValueInAttributeTestInt does not equal " + WithDefaultValue.IntVal;

                    if (col.PropertyName == "TestIntWithSubtypeOfDefault" && !col.DefaultValue.Equals(WithDefaultValue.IntVal))
                        failed += " , TestIntWithSubtypeOfDefault does not equal " + WithDefaultValue.IntVal;

                }

                Assert.True(string.IsNullOrWhiteSpace(failed), failed);

            }
        }
    }
}
