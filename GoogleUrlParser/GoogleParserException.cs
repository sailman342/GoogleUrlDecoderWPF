using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleUrlParser 
{
	public class GoogleParserException : Exception
	{
		public ParsedGoogleUrl ParsedUrl { get; private set; } = null;

		public GoogleParserException (string messageArg, ParsedGoogleUrl parsedUrlArg)
			: base(messageArg)
		{
			ParsedUrl = parsedUrlArg;
		}
	}
}
