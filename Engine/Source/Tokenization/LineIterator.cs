﻿/* 
 * Struct: GregValure.NaturalDocs.Engine.Tokenization.LineIterator
 * ____________________________________________________________________________
 * 
 * An iterator to go through a <Tokenizer> line by line instead of token by token.
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


namespace GregValure.NaturalDocs.Engine.Tokenization
	{
	public struct LineIterator
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Next
		 * Moves to the next line, returning false if we've moved past the end.
		 */
		public bool Next ()
			{
			return Next(1);
			}
			
			
		/* Function: Next (count)
		 * Moves forward the specified number of lines, returning false if we've moved past the end.
		 */
		public bool Next (int count)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }
				
			if (lineIndex < 0)
				{
				if (lineIndex + count <= 0)
					{  
					lineIndex += count;
					return false;
					}
				else
					{
					count += lineIndex;
					lineIndex = 0;
					}
				}
				
			while (count > 0 && lineIndex < tokenizer.Lines.Count)
				{
				tokenIndex += tokenizer.Lines[lineIndex].TokenLength;
				rawTextIndex += tokenizer.Lines[lineIndex].RawTextLength;
				lineIndex++;
				
				count--;
				}
				
			if (count > 0)
				{  lineIndex += count;  }
				
			return (lineIndex < tokenizer.Lines.Count);
			}
			
			
		/* Function: Previous
		 * Moves to the previous line, returning false if we've move past the beginning.
		 */
		public bool Previous ()
			{
			return Previous(1);
			}
			
			
		/* Function: Previous (count)
		 * Moves backwards the specified number of lines, returning false if we've move past the beginning.
		 */
		public bool Previous (int count)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }
				
			if (lineIndex > tokenizer.Lines.Count)
				{
				if (lineIndex - count >= tokenizer.Lines.Count)
					{
					lineIndex -= count;
					return false;
					}
				else
					{
					count -= lineIndex - tokenizer.Lines.Count;
					lineIndex = tokenizer.Lines.Count;
					}
				}
				
			while (count > 0 && lineIndex > 0)
				{
				lineIndex--;
				tokenIndex -= tokenizer.Lines[lineIndex].TokenLength;
				rawTextIndex -= tokenizer.Lines[lineIndex].RawTextLength;
				
				count--;
				}
				
			if (count > 0)
				{  lineIndex -= count;  }
			
			return (lineIndex >= 0);
			}
			
			
		/* Function: FirstToken
		 * Returns a <TokenIterator> at the beginning of the current line.  If the iterator is out of bounds it will be 
		 * set to the first line or one past the last token, depending on which edge it has gone off.
		 */
		public TokenIterator FirstToken (LineBoundsMode boundsMode)
			{
			if (lineIndex >= tokenizer.Lines.Count)
				{  
				return new TokenIterator(tokenizer, tokenizer.Tokens.Count, tokenizer.RawText.Length, 
													 tokenizer.StartingLineNumber + tokenizer.Lines.Count - 1);
				}
			else if (lineIndex <= 0)
				{  
				return new TokenIterator(tokenizer, tokenIndex, rawTextIndex, tokenizer.StartingLineNumber);
				}
			else
				{
				int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
				CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
				
				return new TokenIterator(tokenizer, tokenStart, rawTextStart, tokenizer.StartingLineNumber + lineIndex);
				}
			}
			
			
		/* Function: GetBounds
		 * Sets two <TokenIterators> to the beginning and end of the current line.  If the iterator is out of bounds they
		 * will be equal.
		 */
		public void GetBounds (LineBoundsMode boundsMode, out TokenIterator lineStart, out TokenIterator lineEnd)
			{
			if (!IsInBounds)
				{  
				lineStart = FirstToken(boundsMode);
				lineEnd = lineStart;
				}
			else
				{
				int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
				CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
				
				lineStart = new TokenIterator(tokenizer, tokenStart, rawTextStart, tokenizer.StartingLineNumber + lineIndex);
				lineEnd = new TokenIterator(tokenizer, tokenEnd, rawTextEnd, tokenizer.StartingLineNumber + lineIndex);
				}
			}
			
			
		/* Function: GetRawTextBounds
		 * Returns the location of the line in <Tokenizer.RawText>.
		 */
		public void GetRawTextBounds (LineBoundsMode boundsMode, out int lineStartIndex, out int lineEndIndex)
		    {
		    int tokenStart, tokenEnd;
		    CalculateBounds(boundsMode, out lineStartIndex, out lineEndIndex, out tokenStart, out tokenEnd);
		    }


		/* Function: String
		 * Returns the line as a string.  Note that this allocates a memory copy.  For efficiency, it's preferrable to work on the
		 * original memory whenever possible with functions like <RawTextLocation()> and <Match()>.
		 */
		public string String (LineBoundsMode boundsMode)
			{
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);

			return tokenizer.RawText.Substring(rawTextStart, rawTextEnd - rawTextStart);			
			}
			
			
		/* Function: IsEmpty
		 * Returns whether the current line is empty according to the <LineBoundsMode>.
		 */
		public bool IsEmpty (LineBoundsMode boundsMode)
			{
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
			
			return (rawTextStart == rawTextEnd);
			}


		/* Function: Indent
		 * Returns the indent of the current line content according to the <LineBoundsMode>, expanding tabs.
		 */
		public int Indent (LineBoundsMode boundsMode)
			{
			int rawTextBoundsStart, rawTextBoundsEnd, tokenBoundsStart, tokenBoundsEnd;
			CalculateBounds(boundsMode, out rawTextBoundsStart, out rawTextBoundsEnd, out tokenBoundsStart, out tokenBoundsEnd);
			
			int indent = 0;
			string rawText = tokenizer.RawText;
			
			for (int i = rawTextIndex; i < rawTextBoundsStart; i++)
				{
				if (rawText[i] == '\t')
					{
					indent += Engine.Instance.Config.TabWidth;
					indent -= (indent % Engine.Instance.Config.TabWidth);
					}
				else
					{  indent++;  }
				}
				
			return indent;
			}
		 
		 
		/* Function: Match
		 * Applies a regular expression to the line and returns the Match object as if Regex.Match() was called.  If
		 * the iterator is out of bounds it will be applied to an empty string.
		 */
		public System.Text.RegularExpressions.Match Match (System.Text.RegularExpressions.Regex regex, LineBoundsMode boundsMode)
			{
			if (!IsInBounds)
				{  return regex.Match("");  }
		
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
			
			return regex.Match(tokenizer.RawText, rawTextStart, rawTextEnd - rawTextStart);
			}
			
			
		/* Function: FindToken
		 * Attempts to find the passed string as a token in the line, and set a <TokenIterator> at its position if successful.  
		 * The string must match the entire token, so "some" will not match "something".
		 */
		public bool FindToken (string text, bool ignoreCase, LineBoundsMode boundsMode, out TokenIterator result)
			{
			TokenIterator acrossTokensResult;
			
			if (FindAcrossTokens(text, ignoreCase, boundsMode, out acrossTokensResult) == false ||
				acrossTokensResult.RawTextLength != text.Length)
				{  
				result = new TokenIterator();
				return false;
				}
			else
				{  
				result = acrossTokensResult;
				return true;
				}
			}
			
			
		/* Function: FindAcrossTokens
		 * Attempts to find the passed string in the line, and sets a <TokenIterator> at its position if successful.  This function 
		 * can cross token boundaries, so you can search for "<<" even though that would normally be two tokens.  The result 
		 * must match complete tokens though, so "<< some" will not match "<< something".
		 */
		public bool FindAcrossTokens (string text, bool ignoreCase, LineBoundsMode boundsMode, out TokenIterator result)
			{
			if (!IsInBounds)
				{  
				result = new TokenIterator();  
				return false;
				}
				
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);

			int resultIndex = tokenizer.RawText.IndexOf( text, rawTextStart, rawTextEnd - rawTextStart,
																			  (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : 
																								   StringComparison.CurrentCulture) );
																									   
			if (resultIndex == -1)
				{  
				result = new TokenIterator();  
				return false;
				}
				
			result = new TokenIterator(tokenizer, tokenStart, rawTextStart, LineNumber);
			
			if (resultIndex != rawTextStart)
				{  result.NextByCharacters(resultIndex - rawTextStart);  }
			
			return true;
			}



			
		// Group: Internal Functions
		// __________________________________________________________________________
			

		/* Function: LineIterator
		 * Creates a new iterator from the passed parameters.
		 */
		internal LineIterator (Tokenizer newTokenizer, int newLineIndex, int newTokenIndex, int newRawTextIndex)
			{
			tokenizer = newTokenizer;
			lineIndex = newLineIndex;
			tokenIndex = newTokenIndex;
			rawTextIndex = newRawTextIndex;
			}
			
			
		/* Function: CalculateBounds
		 * Determines and returns the bounds of the current line according to the <LineBoundsMode>.
		 */
		internal void CalculateBounds (LineBoundsMode boundsMode, out int rawTextStart, out int rawTextEnd,
													 out int tokenStart, out int tokenEnd)
			{
			if (!IsInBounds)
				{
				rawTextStart = 0;
				rawTextEnd = 0;
				tokenStart = 0;
				tokenEnd = 0;
				
				return;
				}
				
			rawTextStart = rawTextIndex;
			rawTextEnd = rawTextIndex + tokenizer.Lines[lineIndex].RawTextLength;
			tokenStart = tokenIndex;
			tokenEnd = tokenIndex + tokenizer.Lines[lineIndex].TokenLength;
			
			if (boundsMode == LineBoundsMode.Everything)
				{  return;  }
				
			while (tokenEnd > tokenStart && IsSkippable(tokenizer.Tokens[tokenEnd - 1].Type, boundsMode))
				{
				tokenEnd--;
				rawTextEnd -= tokenizer.Tokens[tokenEnd].Length;
				}
				
			while (tokenStart < tokenEnd && IsSkippable(tokenizer.Tokens[tokenStart].Type, boundsMode))
				{
				rawTextStart += tokenizer.Tokens[tokenStart].Length;
				tokenStart++;
				}
			}
			
			
		/* Function: IsSkippable
		 * Returns whether the <TokenType> should be skipped based on the passed <LineBoundsMode>.
		 */
		internal bool IsSkippable (TokenType type, LineBoundsMode boundsMode)
			{
			if (boundsMode == LineBoundsMode.Everything)
				{  return false;  }
			else if (boundsMode == LineBoundsMode.ExcludeWhitespace)
				{  return (type == TokenType.Whitespace || type == TokenType.LineBreak);  }
			else // LineBoundsMode.CommentContent
				{
				return (type == TokenType.Whitespace || type == TokenType.LineBreak ||
						   type == TokenType.CommentSymbol || type == TokenType.CommentDecoration);
				}
			}
			

			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: LineNumber
		 */
		public int LineNumber
			{
			get
				{  return (tokenizer.StartingLineNumber + lineIndex);  }
			}
			
			
		/* Property: IsInBounds
		 * Whether the iterator is not past the beginning or end of the tokens.
		 */
		public bool IsInBounds
			{
			get
				{  return (lineIndex >= 0 && lineIndex < tokenizer.Lines.Count);  }
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
		
		
		/* Property: RawTextIndex
		 * The index into <Tokenizer.RawText> of the beginning of the current line.
		 */
		internal int RawTextIndex
			{
			get
				{  return rawTextIndex;  }
			}
			
			
		/* Property: TokenIndex
		 * The index into <Tokenizer.Tokens> of the beginning of the current line.
		 */
		internal int TokenIndex
			{
			get
				{  return tokenIndex;  }
			}
			
			
		/* Property: LineIndex
		 * The current line's index into <Tokenizer.Lines>.
		 */
		internal int LineIndex
			{
			get
				{  return lineIndex;  }
			}



		// Group: Operators
		// __________________________________________________________________________


		public override bool Equals (object other)
			{
			if (other == null || (other is LineIterator) == false)
				{  return false;  }
			else
				{
				return ( (object)tokenizer == (object)((LineIterator)other).tokenizer && 
							lineIndex == ((LineIterator)other).lineIndex);
				} 
			}

		public override int GetHashCode ()
			{
			return lineIndex.GetHashCode();
			}
			
		public static bool operator== (LineIterator a, LineIterator b)
			{
			if ((object)a == null && (object)b == null)
				{  return true;  }
			else if ((object)a == null || (object)b == null)
				{  return false;  }
			else
				{  return ( (object)a.tokenizer == (object)b.tokenizer && a.lineIndex == b.lineIndex );  }
			}
			
		public static bool operator!= (LineIterator a, LineIterator b)
			{
			return !(a == b);
			}
			
		public static bool operator> (LineIterator a, LineIterator b)
			{
			if ((object)a == null || (object)b == null)
				{  throw new NullReferenceException();  }
			if ((object)a.tokenizer != (object)b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.lineIndex > b.lineIndex);
			}
			
		public static bool operator>= (LineIterator a, LineIterator b)
			{
			if ((object)a == null || (object)b == null)
				{  throw new NullReferenceException();  }
			if ((object)a.tokenizer != (object)b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.lineIndex >= b.lineIndex);
			}
			
		public static bool operator< (LineIterator a, LineIterator b)
			{
			return !(a >= b);
			}
			
		public static bool operator<= (LineIterator a, LineIterator b)
			{
			return !(a > b);
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: tokenizer
		 */
		private Tokenizer tokenizer;
		
		/* var: lineIndex
		 * Remember that this is an index, not a line number.
		 */
		private int lineIndex;
		
		/* var: tokenIndex
		 */
		private int tokenIndex;
		
		/* var: rawTextIndex
		 */
		private int rawTextIndex;
		
		}
	}