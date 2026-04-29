using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.GrandRecords.Models;

public class RecordType
{
	public byte Id { get; set; }

	public string Code { get; set; }

	public string Name { get; set; }
}


[MapperFor(typeof(RecordType), RequiredMethod.CreateObject)]
public static class RecordTypeMapper 
{
	// Standalone entry: reads three columns starting at position 0 of the row.
	// The (ref int) overload below uses ++index because it is meant to be called
	// inline by a parent mapper that passes "last consumed column" — so we start
	// at -1 here to compensate.
	public static RecordType CreateObject(SqlDataReader dr)
	{
		var index = -1;
		return CreateObject(dr, ref index);
	}

	public static RecordType CreateObject(SqlDataReader dr, ref int index)
	{
		if (dr.IsDBNull(++index))
		{
			index += 2;
			return null;
		}

		return new RecordType
		{
			Id		=	dr.GetByte(index)		,
			Code	=	dr.GetString(++index)	,
			Name	=	dr.GetString(++index)
		};
	}

}