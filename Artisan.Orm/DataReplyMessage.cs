using System;
using System.Runtime.Serialization;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	[DataContract]
	public class DataReplyMessage
	{
		[DataMember]
		public string? Code;

		[DataMember(EmitDefaultValue = false)]
		public string? Text;

		[DataMember(EmitDefaultValue = false)]
		public Int64? Id;

		[DataMember(EmitDefaultValue = false)]
		public object? Value;
	}

	[MapperFor( typeof(DataReplyMessage), RequiredMethod.CreateObject)]
	public static class DataReplyMessageMapper 
	{
		public static DataReplyMessage CreateObject(SqlDataReader dr)
		{
			var i = 0;

			return new DataReplyMessage
			{
				Code	=	dr.GetString				(i)		,
				Text	=	dr.GetStringNullable		(++i)	,
				Id		=	dr.GetValueNullable<long>	(++i)	,
				Value	=	dr.GetValueNullable<object>	(++i)
			};
		}
	}

}

