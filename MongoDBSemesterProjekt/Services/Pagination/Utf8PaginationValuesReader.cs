using NSwag;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;

namespace MongoDBSemesterProjekt.Services.Pagination
{
	public ref struct Utf8PaginationValuesReader
	{
		private ReadOnlySpan<byte> _values;
		private int _index;
		public PaginationValueToken CurrentToken { get; private set; }
		public ReadOnlySpan<byte> CurrentValue { get; private set; }

		public Utf8PaginationValuesReader(ReadOnlySpan<byte> values)
		{
			_values = values;
			_index = 0;
			CurrentToken = PaginationValueToken.Start;
		}
		
		
		public bool ReadAndCheck(PaginationValueToken token)
		{
			if (Read())
			{
				if(CurrentToken != token)
					throw new InvalidOperationException($"Expected {token} but got {CurrentToken}");

				return true;
			}

			return false;
		}

		public bool ReadNextPropertyName([NotNullWhen(true)] out string? propertyName)
		{
			var read = ReadAndCheck(PaginationValueToken.PropertyName);
			if (read)
			{
				propertyName = HttpUtility.UrlDecode(Encoding.UTF8.GetString(CurrentValue));
			}
			else
			{
				propertyName = null;
			}

			return read;
		}

		public bool ReadNextPropertyValue([NotNullWhen(true)] out string? propertyValue)
		{
			var read = ReadAndCheck(PaginationValueToken.PropertyValue);
			if (read)
			{
				propertyValue = HttpUtility.UrlDecode(Encoding.UTF8.GetString(CurrentValue));
			}
			else
			{
				propertyValue = null;
			}
			return read;
		}

		public bool ReadNextProperty([NotNullWhen(true)]out string? propertyName, [NotNullWhen(true)] out string? propertyValue)
		{
			if (ReadNextPropertyName(out propertyName) && ReadNextPropertyValue(out propertyValue))
				return true;

			propertyName = null;
			propertyValue = null;
			return false;
		}

		private void ReadUntil(byte until)
		{
			int started = _index;
			while (_index < _values.Length && _values[_index] != until)
				_index++;

			CurrentValue = _values.Slice(started, _index - started);
		}

		public bool Read() 
		{
			CurrentValue = default;
			if (_index == _values.Length)
			{
				CurrentToken = PaginationValueToken.End;
				return false;
			}

			switch (CurrentToken)
			{
				case PaginationValueToken.Start:
				case PaginationValueToken.PropertySeparator:
					ReadUntil((byte)'=');
					CurrentToken = PaginationValueToken.PropertyName;
					return true;

				case PaginationValueToken.PropertyName:
					_index++;
					CurrentToken = PaginationValueToken.ValueSeparator;
					return true;

				case PaginationValueToken.ValueSeparator:
					ReadUntil((byte)'&');
					CurrentToken = PaginationValueToken.PropertyValue;
					return true;

				case PaginationValueToken.PropertyValue:
					_index++;
					CurrentToken = PaginationValueToken.PropertySeparator;
					return true;

				default:
				case PaginationValueToken.End:
					return false;
			}
		}
	}
}
