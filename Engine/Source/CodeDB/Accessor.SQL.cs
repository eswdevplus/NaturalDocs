﻿/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Accessor
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public partial class Accessor
		{
		
		// Group: Topic Functions
		// __________________________________________________________________________
		
		
		/* Function: GetTopicsInFile
		 * 
		 * Retrieves a list of all the topics present in the passed file ID.  The list will be in comment line number order.
		 * If there are none it will return an empty list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process,
		 * or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsInFile (int fileID, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			List<Topic> topics = new List<Topic>();
			
			using (SQLite.Query query = connection.Query("SELECT TopicID, LanguageID, CommentLineNumber, CodeLineNumber, Title, " +
																					 " Body, Symbol, EndingSymbolID, TopicTypeID, AccessLevel, Tags " +
																		   "FROM Topics WHERE FileID = ? " +
																		   "ORDER BY CommentLineNumber ASC", fileID))
				{
				while (query.Step() && !cancelled())
					{
					Topic topic = new Topic();
					
					topic.TopicID = query.IntColumn(0);
					topic.LanguageID = query.IntColumn(1);
					topic.CommentLineNumber = query.IntColumn(2);
					topic.CodeLineNumber = query.IntColumn(3);
					topic.Title = query.StringColumn(4);
					topic.Body = query.StringColumn(5);
					topic.Symbol = Symbol.FromSymbolString( query.StringColumn(6) );
					topic.EndingSymbolID = query.IntColumn(7);
					topic.TopicTypeID = query.IntColumn(8);
					topic.AccessLevel = (Languages.AccessLevel)query.IntColumn(9);
					topic.TagString = query.StringColumn(10);

					topic.FileID = fileID;
					
					topics.Add(topic);
					}
				}
			
			return topics;
			}
			
			
		/* Function: AddTopic
		 * 
		 * Adds a <Topic> to the database.  Assumes it doesn't exist; if it does, you need to use <UpdateTopic()> or call <DeleteTopic()> 
		 * first.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 * 
		 * Topic Requirements:
		 * 
		 *		TopicID - Must be zero.  This will be automatically assigned and the <Topic> updated.
		 *		FileID - Must be set.
		 *		LanguageID - Must be set.
		 *		CommentLineNumber - Must be set.  If CodeLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		CodeLineNumber - Must be set.  If CommentLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		Title - Must be set.
		 *		Body - Can be null.
		 *		Symbol - Must be set.
		 *		EndingSymbolID - Must be zero.  This will be automatically assigned and the <Topic> updated.
		 *		TopicTypeID - Must be set.
		 *		AccessLevel - Optional.  <Topic> gives it a default value if not set.
		 *		TagString - Can be null.
		 */
		public void AddTopic (Topic topic)
			{
			RequireAtLeast(LockType.ReadWrite);

			StartTopicValidation(topic, "AddTopic");
				RequireZero("TopicID", topic.TopicID);
				RequireNonZero("FileID", topic.FileID);
				RequireNonZero("LanguageID", topic.LanguageID);
				RequireNonZero("CommentLineNumber", topic.CommentLineNumber);
				RequireNonZero("CodeLineNumber", topic.CodeLineNumber);
				RequireContent("Title", topic.Title);
				// Body
				RequireContent("Symbol", topic.Symbol);			
				RequireZero("EndingSymbolID", topic.EndingSymbolID);
				RequireNonZero("TopicTypeID", topic.TopicTypeID);
				// AccessLevel
				// TagString
			EndTopicValidation();
			
			topic.TopicID = Engine.Instance.CodeDB.UsedTopicIDs.LowestAvailable;
			Symbol endingSymbol = topic.Symbol.LastSegment;
			
			using (SQLite.Query query = connection.Query("SELECT EndingSymbolID FROM EndingSymbols " +
																		   "WHERE EndingSymbol = ?", endingSymbol))
				{
				if (query.Step())
					{  
					topic.EndingSymbolID = query.IntColumn(0);  
					
					// It's possible for a topic to retain its ending symbol while otherwise changing too dramatically to use 
					// UpdateTopic().  Since the topic would need to be deleted and readded, it's a worthwhile optimization to remove
					// the ending symbol ID from the list to check in case it's there.
					Engine.Instance.CodeDB.EndingSymbolIDsToCheckForDeletion.Remove(topic.EndingSymbolID);
					}
				}
				
			if (topic.EndingSymbolID == 0)
				{
				topic.EndingSymbolID = Engine.Instance.CodeDB.UsedEndingSymbolIDs.LowestAvailable;
				
				connection.Execute("INSERT INTO EndingSymbols (EndingSymbolID, EndingSymbol) VALUES (?, ?)", 
											topic.EndingSymbolID, endingSymbol);
											
				Engine.Instance.CodeDB.UsedEndingSymbolIDs.Add(topic.EndingSymbolID);
				}
				
			connection.Execute("INSERT INTO Topics (TopicID, FileID, LanguageID, CommentLineNumber, CodeLineNumber, " +
																	" Title, Body, Symbol, EndingSymbolID, TopicTypeID, AccessLevel, Tags) " +
																	" VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
										topic.TopicID, topic.FileID, topic.LanguageID, topic.CommentLineNumber, topic.CodeLineNumber,
										topic.Title, topic.Body, topic.Symbol, topic.EndingSymbolID, topic.TopicTypeID,
										(int)topic.AccessLevel, topic.TagString);
			
			Engine.Instance.CodeDB.UsedTopicIDs.Add(topic.TopicID);
			
			
			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);
					
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnAddTopic(topic, eventAccessor);  }
					}
				}
			finally
				{
				Engine.Instance.CodeDB.ReleaseChangeWatchers();
				}
			}


		/* Function: UpdateTopic
		 * 
		 * Performs minor updates to an existing <Topic> in the database.  If a topic has changed more substantially than the parameters
		 * allow, you cannot use this function.  You must delete the old topic and add it as a new one instead.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 *		
		 * Parameter Requirements:
		 * 
		 *		Topic - The topic must have been retrieved from the database and thus have all its fields set.  The body and line numbers
		 *				   will be replaced in the object with the passed values.
		 *		CommentLineNumber - Must be set.  If you read it from a <Topic> it will automatically return CodeLineNumber if it isn't set.
		 *		CodeLineNumber - Must be set.  If you read it from a <Topic> it will automatically return CommentLineNumber if it isn't set.
		 *		Body - Can be null.
		 */
		public void UpdateTopic (Topic topic, int newCommentLineNumber, int newCodeLineNumber, string newBody)
			{
			RequireAtLeast(LockType.ReadWrite);

			connection.Execute("UPDATE Topics SET CommentLineNumber=?, CodeLineNumber=?, Body=? WHERE TopicID = ?",
										 newCommentLineNumber, newCodeLineNumber, newBody, topic.TopicID);
			// Don't update the fields yet since the change notification requires the old one.

			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);
					
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnUpdateTopic(topic, newCommentLineNumber, newCodeLineNumber, newBody, eventAccessor);  }
					}
				}
			finally
				{
				Engine.Instance.CodeDB.ReleaseChangeWatchers();
				}
				
			topic.CommentLineNumber = newCommentLineNumber;
			topic.CodeLineNumber = newCodeLineNumber;
			topic.Body = newBody;
			}
			
			
		/* Function: DeleteTopic
		 * 
		 * Removes a topic from the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		- The topic must have been retrieved from the database, and thus have all its fields set.
		 */
		public void DeleteTopic (Topic topic)
			{
			RequireAtLeast(LockType.ReadWrite);

			StartTopicValidation(topic, "DeleteTopic");
				RequireNonZero("TopicID", topic.TopicID);
				// FileID
				// LanguageID
				// CommentLineNumber
				// CodeLineNumber
				// Title
				// Body
				// Symbol
				RequireNonZero("EndingSymbolID", topic.EndingSymbolID);
				// TopicTypeID
				// AccessLevel
				// TagString
			EndTopicValidation();

			connection.Execute("DELETE FROM Topics WHERE TopicID = ?", topic.TopicID);
			Engine.Instance.CodeDB.UsedTopicIDs.Remove(topic.TopicID);
			
			Engine.Instance.CodeDB.EndingSymbolIDsToCheckForDeletion.Add(topic.EndingSymbolID);

			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);
					
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnDeleteTopic(topic, eventAccessor);  }
					}
				}
			finally
				{
				Engine.Instance.CodeDB.ReleaseChangeWatchers();
				}
			}
			
			
		/* Function: UpdateTopicsInFile
		 * 
		 * Replaces all the topics in the database under the passed file ID with the passed list.  It will query the existing topics itself,
		 * perform a comparison, and call <AddTopic()>, <UpdateTopic()>, and <DeleteTopic()> as necessary.  Pass a <CancelDelegate>
		 * if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If any changes occur, it will be upgraded automatically.
		 * 
		 * Topic Requirements:
		 * 
		 *		TopicID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 *		FileID - Must match the parameter.
		 *		LanguageID - Must be set.
		 *		CommentLineNumber - Must be set.  If CodeLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		CodeLineNumber - Must be set.  If CommentLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		Title - Must be set.
		 *		Body - Can be null.
		 *		Symbol - Must be set.
		 *		EndingSymbolID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 *		TopicTypeID - Must be set.
		 *		AccessLevel - Optional.  <Topic> gives it a default value if not set.
		 *		TagString - Can be null.
		 */
		public void UpdateTopicsInFile (int fileID, List<Topic> newTopics, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			foreach (Topic newTopic in newTopics)
				{
				if (newTopic.FileID != fileID)
					{  throw new Exception ("Can't update topics in file if the file IDs don't match.");  }
				// We'll leave the rest of the topic field validation to AddTopic(), DeleteTopic(), and UpdateTopic().
				}
			
			List<Topic> oldTopics = GetTopicsInFile(fileID, cancelled);
			bool madeChanges = false;
			
			try
				{
				
				foreach (Topic newTopic in newTopics)
					{
					if (cancelled())
						{  break;  }
						
					bool foundMatch = false;
					for (int i = 0; foundMatch == false && i < oldTopics.Count; i++)
						{
						Topic.DatabaseCompareResult result = newTopic.DatabaseCompare(oldTopics[i]);
						
						if (result == Topic.DatabaseCompareResult.Equal)
							{
							foundMatch = true;
							newTopic.TopicID = oldTopics[i].TopicID;
							newTopic.EndingSymbolID = oldTopics[i].EndingSymbolID;
							oldTopics.RemoveAt(i);
							}
						else if (result == Topic.DatabaseCompareResult.EqualExceptLineNumbersAndBody)
							{
							if (madeChanges == false)
								{
								RequireAtLeast(LockType.ReadWrite);
								BeginTransaction();
								madeChanges = true;
								}
								
							foundMatch = true;
							UpdateTopic(oldTopics[i], newTopic.CommentLineNumber, newTopic.CodeLineNumber, newTopic.Body);
							newTopic.TopicID = oldTopics[i].TopicID;
							newTopic.EndingSymbolID = oldTopics[i].EndingSymbolID;
							oldTopics.RemoveAt(i);
							}
						}
						
					if (foundMatch == false)
						{
						if (madeChanges == false)
							{
							RequireAtLeast(LockType.ReadWrite);
							BeginTransaction();
							madeChanges = true;
							}
							
						AddTopic(newTopic);
						}
					}
					
				// All matches would have been removed, so anything left in oldTopics was deleted.
				if (oldTopics.Count > 0 && !cancelled())
					{
					if (madeChanges == false)
						{
						RequireAtLeast(LockType.ReadWrite);
						BeginTransaction();
						madeChanges = true;
						}
						
					foreach (Topic oldTopic in oldTopics)
						{  DeleteTopic(oldTopic);  }
					}
					
				if (madeChanges == true)
					{
					CommitTransaction();
					}
				}
			catch
				{
				if (madeChanges == true && inTransaction)
					{  RollbackTransactionForException();  }
					
				throw;
				}
			}
			
			
		/* Function: DeleteTopicsInFile
		 * 
		 * Deletes all the topics in the database under the passed file ID.  Pass a <CancelDelegate> if you'd like to be able to
		 * interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If any deletions occur, it will be upgraded automatically.
		 */
		public void DeleteTopicsInFile (int fileID, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);
			
			List<Topic> topics = GetTopicsInFile(fileID, cancelled);
			
			if (topics.Count > 0 && !cancelled())
				{
				RequireAtLeast(LockType.ReadWrite);
				BeginTransaction();
					
				foreach (Topic topic in topics)
					{  
					DeleteTopic(topic);
					
					if (cancelled())
						{  break;  }
					}

				CommitTransaction();
				}
			}



		// Group: Transaction Functions
		// __________________________________________________________________________
		
		
		/* Function: BeginTransaction
		 * 
		 * Starts a new transaction.  Note that transactions MUST BE COMMITTED except in error conditions like exceptions.
		 * There are other changes to Natural Docs' state that occur with each change independently of transactions so they
		 * cannot be rolled back with the database.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void BeginTransaction ()
			{
			RequireAtLeast(LockType.ReadWrite);
			
			if (inTransaction)
				{  throw new Exception("Tried to create a transaction when one was already in effect.");  }
				
			connection.Execute("BEGIN IMMEDIATE TRANSACTION");
			inTransaction = true;
			}
			
		/* Function: CommitTransaction
		 * 
		 * Commits an existing transaction to the database.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void CommitTransaction ()
			{
			RequireAtLeast(LockType.ReadWrite);
			
			if (!inTransaction)
				{  throw new Exception("Tried to commit a transaction when one was not in effect.");  } 
			
			connection.Execute("COMMIT TRANSACTION");
			inTransaction = false;
			}

		/* Function: RollbackTransactionForException
		 * 
		 * Rolls back an existing transaction from the database because an exception occurred.  This is the ONLY reason
		 * you can roll back a transaction because there are other state changes within Natural Docs that occur and would
		 * not be reverted with the database.  This function only exists so that you can get out of the transaction if an
		 * exception occurs, which prevents an additional exception from occurring if you try to dispose of the Accessor
		 * while a transaction is still in effect.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void RollbackTransactionForException ()
			{
			if (inTransaction)
				{  
				connection.Execute("ROLLBACK TRANSACTION");
				inTransaction = false;
				}
			}

		}
	}