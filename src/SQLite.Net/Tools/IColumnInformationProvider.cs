using System;
using System.Reflection;
using System.Collections.Generic;

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

		/// <summary>
		/// Attempts to read an object from <see cref="stmt"/>. Returns non-null if the object is supported.
		/// </summary>
		/// <param name="mapping">Table mapping for the type to return</param>
		/// <param name="sqLiteApi">SQLite API</param>
		/// <param name="stmt">Statement row to read from.</param>
		/// <returns>An object or null if the table/type is not supported.</returns>
		object? TryReadObject(TableMapping mapping, ISQLiteApi sqLiteApi, IDbStatement stmt) => null;
	}
}

