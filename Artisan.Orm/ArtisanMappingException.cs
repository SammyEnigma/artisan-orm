using System;

namespace Artisan.Orm
{

	/// <summary>
	/// Thrown when Artisan.ORM cannot perform object-relational mapping:
	/// a required mapper method is missing, a mapper is not registered for
	/// a given type, or an auto-generated mapping function cannot be built
	/// because the reader fields do not match any public properties of the
	/// target type.
	///
	/// Inherits from <see cref="InvalidOperationException"/>, so existing
	/// <c>catch (InvalidOperationException)</c> handlers keep working.
	/// </summary>
	public class ArtisanMappingException : InvalidOperationException
	{
		public ArtisanMappingException() { }

		public ArtisanMappingException(string message) : base(message) { }

		public ArtisanMappingException(string message, Exception innerException)
			: base(message, innerException) { }
	}

}
