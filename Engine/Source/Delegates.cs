﻿/* 
 * Class: GregValure.NaturalDocs.Engine.Delegates
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine
	{
	static public class Delegates
		{
		
		/* Property: NeverCancel
		 * A <CancelDelegate> that always returns false.
		 */
		public static CancelDelegate NeverCancel = delegate() {  return false;  };
		
		}

	/* Delegate: SimpleDelegate
	 * A parameterless delegate.
	 */
	public delegate void SimpleDelegate ();
	
	/* Delegate: CancelDelegate
	 * A delegate that returns a bool of whether to cancel an operation or not.
	 */
	public delegate bool CancelDelegate ();
		
	}