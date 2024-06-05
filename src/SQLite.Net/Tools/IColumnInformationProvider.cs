using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SQLite.Net2
{
	public interface IColumnInformationProvider
	{
		bool IsPK(MemberInfo m);
		string Collation(MemberInfo m);
		bool IsAutoInc(Type containedType, MemberInfo m, int tupleElementIndex);
		int? MaxStringLength(MemberInfo p);
		IEnumerable<IndexedAttribute> GetIndices(MemberInfo p);
		object GetDefaultValue(MemberInfo p);
		bool IsMarkedNotNull(MemberInfo p);
		bool IsIgnored(MemberInfo p);
		string GetColumnName(Type containedType, MemberInfo p, int tupleElementIndex);
		Type GetMemberType(MemberInfo m);
		object GetValue(MemberInfo m, object obj);
		bool TryBindParameter(ISQLiteApi isqLite3Api, IDbStatement stmt, int index, object value);
		bool TryGetSqliteColumnType(Type type, out string sqliteType);
		bool TryReadCol(ISQLiteApi isqLite3Api, IDbStatement stmt, int index, Type clrType, out object? value);

		public IOrmDeserializer? GetDeserialize(
			ISQLiteApi api,
			IDbStatement stmt,
			Type modelType,
			TableMapping.Column[] cols) => null;
	}

	public interface IOrmDeserializer
	{
		public object Deserialize();
	}
}

