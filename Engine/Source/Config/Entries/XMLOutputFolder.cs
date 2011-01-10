﻿/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Entries.XMLOutputFolder
 * ____________________________________________________________________________
 * 
 * An <Entry> for a XML output folder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config.Entries
	{
	public class XMLOutputFolder : OutputEntry
		{
		
		public XMLOutputFolder (Path folder) : base (folder)
			{
			}

		public override bool IsSameFundamentalEntry (Entry other)
		    {
		    return ( other is XMLOutputFolder && 
		               (other as XMLOutputFolder).Folder == Folder );
		    }

		public override void CopyUnsetPropertiesFrom (Entry other)
			{
			if (Number == 0)
				{  Number = (other as XMLOutputFolder).Number;  }
			}

		}
	}