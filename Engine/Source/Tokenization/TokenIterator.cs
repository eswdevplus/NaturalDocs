﻿/* 
 * Struct: GregValure.NaturalDocs.Engine.Tokenization.TokenIterator
 * ____________________________________________________________________________
 * 
 * An iterator for effeciently walking through the tokens in <Tokenizer> while keeping track of the line number
 * and offset into the raw text.
 * 
 * It is designed to be tolerant to allow for easier parsing.  You can go past the bounds of the data without 
 * exceptions being thrown.
 * 
 * It is a struct rather than a class because it is expected many of them are going to be created, copied, passed
 * around, and then disappear just as quickly.  It's not worth the memory churn to be a reference type, and having
 * them behave as a value type is more intuitive.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace GregValure.NaturalDocs.Engine.Tokenization
	{
	public struct TokenIterator
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Next (count)
		 * Moves forward the passed number of tokens, returning false if we're past the last token.
		 */
		public bool Next (int count = 1)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }
				
			if (tokenIndex < 0)
				{
				if (tokenIndex + count <= 0)
					{  
					tokenIndex += count;
					return false;
					}
				else
					{
					count += tokenIndex;
					tokenIndex = 0;
					}
				}

			while (count > 0 && tokenIndex < tokenizer.Tokens.Count)
				{
				rawTextIndex += tokenizer.Tokens[tokenIndex].Length;

				if (tokenizer.Tokens[tokenIndex].Type == TokenType.LineBreak)
					{  lineNumber++;  }
					
				tokenIndex++;
				count--;
				}
				
			if (count > 0)
				{
				// Finish advancing regardless of whether we're past the end or not, so if we go past six we must go back six
				// to get into valid territory.
				tokenIndex += count;
				}
			
			return (tokenIndex < tokenizer.Tokens.Count);
			}
			
			
		/* Function: NextByCharacters
		 * 
		 * Moves forward by the passed number of characters, returning false if we're past the last token.
		 * 
		 * This throws an exception if advancing by the passed number of characters would cause the iterator to not fall
		 * evenly on a token boundary.  It is assumed that this function will primarily be used after a positive result from 
		 * <MatchesAcrossTokens()> or <TokensInCharacters()> which would cause this to not be an issue.
		 */
		public bool NextByCharacters (int characters)
			{
			int tokensInCharacters = TokensInCharacters(characters);
			
			if (tokensInCharacters == -1)
				{  throw new InvalidOperationException();  }
				
			return Next(tokensInCharacters);
			}
			
			
		/* Function: Previous (count)
		 * Moves backwards the passed number of tokens, returning false if we're past the first token.
		 */
		public bool Previous (int count = 1)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }
				
			if (tokenIndex > tokenizer.Tokens.Count)
				{
				if (tokenIndex - count >= tokenizer.Tokens.Count)
					{
					tokenIndex -= count;
					return false;
					}
				else
					{
					count -= tokenIndex - tokenizer.Tokens.Count;
					tokenIndex = tokenizer.Tokens.Count;
					}
				}

			while (count > 0 && tokenIndex > 0)
				{			
				tokenIndex--;

				rawTextIndex -= tokenizer.Tokens[tokenIndex].Length;
			
				if (tokenizer.Tokens[tokenIndex].Type == TokenType.LineBreak)
					{  lineNumber--;  }
					
				count--;
				}

			if (count > 0)
				{
				// Finish advancing regardless of whether we're past the beginning or not, so if we go past six we must go 
				// forward six to get into valid territory.
				tokenIndex -= count;
				}
				
			return (tokenIndex >= 0);			
			}
			
			
		/* Function: TokensInCharacters
		 * Returns the number of tokens between the current position and the passed number of characters.  If advancing
		 * by the character count would not land on a token boundary this returns -1.
		 */
		public int TokensInCharacters (int characterCount)
			{
			if (!IsInBounds)
				{  return -1;  }
				
			int i = tokenIndex;
			int tokenCount = 0;
			
			while (characterCount > 0 && i < tokenizer.Tokens.Count)
				{
				characterCount -= tokenizer.Tokens[i].Length;
				i++;
				tokenCount++;
				}
				
			if (characterCount == 0)  // i landing one past the last token is okay
				{  return tokenCount;  }
			else
				{  return -1;  }
			}
			
			
		/* Function: MatchesToken
		 * Returns whether the passed string matches the current token.  The string must match the entire
		 * token, so "some" won't match "something".  Returns false if the iterator is out of bounds.
		 */
		public bool MatchesToken (string text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return false;  }
				
			return ( text.Length == tokenizer.Tokens[tokenIndex].Length &&
						String.Compare(tokenizer.RawText, rawTextIndex, text, 0, text.Length, ignoreCase) == 0 );
			}
			
			
		/* Function: MatchesToken
		 * Applies a regular expression to the token and returns the Match object as if Regex.Match() was called.  If
		 * the iterator is out of bounds it will be applied to an empty string.
		 */
		public Match MatchesToken (System.Text.RegularExpressions.Regex regex)
			{
			if (!IsInBounds)
				{  return regex.Match("");  }
		
			return regex.Match(tokenizer.RawText, rawTextIndex, tokenizer.Tokens[tokenIndex].Length);
			}


		/* Function: MatchesAcrossTokens
		 * Returns whether the passed string matches the tokens at the current position.  The string comparison can 
		 * span multiple tokens, which allows you to test against things like "//" which would be two tokens.  However,
		 * the string must still match complete tokens so "// some" won't match "// something".  Returns false if the 
		 * iterator is out of bounds.
		 */
		public bool MatchesAcrossTokens (string text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return false;  }
				
			return ( TokensInCharacters(text.Length) != -1 &&
						String.Compare(tokenizer.RawText, rawTextIndex, text, 0, text.Length, ignoreCase) == 0 );
			}


		/* Function: AppendTokenTo
		 * Appends the token to the passed StringBuilder.  This is more efficient than appending the result of <String>
		 * because it copies directly from the source without creating an intermediate string.
		 */
		public void AppendTokenTo (System.Text.StringBuilder output)
			{
			int length = RawTextLength;  // Will be zero if out of bounds.

			if (length == 1)
				{  output.Append(tokenizer.RawText[rawTextIndex]);  }
			else if (length > 1)
				{  output.Append(tokenizer.RawText, rawTextIndex, length);  }
			}
			
			
		/* Function: ChangeType
		 * 
		 * Changes the current <TokenType> to something else.  You must respect the following rules though or
		 * it will throw an exception.
		 * 
		 * - You can switch tokens among text, symbols, whitespace, and code types.  If you change any token to
		 *   or from whitespace it will affect the values returned by <LineIterator>.
		 * - You cannot change line break tokens.  Otherwise the line number wouldn't match the source file anymore.
		 * - You cannot change null tokens.
		 */
		public void ChangeType (TokenType newType)
			{
			if (!IsInBounds)
				{  throw new InvalidOperationException();  }
				
			TokenType oldType = tokenizer.Tokens[tokenIndex].Type;
			
			// There will not be any oldTypes as null since we're in bounds and we can't manually change anything to it.
			if (oldType == TokenType.LineBreak || newType == TokenType.LineBreak || newType == TokenType.Null)
				{  throw new Exceptions.InvalidConversion(oldType, newType);  }
				
			// We have to do it this way because it's a struct.  We can't modify it inline.
			Token token;
			token.Length = tokenizer.Tokens[tokenIndex].Length;
			token.Type = newType;
			tokenizer.Tokens[tokenIndex] = token;
			}
			
			
		/* Function: ChangeTypeByCharacters
		 * 
		 * Changes the <TokenType> of the tokens encompassed by the passed number of characters.  See 
		 * <ChangeType()> for the rules you must follow when converting <TokenTypes>.
		 * 
		 * This throws an exception if the number of characters does not evenly fall on a token boundary.  It
		 * is assumed that this function will primarily be used after a positive result from <MatchesAcrossTokens()>
		 * or <TokensInCharacters()> which would cause this to not be an issue.
		 */
		public void ChangeTypeByCharacters (TokenType newType, int characters)
			{
			int tokenCount = TokensInCharacters(characters);
			
			if (tokenCount == -1)
				{  throw new InvalidOperationException();  }
			else if (tokenCount == 1)
				{  ChangeType(newType);  }
			else
				{
				TokenIterator iterator = this;
				
				while (tokenCount > 0)
					{
					iterator.ChangeType(newType);
					iterator.Next();
					tokenCount--;
					}
				}
			}

			
			
			
		// Group: Protected/Internal Functions
		// __________________________________________________________________________
		
		
		/* Function: TokenIterator
		 * Creates a new iterator with the passed variables.
		 */
		internal TokenIterator (Tokenizer newTokenizer, int newTokenIndex, int newRawTextIndex, int newLineNumber)	
			{
			tokenizer = newTokenizer;
			tokenIndex = newTokenIndex;
			rawTextIndex = newRawTextIndex;
			lineNumber = newLineNumber;
			}

			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: String
		 * Returns the token as a string, or an empty string if it's out of bounds.  Note that this allocates memory and 
		 * creates a copy of the string.  Whenever possible use functions like <MatchesToken()> and <AppendTokenTo()> to 
		 * work directly on the original memory, or use <RawTextIndex> and <RawTextLength> with <Tokenizer.RawText> 
		 * to access it yourself.
		 */
		public string String
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.RawText.Substring( rawTextIndex, tokenizer.Tokens[tokenIndex].Length );  }
				else
					{  return "";  }
				}
			}
			
		/* Property: Character
		 * Returns the first character of the token, or null if it's out of bounds.  This is useful for symbol tokens which will
		 * always be only one character long.
		 */
		public char Character
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.RawText[rawTextIndex];  }
				else
					{  return '\0';  }
				}
			}
			
		/* Property: Type
		 * The <TokenType> of the current token.  Will return <TokenType.Null> if the iterator is out of bounds.  See
		 * <ChangeType()> for the rules that apply when changing the type.
		 */
		public TokenType Type
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.Tokens[tokenIndex].Type;  }
				else
					{  return TokenType.Null;  }
				}
			set
				{  ChangeType(value);  }
			}
			
		/* Property: FundamentalType
		 * Returns the fundamental <TokenType> of the current token, regardless of whether it was changed to one of
		 * the code types, or <TokenType.Null> if the iterator is out of bounds.
		 */
		public TokenType FundamentalType
			{
			get
				{
				if (!IsInBounds)
					{  return TokenType.Null;  }
					
				else if (tokenizer.Tokens[tokenIndex].Type < TokenType.EndOfFundamentalTypes)
					{  return tokenizer.Tokens[tokenIndex].Type;  }
					
				else
					{  return Tokenizer.FundamentalTypeOf(tokenizer.RawText[rawTextIndex]);  }
				}
			}
			
		/* Property: LineNumber
		 * Returns the line number of the current token, or the one it left off on if it went out of bounds.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			}
			
		/* Property: RawTextIndex
		 * Returns the offset of the current token into <Tokenizer.RawText>.  Will be zero if it went past the beginning, or
		 * the index one past the last character if it went past the end.
		 */
		public int RawTextIndex
			{
			get
				{  return rawTextIndex;  }
			}
			
		/* Property: RawTextLength
		 * Returns the length of the current token in characters, or zero if the iterator is out of bounds.
		 */
		public int RawTextLength
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.Tokens[tokenIndex].Length;  }
				else
					{  return 0;  }
				}
			}
			
		/* Property: IsInBounds
		 * Whether the iterator is not past the beginning or end of the list of tokens.
		 */
		public bool IsInBounds
			{
			get
				{  return (tokenIndex >= 0 && tokenIndex < tokenizer.Tokens.Count);  }
			}
			
		/* Property: Tokenizer
		 * The <Tokenizer> associated with this iterator.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}

			
			
		// Group: Internal Properties
		// __________________________________________________________________________
		
			
		/* Property: TokenIndex
		 * The current index into <Tokenizer.Tokens>.
		 */
		internal int TokenIndex
			{
			get
				{  return tokenIndex;  }
			}
			
			
			
		// Group: Operators
		// __________________________________________________________________________


		public override bool Equals (object other)
			{
			if (other == null || (other is TokenIterator) == false)
				{  return false;  }
			else
				{
				return ( (object)tokenizer == (object)((TokenIterator)other).tokenizer && 
							tokenIndex == ((TokenIterator)other).tokenIndex );
				} 
			}

		public override int GetHashCode ()
			{
			return tokenIndex.GetHashCode();
			}
			
		public static bool operator== (TokenIterator a, TokenIterator b)
			{
			if ((object)a == null && (object)b == null)
				{  return true;  }
			else if ((object)a == null || (object)b == null)
				{  return false;  }
			else
				{  return ( (object)a.tokenizer == (object)b.tokenizer && a.tokenIndex == b.tokenIndex );  }
			}
			
		public static bool operator!= (TokenIterator a, TokenIterator b)
			{
			return !(a == b);
			}
			
		public static bool operator> (TokenIterator a, TokenIterator b)
			{
			if ((object)a == null || (object)b == null)
				{  throw new NullReferenceException();  }
			if ((object)a.tokenizer != (object)b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.tokenIndex > b.tokenIndex);
			}
			
		public static bool operator>= (TokenIterator a, TokenIterator b)
			{
			if ((object)a == null || (object)b == null)
				{  throw new NullReferenceException();  }
			if ((object)a.tokenizer != (object)b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.tokenIndex >= b.tokenIndex);
			}
			
		public static bool operator< (TokenIterator a, TokenIterator b)
			{
			return !(a >= b);
			}
			
		public static bool operator<= (TokenIterator a, TokenIterator b)
			{
			return !(a > b);
			}

			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: tokenizer
		 * The <Tokenizer> associated with this iterator.
		 */
		private Tokenizer tokenizer;
		
		/* var: tokenIndex
		 * The current index into <Tokenizer.Tokens>.  Can be a negative number if we're before the first token.
		 */
		private int tokenIndex;
		
		/* var: rawTextIndex
		 * The current index into <Tokenizer.RawText>.
		 */
		private int rawTextIndex;
		
		/* var: lineNumber
		 * The current line number.  Lines start at one.
		 */
		private int lineNumber;
	
		}
	}