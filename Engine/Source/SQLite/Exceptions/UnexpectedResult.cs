﻿/* 
 * Class: GregValure.NaturalDocs.Engine.SQLite.UnexpectedResult
 * ____________________________________________________________________________
 * 
 * Thrown when SQLite returns an unexpected result code
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.SQLite.Exceptions
	{
	public class UnexpectedResult : Exception
		{
		public UnexpectedResult (string message, SQLite.API.Result result)
			: base (message + "  Unexpected SQLite result: " + result)
			{
			}
		}
	}