﻿/* 
 * Class: GregValure.NaturalDocs.Engine.Config.ConfigFileParser
 * ____________________________________________________________________________
 * 
 * A class that handles the loading and saving of <Project.txt> and <Project.nd>.  This is implemented with a separate class
 * instead of being static functions in <Config.Manager> so that it can be extended via a derived class.
 * 
 * 
 * Group: Files
 * ____________________________________________________________________________
 * 
 * File: Project.txt
 * 
 *		The configuration file that defines the project settings for Natural Docs.  Various settings may be overridden by the command
 *		line, though.
 *		
 * 
 *		Project Information:
 *		
 *			> Title: [title]
 *			> Subtitle: [subtitle]
 *			> Copyright: [copyright]
 *
 *			The title, sub-title, and copyright notice for the project.  (C), (R), and (TM) will be converted into their respective symbols.
 *			
 *			> Timestamp: [timestamp code]
 *			
 *			The timestamp code is a regular string with the following subsitutions performed:
 *			
 *				m - Single digit month, where applicable.  January is "1".
 *				mm - Always double digit month.  January is "01".
 *				mon - Short month word.  January is "Jan".
 *				month - Long month word.  January is "January".
 *				d - Single digit day, where applicable.  1 is "1".
 *				dd - Always double digit day.  1 is "01".
 *				day - Day with text extension.  1 is "1st".
 *				yy - Double digit year.  2011 is "11".
 *				yyyy - Four digit year.  2011 is "2011".
 *				year - Four digit year.  2011 is "2011".
 *			
 *			Anything else is left literal in the output.  The substitution requires a non-letter on each side, so every m will not turn into
 *			the month.
 *			
 *			> Style: [name]
 *			
 *			The style to apply to all output entries.
 *			
 * 
 *		Input Entries:
 * 
 *			> Source Folder: [path]
 *			> Source Folder [number]: [path]
 *			>    Name: [name]
 *				
 *			Specifies a source folder.  Relative paths are relative to the project folder.
 *			
 *			A number can optionally be specified.  If none is, it uses the lowest number not in use by another entry.  Each source
 *			entry must have a unique number.
 *			
 *			Name can optionally be specified.  It's not required if there is only one source folder.  If there's more than one and it's 
 *			not specified it will be autogenerated.
 *			
 *			> Image Folder: [path]
 *			> Image Folder [number]: [path]
 *			
 *			Specifies an image folder.  Relative paths are relative to the project folder.
 *			
 *			A number can optionally be specified.  If none is, it uses the lowest number not in use by another entry.  Each image
 *			entry must have a unique number.
 *			
 * 
 *		Filter Entries:
 *		
 *			> Ignore Source Folder: [path]
 *			
 *			Specifies a source folder that should not be scanned for files.  Relative paths are relative to the project folder.
 *			
 *			> Ignore Source Folder Pattern: [pattern]
 *			
 *			Specifies a pattern that should cause a source folder to be ignored if it matches.  ? matches a single character, * matches 
 *			zero or more characters.  It applies to the entire folder name, so "cli" will not match "client", although "cli*" will.
 *			
 * 
 *		Output Entries:
 * 
 *			> HTML Output Folder: [path]
 *			>    [Project Info Settings]
 *			
 *			Specifies a HTML output folder.  Relative paths are relative to the project folder.
 *			
 *			Each folder may override any of the project information settings if desired.  This allows projects with multiple output targets
 *			to give each one its own style, subtitle, etc.
 *			
 *			> XML Output Folder: [path]
 *			>    [Project Info Settings]
 *			
 *			Specifies an XML output folder.  Relative paths are relative to the project folder.  Like HTML, project information settings 
 *			can be overridden.
 *			
 *			Although internally output entries have numbers like input entries, they're not stored in this file because it's not important
 *			for them to remain consistent from one computer to the next.  They're only used for temporary data, whereas input entry
 *			numbers affect the generated output file paths (/files2, etc.) and thus need to remain consistent so URLs don't constantly
 *			change.
 *			
 * 
 *		Global Settings:
 * 
 *			> Tab Width: [width]
 *			
 *			The number of spaces a tab should be expanded to.
 *			
 * 
 *		Revisions:
 * 
 *		2.0:
 *		
 *			The initial version of this file.
 *			
 * 
 * 
 * File: Project.nd
 * 
 *		A binary file which stores some of the previous settings of <Project.txt>.  Only settings relevant to the global operation of 
 *		the program are stored.  Information that is only relevant to the output builders is not because whether a change is significant
 *		and what its effects are are dependent on the builders themselves.  They are expected to track any changes that are relevant
 *		themselves.
 *		
 *		Format:
 *		
 *			> [[Binary Header]]
 *		
 *			The file starts with the standard binary file header as managed by <BinaryFile>.
 *			
 *			> [Int32: Tab Width]
 *
 *			Global properties.
 *			
 *			> [String: Identifier]
 *			> [[Properties]]
 *			> ...
 *			> [String: null]
 *			
 *			A segment of data for each <Entry>.  They each have an identification string which one <Entry> should claim, and the
 *			following properties and their encodings are specific to the extensions.  For simplicity, <Entries> should use the same
 *			identifiers they use in <Project.txt>.
 *			
 *			Segments are only created for input and output entries, not for filters.  When a filter is changed from one run to the
 *			next it will be automatically reflected in the file scans, so there is no need to detect it.  A new output target requires a
 *			full rebuild, and knowing its number is important for allowing it to keep intermediate data.
 *			
 *			xxx source and image data is only important until we transition to having the file source number included in all file information.
 *			once that it done it won't be necessary to store here anymore.
 *			
 *			Segments continue until a null identifier is reached.
 *			
 *			> [String: Identifier="Source Folder"]
 *			> [String: Absolute Path]
 *			> [Int32: Number]
 *		
 *			> [String: Identifier="Image Folder"]
 *			> [String: Absolute Path]
 *			> [String: Number]
 * 
 *			> [String: Identifier="HTML Output Folder"]
 *			> [String: Absolute Path]
 *			> [Int32: Number]
 *		
 *			> [String: Identifier="XML Output Folder"]
 *			> [String: Absolute Path]
 *			> [Int32: Number]
 *			
 *			Project information is not stored, either at the global or output entry level, as it is only relevant to the output builders.
 *			The same applies for the names of input folders.
 * 
 *		Revisions:
 *		
 *			2.0:
 *			
 *				- The file is introduced.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Config
	{
	public class ConfigFileParser
		{

		// Group: Functions
		// __________________________________________________________________________


		public ConfigFileParser ()
			{
			configFile = null;
			binaryConfigFile = null;
			configFilePath = null;
			configFileData = null;

			subtitleRegex = null;
			timestampRegex = null;
			tabWidthRegex = null;

			sourceFolderRegex = null;
			imageFolderRegex = null;
			htmlOutputFolderRegex = null;
			xmlOutputFolderRegex = null;
			ignoredSourceFolderRegex = null;
			ignoredSourceFolderPatternRegex = null;
			}

					
		/* Function: LoadFile
		 * 
		 * Loads and parses the information in <Project.txt>, returning whether it was successful.  If it was, the extracted data will
		 * be available in <ParsedData>.  If not, errors explaining why will be added to the list. 
		 * 
		 */
		public bool LoadFile (Path filename, Errors.ErrorList errorList)
			{
			configFilePath = filename;
			configFileData = new ConfigData();
			int previousErrorCount = errorList.Count;

			using (configFile = new ConfigFile())
				{
				// We don't condense value whitespace because some things like title, subtitle, and copyright
				// may want multiple spaces.
				bool openResult = configFile.Open(configFilePath, 
																			 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
																			 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
																			 errorList);
														 
				if (openResult == false)
					{  return false;  }

				if (subtitleRegex == null)
					{					
					subtitleRegex = new Regex.Config.Subtitle();
					timestampRegex = new Regex.Config.Timestamp();
					tabWidthRegex = new Regex.Config.TabWidth();

					sourceFolderRegex = new Regex.Config.SourceFolder();
					imageFolderRegex = new Regex.Config.ImageFolder();
					htmlOutputFolderRegex = new Regex.Config.HTMLOutputFolder();
					xmlOutputFolderRegex = new Regex.Config.XMLOutputFolder();
					ignoredSourceFolderRegex = new Regex.Config.IgnoredSourceFolder();
					ignoredSourceFolderPatternRegex = new Regex.Config.IgnoredSourceFolderPattern();
					}

				string lcIdentifier, value;
				Entry lastEntry = null;
				ProjectInfo targetProjectInfo = configFileData.ProjectInfo;
				
				while (configFile.Get(out lcIdentifier, out value))
					{
					if (GetEntryHeader(lcIdentifier, value) == true)
						{
						// GetEntryHeader will always add an entry if it returns true, even if there's an error in it.
						lastEntry = configFileData.Entries[ configFileData.Entries.Count - 1 ];

						if (lastEntry is OutputEntry)
							{  targetProjectInfo = (lastEntry as OutputEntry).ProjectInfo;  }
						}
					else if (GetGlobalProperty(lcIdentifier, value) == false &&
								 GetProjectInfoProperty(lcIdentifier, value, targetProjectInfo) == false &&
								 (lastEntry == null || GetEntryProperty(lcIdentifier, value, lastEntry)) == false)
						{
					   configFile.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );
						}
					}
				
				configFile.Close();				
				}
				
				
			if (errorList.Count == previousErrorCount)
				{  return true;  }
			else
				{
				// Replace what we have so far with an empty structure.
				configFileData = new ConfigData();
				return false;
				}
			}


		/* Function: SaveFile
		 * Saves the passed <ConfigData> into <Project.txt>, returning whether it was successful.
		 */
		public bool SaveFile (Path filename, ConfigData content, Errors.ErrorList errorList)
			{
			configFilePath = filename;
			configFileData = content;

			StringBuilder output = new StringBuilder(1024);
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();

			AppendFileHeader(output);			
			
			AppendProjectInfo(output);
			AppendInputEntries(output);
			AppendFilterEntries(output);
			AppendOutputEntries(output);
			AppendGlobalSettings(output);
			
			return ConfigFile.SaveIfDifferent(filename, output.ToString(), false, errorList);
			}


		/* Function: LoadBinaryFile
		 * Loads the information in <Project.nd>, returning whether it was successful.
		 */
		public bool LoadBinaryFile (Path filename)
			{
			configFilePath = filename;
			configFileData = new ConfigData();
			binaryConfigFile = new BinaryFile();
			bool result = true;
			
			try
				{
				if (binaryConfigFile.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
					
					// [Int32: Tab Width]

					configFileData.TabWidth = binaryConfigFile.ReadInt32();
					
					// [String: Identifier]
					// [[Properties]]
					// ...
					// [String: null]

					string identifier = binaryConfigFile.ReadString();

					while (identifier != null)
						{
						ReadBinaryEntry(identifier);
						identifier = binaryConfigFile.ReadString();
						}
					}
				}
			catch
				{
				result = false;

				// Reset everything.
				configFileData = new ConfigData();
				}
			finally
				{
				binaryConfigFile.Close();
				}
				
			return result;
			}


		/* Function: SaveBinaryFile
		 * Saves the passed <ConfigData> into <Project.nd>.  Throws an exception if unsuccessful.
		 */
		public void SaveBinaryFile (Path filename, ConfigData content)
			{
			configFilePath = filename;
			configFileData = content;

			binaryConfigFile = new BinaryFile();
			binaryConfigFile.OpenForWriting(filename);

			try
				{

				// [Int32: Tab Width]

				binaryConfigFile.WriteInt32(content.TabWidth);
				
				// [String: Identifier]
				// [[Properties]]
				// ...
				// [String: null]
				
				foreach (Entry entry in content.Entries)
				    {
				    // Ignore Filter types.
				    if (entry is InputEntry || entry is OutputEntry)
				        {  WriteBinaryEntry(entry);  }
				    }
					
				binaryConfigFile.WriteString(null);
				}
				
			finally
				{
				binaryConfigFile.Close();
				}
			}


		/* Function: LoadOldMenuFile
		 * 
		 * Loads and parses the information from a pre-2.0 version of <Menu.txt>.  This will only pick up things like the project title.
		 * No errors will be reported if parsing goes badly.  Returns whether it was successful.
		 */
		public virtual bool LoadOldMenuFile (Path filename)
			{
			configFilePath = filename;
			configFileData = new ConfigData();

			using (configFile = new ConfigFile())
				{
				Errors.ErrorList ignored = new Errors.ErrorList();
				bool openResult = configFile.Open(filename, 
																				  ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
																				  ConfigFile.FileFormatFlags.SupportsBraces |
																				  ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
																				  ignored);
														 
				if (openResult == false)
					{  return false;  }
					
				if (configFile.Version > "1.99")
					{  return false;  }
					
				Regex.Config.Subtitle subtitleRegex = new Regex.Config.Subtitle();
				Regex.Config.Timestamp timestampRegex = new Regex.Config.Timestamp();
					
				string lcIdentifier, value;
				
				while (configFile.Get(out lcIdentifier, out value))
					{
					if (lcIdentifier == "title")
						{  configFileData.Title = TextConverter.ConvertCopyrightAndTrademark(value);  }
					else if (subtitleRegex.IsMatch(lcIdentifier))
						{  configFileData.Subtitle = TextConverter.ConvertCopyrightAndTrademark(value);  }
					else if (lcIdentifier == "footer" || lcIdentifier == "copyright")
						{  configFileData.Copyright = TextConverter.ConvertCopyrightAndTrademark(value);  }
					else if (timestampRegex.IsMatch(lcIdentifier))
						{  configFileData.TimestampCode = value;  }
					// Otherwise just ignore the entry.
					}
				
				configFile.Close();				
				}

			return true;				
			}



		// Group: Reading Functions
		// __________________________________________________________________________


		/* Function: GetGlobalProperty
		 * If the passed identifier is a global property like Title, handles it and returns true.  If the value is invalid it will add an
		 * error to <errorList> and still return true.  It only returns false if this is an unrecognized property.
		 */
		protected virtual bool GetGlobalProperty (string lcIdentifier, string value)
			{
			if (tabWidthRegex.IsMatch(lcIdentifier))
				{
				int tabWidth = 0;
						
				if (Int32.TryParse(value, out tabWidth) == true)
					{  
					if (tabWidth > 0)
						{  configFileData.TabWidth = tabWidth;  }
					else
						{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Error.TabWidthMustBeGreaterThanZero") );  }
					}
				else
					{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Error.TabWidthMustBeANumber") );  }
				}
			else
				{
				return false;
				}

			return true;
			}


		/* Function: GetProjectInfoProperty
		 * If the passed identifier is a project info property like Title, handles it and returns true.  It will add it to the passed
		 * <ProjectInfo> object so this can be used with the global settings or an output target's settings.  If the value is invalid 
		 * it will add an error to <errorList> and still return true.  It only returns false if this is an unrecognized property.
		 */
		protected virtual bool GetProjectInfoProperty (string lcIdentifier, string value, ProjectInfo projectInfo)
			{
			if (lcIdentifier == "title")
				{
				projectInfo.Title = TextConverter.ConvertCopyrightAndTrademark(value);
				}
			else if (subtitleRegex.IsMatch(lcIdentifier))
				{
				projectInfo.Subtitle = TextConverter.ConvertCopyrightAndTrademark(value);
				}
			else if (lcIdentifier == "copyright")
				{
				projectInfo.Copyright = TextConverter.ConvertCopyrightAndTrademark(value);
				}
			else if (timestampRegex.IsMatch(lcIdentifier))
				{
				projectInfo.TimeStampCode = value;
				}
			else if (lcIdentifier == "style")
				{
				projectInfo.StyleName = value;
				}
			else
				{
				return false;
				}

			return true;
			}


		/* Function: GetEntryHeader
		 * If the passed identifier starts a block entry like "Source Folder", adds an item to <configFileData.Entries> and 
		 * returns true.  If the value is invalid it will still add an entry, add an error to <errorList>, and still return true.  
		 * The entry is added so its settings will not cause errors.  It will only return false if the identifier is unrecognized.
		 */
		protected virtual bool GetEntryHeader (string lcIdentifier, string value)
			{

			// Source folder

			System.Text.RegularExpressions.Match match = sourceFolderRegex.Match(lcIdentifier);

			if (match.Success)
				{
				Path folderPath = value;

				if (folderPath.IsRelative)
					{  folderPath = configFilePath.ParentFolder + "/" + folderPath;  }

				Entries.InputFolder entry = new Entries.InputFolder(folderPath, Files.InputType.Source);
				int number = 0;

				if (int.TryParse(match.Groups[1].Value, out number))
					{  entry.Number = number;  }

				// We add it regardless of whether the folder exists so that later properties don't cause errors.
				configFileData.Entries.Add(entry);

				if (System.IO.Directory.Exists(folderPath) == false)
					{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceFolderDoesNotExist(folder)", folderPath) );  }

				return true;
				}
				

			// Image folder

			match = imageFolderRegex.Match(lcIdentifier);

			if (match.Success)
				{  
				Path folderPath = value;

				if (folderPath.IsRelative)
					{  folderPath = configFilePath.ParentFolder + "/" + folderPath;  }

				Entries.InputFolder entry = new Entries.InputFolder(folderPath, Files.InputType.Image);
				int number = 0;

				if (int.TryParse(match.Groups[1].Value, out number))
					{  entry.Number = number;  }

				// We add it regardless of whether the folder exists so that later properties don't cause errors.
				configFileData.Entries.Add(entry);

				if (System.IO.Directory.Exists(folderPath) == false)
					{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Project.txt.ImageFolderDoesNotExist(folder)", folderPath) );  }

				return true;
				}


			// HTML output folder
				
			else if (htmlOutputFolderRegex.IsMatch(lcIdentifier))
				{  
				Path folderPath = value;

				if (folderPath.IsRelative)
					{  folderPath = configFilePath.ParentFolder + "/" + folderPath;  }

				Entries.HTMLOutputFolder entry = new Entries.HTMLOutputFolder(folderPath);

				// We add it regardless of whether the folder exists so that later properties don't cause errors.
				configFileData.Entries.Add(entry);

				if (System.IO.Directory.Exists(folderPath) == false)
					{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputFolderDoesNotExist(folder)", folderPath) );  }

				return true;
				}


			// XML output folder
				
			else if (xmlOutputFolderRegex.IsMatch(lcIdentifier))
				{  
				Path folderPath = value;

				if (folderPath.IsRelative)
					{  folderPath = configFilePath.ParentFolder + "/" + folderPath;  }

				Entries.XMLOutputFolder entry = new Entries.XMLOutputFolder(folderPath);

				// We add it regardless of whether the folder exists so that later properties don't cause errors.
				configFileData.Entries.Add(entry);

				if (System.IO.Directory.Exists(folderPath) == false)
					{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputFolderDoesNotExist(folder)", folderPath) );  }

				return true;
				}

				
			// Ignored source folder
				
			else if (ignoredSourceFolderRegex.IsMatch(lcIdentifier))
				{  
				Path folderPath = value;

				if (folderPath.IsRelative)
					{  folderPath = configFilePath.ParentFolder + "/" + folderPath;  }

				Entries.IgnoredSourceFolder entry = new Entries.IgnoredSourceFolder(folderPath);

				// We add it regardless of whether the folder exists so that later properties don't cause errors.
				configFileData.Entries.Add(entry);

				if (System.IO.Directory.Exists(folderPath) == false)
					{  configFile.AddError( Locale.Get("NaturalDocs.Engine", "Project.txt.IgnoredSourceFolderDoesNotExist(folder)", folderPath) );  }

				return true;
				}
				

			// Ignored source folder pattern
				
			else if (ignoredSourceFolderPatternRegex.IsMatch(lcIdentifier))
				{  
				Entries.IgnoredSourceFolderPattern entry = new Entries.IgnoredSourceFolderPattern(value);
				configFileData.Entries.Add(entry);
				return true;
				}
				

			else
				{  return false;  }
			}


		/* Function: GetEntryProperty
		 * If the passed identifier is a valid keyword for passed <Entry>, applies the property and returns true.  This does not 
		 * cover the project info settings for <OutputEntries>, use <GetProjectInfoProperty()> for that instead.  If the value is 
		 * invalid it will add an error to <errorList> and still return true.  It will only return false if the identifier is unrecognized.
		 */
		protected virtual bool GetEntryProperty (string lcIdentifier, string value, Entry entry)
			{
			if (lcIdentifier == "name")
				{
				 if (entry is Entries.InputFolder && 
					  (entry as Entries.InputFolder).InputType == Files.InputType.Source)
					{
					(entry as Entries.InputFolder).Name = value;
					}
				else
					{
					configFile.AddError( Locale.Get("NaturalDocs.Engine", "Project.txt.NameOnlyAppliesToSourceFolders") );
					}

				return true;
				}
				
			else
				{  return false;  }
			}


		/* Function: ReadBinaryEntry
		 * Creates an <Entry> from the passed identifier and adds it to <configFileData>.  Reads any additional properties it
		 * may have from <binaryConfigFile>, which will be in position right after the identifier string.
		 */
		protected virtual void ReadBinaryEntry (string identifier)
			{
			if (identifier == "Source Folder")
				{  ReadBinarySourceFolderEntry();  }
			else if (identifier == "Image Folder")
				{  ReadBinaryImageFolderEntry();  }
			else if (identifier == "HTML Output Folder")
				{  ReadBinaryHTMLOutputFolderEntry();  }
			else if (identifier == "XML Output Folder")
				{  ReadBinaryXMLOutputFolderEntry();  }
			else
				{  throw new Exception("Unknown Project.nd entry " + identifier);  }
			}


		protected virtual void ReadBinarySourceFolderEntry ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			string path = binaryConfigFile.ReadString();

			Entries.InputFolder entry = new Entries.InputFolder(path, Files.InputType.Source);

			entry.Number = binaryConfigFile.ReadInt32();
			configFileData.Entries.Add(entry);
			}


		protected virtual void ReadBinaryImageFolderEntry ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			string path = binaryConfigFile.ReadString();

			Entries.InputFolder entry = new Entries.InputFolder(path, Files.InputType.Image);

			entry.Number = binaryConfigFile.ReadInt32();
			configFileData.Entries.Add(entry);
			}


		protected virtual void ReadBinaryHTMLOutputFolderEntry ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			string path = binaryConfigFile.ReadString();
			Entries.HTMLOutputFolder entry = new Entries.HTMLOutputFolder(path);

			entry.Number = binaryConfigFile.ReadInt32();

			configFileData.Entries.Add(entry);
			}


		protected virtual void ReadBinaryXMLOutputFolderEntry ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			string path = binaryConfigFile.ReadString();
			Entries.XMLOutputFolder entry = new Entries.XMLOutputFolder(path);

			entry.Number = binaryConfigFile.ReadInt32();

			configFileData.Entries.Add(entry);
			}



		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: AppendFileHeader
		 * Appends the general file header to the passed string.
		 */
		protected virtual void AppendFileHeader (StringBuilder output)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.FileHeader.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendProjectInfo
		 * Appends the default project info in <configFileData> to the passed string.
		 */
		protected virtual void AppendProjectInfo (StringBuilder output)
			{
			string longYear = DateTime.Now.Year.ToString();
			string shortYear = longYear.Substring(2, 2);


			// Header

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ProjectInfoHeader.multiline") );
			output.AppendLine();


			// Defined values

			bool hasDefinedValues = false;
				
			if (configFileData.Title != null)
				{  
				output.AppendLine("Title: " + configFileData.Title);
				if (configFileData.Subtitle == null)
					{  output.AppendLine();  }
				hasDefinedValues = true;
				}
					
			if (configFileData.Subtitle != null)
				{
				output.AppendLine("Subtitle: " + configFileData.Subtitle);
				output.AppendLine();
				hasDefinedValues = true;
				}
					
			if (configFileData.Copyright != null)
				{
				output.AppendLine("Copyright: " + configFileData.Copyright);
				output.AppendLine();
				hasDefinedValues = true;
				}
			
			if (configFileData.TimestampCode != null)
				{  
				output.AppendLine("Timestamp: " + configFileData.TimestampCode);
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSubstitutions(shortYear, longYear).multiline",
																  shortYear, longYear) );
				output.AppendLine();
				hasDefinedValues = true;
				}

			if (configFileData.StyleName != null)
				{
				output.AppendLine("Style: " + configFileData.StyleName);
				output.AppendLine();
				hasDefinedValues = true;
				}

			if (hasDefinedValues)
				{  output.AppendLine();  }


			// Syntax reference

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ProjectInfoHeaderText.multiline") );

			if (configFileData.Title == null)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TitleSyntax.multiline") );
				}
					
			if (configFileData.Subtitle == null)
				{  
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SubtitleSyntax.multiline") );
				}
					
			if (configFileData.Copyright == null)
				{  
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.CopyrightSyntax.multiline") );
				}
			
			if (configFileData.TimestampCode == null)
				{  
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSyntax.multiline") );
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSubstitutions(shortYear, longYear).multiline",
																  shortYear, longYear) );
				}

			if (configFileData.StyleName == null)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.StyleSyntax.multiline") );
				}

			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendOverriddenProjectInfo
		 * Appends the overridden project info in the passed <OutputEntry> to the passed string.
		 */
		protected virtual void AppendOverriddenProjectInfo (OutputEntry entry, StringBuilder output)
			{
			if (entry.ProjectInfo.Title != null)
				{  output.AppendLine("   Title: " + entry.ProjectInfo.Title);  }
					
			if (entry.ProjectInfo.Subtitle != null)
				{  output.AppendLine("   Subtitle: " + entry.ProjectInfo.Subtitle);  }
					
			if (entry.ProjectInfo.Copyright != null)
				{  output.AppendLine("   Copyright: " + entry.ProjectInfo.Copyright);  }
			
			if (entry.ProjectInfo.TimeStampCode != null)
				{  output.AppendLine("   Timestamp: " + entry.ProjectInfo.TimeStampCode);  }

			if (entry.ProjectInfo.StyleName != null)
				{  output.AppendLine("   Style: " + entry.ProjectInfo.StyleName);   }
			}


		/* Function: AppendInputEntries
		 * Appends all input entries in <configFileData> to the passed StringBuilder.
		 */
		protected virtual void AppendInputEntries (StringBuilder output)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.InputHeader.multiline") );
			output.AppendLine();

			bool hasInputEntries = false;
				
			foreach (Entry entry in configFileData.Entries)
			    {
			    if (entry is InputEntry)
			        {
			        AppendEntry(entry, output);
			        output.AppendLine();
					  hasInputEntries = true;
			        }
			    }

			if (hasInputEntries)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.InputHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceFolderSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ImageFolderSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendFilterEntries
		 * Appends all filter entries in <configFileData> to the passed StringBuilder.
		 */
		protected virtual void AppendFilterEntries (StringBuilder output)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.FilterHeader.multiline") );
			output.AppendLine();

			bool hasFilterEntries = false;
				
			foreach (Entry entry in configFileData.Entries)
			    {
			    if (entry is FilterEntry)
			        {
			        AppendEntry(entry, output);
			        output.AppendLine();
					  hasFilterEntries = true;
			        }
			    }

			if (hasFilterEntries)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.FilterHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.IgnoreSourceFolderSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.IgnoreSourceFolderPatternSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendOutputEntries
		 * Appends all output entries in <configFileData> to the passed StringBuilder.
		 */
		protected virtual void AppendOutputEntries (StringBuilder output)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputHeader.multiline") );
			output.AppendLine();

			bool hasOutputEntries = false;
				
			foreach (Entry entry in configFileData.Entries)
			    {
			    if (entry is OutputEntry)
			        {
			        AppendEntry(entry, output);
			        output.AppendLine();
					  hasOutputEntries = true;
			        }
			    }

			if (hasOutputEntries)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.HTMLOutputFoldersSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.XMLOutputFoldersSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendGlobalSettings
		 * Appends the global properties in <configFileData> to the passed string.
		 */
		protected virtual void AppendGlobalSettings (StringBuilder output)
			{

			// Header

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.GlobalSettingsHeader.multiline") );
			output.AppendLine();


			// Defined values

			bool hasDefinedValues = false;
				
			if (configFileData.TabWidth != 0)
				{
				output.AppendLine("Tab Width: " + configFileData.TabWidth);
				output.AppendLine();
				hasDefinedValues = true;
				}

			if (hasDefinedValues)
				{  output.AppendLine();  }


			// Syntax reference

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.GlobalSettingsHeaderText.multiline") );

			if (configFileData.TabWidth == 0)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TabWidthSyntax.multiline") );
				}

			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendEntry
		 * Appends the passed <Entry> to the StringBuilder.
		 */
		protected virtual void AppendEntry (Entry entry, StringBuilder output)
			{
			if (entry is Entries.InputFolder)
				{  AppendInputFolderEntry(entry as Entries.InputFolder, output);  }
			else if (entry is Entries.HTMLOutputFolder)
				{  AppendHTMLOutputFolderEntry(entry as Entries.HTMLOutputFolder, output);  }
			else if (entry is Entries.XMLOutputFolder)
				{  AppendXMLOutputFolderEntry(entry as Entries.XMLOutputFolder, output);  }
			else if (entry is Entries.IgnoredSourceFolder)
				{  AppendIgnoredSourceFolderEntry(entry as Entries.IgnoredSourceFolder, output);  }
			else if (entry is Entries.IgnoredSourceFolderPattern)
				{  AppendIgnoredSourceFolderPatternEntry(entry as Entries.IgnoredSourceFolderPattern, output);  }
			else
				{  throw new Exception ("Can't append unrecognized entry type " + entry.GetType() + ".");  }
			}


		protected virtual void AppendInputFolderEntry (Entries.InputFolder entry, StringBuilder output)
			{
			if (entry.InputType == Files.InputType.Source)
				{  output.Append("Source");  }
			else if (entry.InputType == Files.InputType.Image)
				{  output.Append("Image");  }

			output.Append(" Folder");

			if (entry.Number > 1)
				{  output.Append(" " + entry.Number);  }

			output.Append(": ");

			Path relativePath = configFilePath.ParentFolder.MakeRelative(entry.Folder);
			output.AppendLine( (relativePath != null ? relativePath : entry.Folder) );

			if (entry.Name != null)
				{  output.AppendLine("   Name: " + entry.Name);  }
			}


		protected virtual void AppendHTMLOutputFolderEntry (Entries.HTMLOutputFolder entry, StringBuilder output)
			{
			output.Append("HTML Output Folder: ");

			Path relativePath = configFilePath.ParentFolder.MakeRelative(entry.Folder);
			output.AppendLine( (relativePath != null ? relativePath : entry.Folder) );

			AppendOverriddenProjectInfo(entry, output);
			}


		protected virtual void AppendXMLOutputFolderEntry (Entries.XMLOutputFolder entry, StringBuilder output)
			{
			output.Append("XML Output Folder: ");

			Path relativePath = configFilePath.ParentFolder.MakeRelative(entry.Folder);
			output.AppendLine( (relativePath != null ? relativePath : entry.Folder) );

			AppendOverriddenProjectInfo(entry, output);
			}


		protected virtual void AppendIgnoredSourceFolderEntry (Entries.IgnoredSourceFolder entry, StringBuilder output)
			{
			output.Append("Ignore Source Folder: ");

			Path relativePath = configFilePath.ParentFolder.MakeRelative(entry.Folder);
			output.AppendLine( (relativePath != null ? relativePath : entry.Folder) );
			}


		protected virtual void AppendIgnoredSourceFolderPatternEntry (Entries.IgnoredSourceFolderPattern entry, StringBuilder output)
			{
			output.AppendLine("Ignore Source Folder Pattern: " + entry.Pattern);
			}


		protected virtual void WriteBinaryEntry (Entry entry)
			{
			if (entry is Entries.InputFolder)
				{  WriteBinaryInputFolderEntry(entry as Entries.InputFolder);  }
			else if (entry is Entries.HTMLOutputFolder)
				{  WriteBinaryHTMLOutputFolderEntry(entry as Entries.HTMLOutputFolder);  }
			else if (entry is Entries.XMLOutputFolder)
				{  WriteBinaryXMLOutputFolderEntry(entry as Entries.XMLOutputFolder);  }
			else
				{  throw new Exception ("Can't write unrecognized entry type " + entry.GetType() + ".");  }
			}


		protected virtual void WriteBinaryInputFolderEntry (Entries.InputFolder entry)
			{
			if (entry.InputType == Files.InputType.Source)
				{
				// [String: Identifier="Source Folder"]
				// [String: Absolute Path]
				// [Int32: Number]
				
				binaryConfigFile.WriteString("Source Folder");
				binaryConfigFile.WriteString(entry.Folder);
				binaryConfigFile.WriteInt32(entry.Number);
				}
				
			else // (entry.InputType == Files.InputType.Image)
				{
				// [String: Identifier="Image Folder"]
				// [String: Absolute Path]
				// [String: Number]
				
				binaryConfigFile.WriteString("Image Folder");
				binaryConfigFile.WriteString(entry.Folder);
				binaryConfigFile.WriteInt32(entry.Number);
				}
			}


		protected virtual void WriteBinaryHTMLOutputFolderEntry (Entries.HTMLOutputFolder entry)
			{
			// [String: Identifier="HTML Output Folder"]
			// [String: Absolute Path]
			// [Int32: Number]

			binaryConfigFile.WriteString("HTML Output Folder");
			binaryConfigFile.WriteString(entry.Folder);
			binaryConfigFile.WriteInt32(entry.Number);
			}


		protected virtual void WriteBinaryXMLOutputFolderEntry (Entries.XMLOutputFolder entry)
			{
			// [String: Identifier="XML Output Folder"]
			// [String: Absolute Path]
			// [Int32: Number]

			binaryConfigFile.WriteString("XML Output Folder");
			binaryConfigFile.WriteString(entry.Folder);
			binaryConfigFile.WriteInt32(entry.Number);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ParsedData
		 * The <ConfigData> extracted from the file.
		 */
		public ConfigData ParsedData
			{
			get
				{  return configFileData;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		
		/* var: configFile
		 * The <ConfigFile> being parsed if we're loading <Project.txt>.
		 */
		protected ConfigFile configFile;

		/* var: binaryConfigFile
		 * The <BinaryFile> being loaded or saved.
		 */
		protected BinaryFile binaryConfigFile;

		/* var: configFilePath
		 * The <Path> of the file being loaded or saved, which can either be <configFile> or <binaryConfigFile>.
		 */
		protected Path configFilePath;

		/* var: configFileData
		 * A <ConfigData> representation of the file being loaded or saved.
		 */
		protected ConfigData configFileData;


		Regex.Config.Subtitle subtitleRegex;
		Regex.Config.Timestamp timestampRegex;
		Regex.Config.TabWidth tabWidthRegex;

		Regex.Config.SourceFolder sourceFolderRegex;
		Regex.Config.ImageFolder imageFolderRegex;
		Regex.Config.HTMLOutputFolder htmlOutputFolderRegex;
		Regex.Config.XMLOutputFolder xmlOutputFolderRegex;
		Regex.Config.IgnoredSourceFolder ignoredSourceFolderRegex;
		Regex.Config.IgnoredSourceFolderPattern ignoredSourceFolderPatternRegex;

		}
	}