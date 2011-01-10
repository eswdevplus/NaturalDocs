﻿/* 
 * Class: GregValure.NaturalDocs.Engine.TextConverter
 * ____________________________________________________________________________
 * 
 * Functions to manage converting between plain text, HTML, and the <NDMarkup Format>.  There's significant
 * overlap between HTML and NDMarkup so it makes sense to put them all in one package.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;


namespace GregValure.NaturalDocs.Engine
	{
	static public class TextConverter
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Function: TextToHTML
		 * Converts a plain text string to HTML.  This encodes <, >, ", and & as entity characters, replaces generic quotes
		 * with left and right, and encodes double spaces with &nbsp;.
		 */
		public static string TextToHTML (string text)
			{
			string output = ConvertQuotes(text);
			output = EncodeEntityChars(output);
			output = ConvertMultipleWhitespaceChars(output);

			return output;
			}


		/* Function: EncodeEntityChars
		 * Returns the input string with <, >, ", and & replaced by their entity encodings.  If the result
		 * string will be appended to a StringBuilder, it is more efficient to use <EncodeEntityCharsAndAppend> instead 
		 * of this function.
		 */
		public static string EncodeEntityChars (string input)
			{
			if (input.IndexOfAny(EntityCharLiterals) == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder();
			EncodeEntityCharsAndAppend(input, output);
			return output.ToString();
			}
		
		
		/* Function: EncodeEntityCharsAndAppend
		 * Appends the contents of the input string to the output StringBuilder will <, >, ", and & replaced by
		 * their entity encodings.
		 */
		public static void EncodeEntityCharsAndAppend (string input, StringBuilder output)
			{
			EncodeEntityCharsAndAppend(input, output, 0, input.Length);
			}
		
		
		/* Function: EncodeEntityCharsAndAppend
		 * Appends the contents of the input string to the output StringBuilder will <, >, ", and & replaced by
		 * their entity encodings.  Offset and length represent the portion of the input string to convert.
		 */
		public static void EncodeEntityCharsAndAppend (string input, StringBuilder output, int offset, int length)
			{
			int endOfInput = offset + length;
			
			while (offset < endOfInput)
				{
				int nextEntityChar = input.IndexOfAny(EntityCharLiterals, offset, endOfInput - offset);
				
				if (nextEntityChar == -1)
					{  break;  }
				
				if (nextEntityChar != offset)
					{  output.Append(input, offset, nextEntityChar - offset);  }
					
				if (input[nextEntityChar] == '"')
					{  output.Append("&quot;");  }
				else if (input[nextEntityChar] == '&')
					{  output.Append("&amp;");  }
				else if (input[nextEntityChar] == '<')
					{  output.Append("&lt;");  }
				else if (input[nextEntityChar] == '>')
					{  output.Append("&gt;");  }
					
				offset = nextEntityChar + 1;
				}
				
			if (offset < endOfInput)
				{  output.Append(input, offset, endOfInput - offset);  }
			}
			
			
		/* Function: ConvertCopyrightAndTrademark
		 * Returns a string with all occurrances of (c), (r), and (tm) converted to their respective Unicode characters.
		 */
		public static string ConvertCopyrightAndTrademark (string input)
			{
			return copyrightAndTrademarkRegex.Replace(input, 
				delegate (Match match)
					{
					string lcMatch = match.Value.ToLower();
					
					if (lcMatch == "(c)")
						{  return "©";  }
					else if (lcMatch == "(r)")
						{  return "®";  }
					else // (lcMatch == "(tm)")
						{  return "™";  }
					}
				);
			}


		/* Function: ConvertQuotes
		 * Converts neutral quotes and apostrophes into left and right Unicode characters.
		 */
		public static string ConvertQuotes (string input)
			{
			int index = input.IndexOfAny(QuoteLiterals);

			if (index == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder(input);
			string acceptableLeftCharacters = " \t([{";

			do
				{
				if (index == 0 || acceptableLeftCharacters.IndexOf(input[index - 1]) != -1)
					{  
					if (output[index] == '"')
						{  output[index] = '“';  }
					else
						{  output[index] = '‘';  }
					}
				else
					{
					if (output[index] == '"')
						{  output[index] = '”';  }
					else
						{  output[index] = '’';  }
					}

				index = input.IndexOfAny(QuoteLiterals, index + 1);
				}
			while (index != -1);

			return output.ToString();
			}


		/* Function: ConvertMultipleWhitespaceChars
		 * Replaces instances of at least two whitespace characters in a row with &nbsp; and a space.
		 */
		public static string ConvertMultipleWhitespaceChars (string input)
			{
			return multipleWhitespaceCharsRegex.Replace(input, "&nbsp; ");
			}

		
		
		// Group: Variables
		// __________________________________________________________________________

		/* var: EntityCharLiterals
		 * An array of characters that would need to be converted to entity characters in <NDMarkup>.  Useful for String.IndexOfAny(char[]).
		 */
		static char[] EntityCharLiterals = new char[] { '"', '&', '<', '>' };

		/* var: QuoteLiterals
		 * The neutral quote and apostrophe characters suitable for String.IndexOfAny(char[]).
		 */
		static char[] QuoteLiterals = new char[] { '"', '\'' };
		
		static Regex.NDMarkup.CopyrightAndTrademark copyrightAndTrademarkRegex = new Regex.NDMarkup.CopyrightAndTrademark();
		static Regex.MultipleWhitespaceChars multipleWhitespaceCharsRegex = new Regex.MultipleWhitespaceChars();
		}
	}