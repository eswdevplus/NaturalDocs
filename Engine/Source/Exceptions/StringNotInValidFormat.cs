﻿/* 
 * Class: GregValure.NaturalDocs.Engine.Exceptions.StringNotInValidFormat
 * ____________________________________________________________________________
 * 
 * An exception thrown when an input string needs to be in a particular format but is not.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Exceptions
	{
	public class StringNotInValidFormat : Exception
		{
		public StringNotInValidFormat (string inputString, string formatType)
			: base ("The string \"" + inputString + "\" is not in a valid format for " + formatType + ".")
			{
			}

		public StringNotInValidFormat (string inputString, object callingObject)
			: base ("The string \"" + inputString + "\" is not in a valid format for " + callingObject.GetType().Name + ".")
			{
			}
		}
	}