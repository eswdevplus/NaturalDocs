﻿/* 
 * Class: GregValure.NaturalDocs.Engine.SQLite.Query
 * ____________________________________________________________________________
 * 
 * An object representing a SQLite query.
 * 
 * 
 * Topic: Usage
 * 
 *		- These objects should not be created directly.  Use <SQLite.Connection> functions instead which will return
 *		  them.
 *		
 *		- Once a statement is ready, call <Step()> to get the first row, if any.  Use functions like <IntColumn()> and
 *		  <StringColumn()> to get the values from the row. Continue calling <Step()> until you run out of rows or 
 *		  don't need any more.
 *		  
 *		- If desired, you can call <Reset()> at any time to start over from the beginning.  You can also have it clear
 *		  the bindings so you can call <BindValues()> to run it again with new values.
 *		  
 *		- <Dispose()> of the object when you are done with it.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		The underlying database is thread safe, but <Connection> and Query objects are not.  Each thread needs to have
 *		its own <Connection> object and use only Queries created by that object.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

using System;


namespace GregValure.NaturalDocs.Engine.SQLite
	{
	public class Query : IDisposable
		{
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Query
		 * Query objects should be created by <SQLite.Connection>, not directly.
		 */
		public Query ()
			{
			statementHandle = IntPtr.Zero;
			connectionHandle = IntPtr.Zero;
			} 
			
		/* Destructor: ~Query
		 */
		~Query ()
			{
			Dispose(true);
			}
			
			
		/* Function: Prepare
		 * Prepares a SQL statement for execution.  If values are specified, <BindValues()> is called on them.  This
		 * function should only be called by <SQLite.Connection>.
		 */
		public void Prepare (IntPtr newConnectionHandle, string statement, params Object[] values)
			{
			if (statementHandle != IntPtr.Zero)
				{  throw new Exception("Tried to prepare a query when one already existed.");  }

			connectionHandle = newConnectionHandle;

			API.Result result = API.PrepareV2 (connectionHandle, statement, out statementHandle);
			
			if (result != API.Result.OK)
				{
				statementHandle = IntPtr.Zero;
				connectionHandle = IntPtr.Zero;
				
				throw new Exceptions.UnexpectedResult("Could not prepare query.", result);
				}
				
			if (values.Length != 0)
				{  BindValues(values);  }
			}
			
			
		/* Function: BindValues
		 * 
		 * Binds values to any question marks in the SQL statement.  Any values that aren't integers, strings, or 
		 * doubles will have ToString called on them.
		 * 
		 * This function is called automatically by <Prepare()> so it usually won't need to be called manually.  
		 * However, if <Reset()> is called this may be called again to bind new values to the statement.
		 */
		public void BindValues (params Object[] values)
			{
			int index = 1;  // Indexes start at 1 for these API functions for some reason.
			API.Result result;
			
			foreach (Object value in values)
				{
				if (value == null)
					{  result = API.BindNull(statementHandle, index);  }
				else if (value is int)
					{  result = API.BindInt(statementHandle, index, (int)value);  }
				else if (value is string)
					{  result = API.BindText(statementHandle, index, (string)value);  }  
				else if (value is double)
					{  result = API.BindDouble(statementHandle, index, (double)value);  }
				else
					{  result = API.BindText(statementHandle, index, value.ToString());  }
					
				if (result != API.Result.OK)
					{  throw new Exceptions.UnexpectedResult("Could not bind value to statement", result);  }
					
				index++;
				}
			}
			
			
		/* Function: Step
		 * Executes the statement until it returns a row or completes.  Returns true if it returns a row or false if
		 * there are no more.
		 */
		public bool Step ()
			{
			API.Result result = API.Step(statementHandle);
			
			if (result == API.Result.Row)
				{  return true;  }
			else if (result == API.Result.Done)
				{  return false;  }
			else
				{  throw new Exceptions.UnexpectedResult("Could not step query.", result);  }
			}
			
			
		/* Function: IntColumn
		 * Returns the value of an integer column in a row found by <Step()>.  The column indexes start at zero.
		 */
		public int IntColumn (int columnIndex)
			{  return API.ColumnInt(statementHandle, columnIndex);  }
			
		/* Function: StringColumn
		 * Returns the value of a string column in a row found by <Step()>.  The column indexes start at zero.
		 */
		public string StringColumn (int columnIndex)
			{  return API.ColumnText(statementHandle, columnIndex);  }
			
		/* Function: DoubleColumn
		 * Returns the value of a double (floating point) column in a row found by <Step()>.  The column indexes 
		 * start at zero.
		 */
		public double DoubleColumn (int columnIndex)
			{  return API.ColumnDouble(statementHandle, columnIndex);  }
			
			
		/* Function: Reset
		 * Restarts the query from the beginning.  Can optionally clear its bindings.
		 */
		public void Reset (bool clearBindings)
			{
			API.Result result = API.Reset(statementHandle);
			
			if (result != API.Result.OK)
				{  throw new Exceptions.UnexpectedResult("Could not reset the query.", result);  }
				
			if (clearBindings == true)
				{
				result = API.ClearBindings(statementHandle);
				
				if (result != API.Result.OK)
					{  throw new Exceptions.UnexpectedResult("Could not clear statement bindings.", result);  }
				}
			}
		
		
		
		// Group: IDisposable Functions
		// __________________________________________________________________________
		
		
		public void Dispose ()
			{
			Dispose(false);
			}
			
		protected void Dispose (bool strictRulesApply)
			{
			if (statementHandle != IntPtr.Zero)
				{
				API.Result finalizeResult = API.Finalize (statementHandle);
				statementHandle = IntPtr.Zero;
				connectionHandle = IntPtr.Zero;
				
				if (strictRulesApply == false && finalizeResult != API.Result.OK)
					{  throw new Exceptions.UnexpectedResult("Could not close SQL statement.", finalizeResult);  }
				}	
			}
		
			
			
	
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* Handle: statementHandle
		 * A handle to the prepared SQLite statement.
		 */
		protected IntPtr statementHandle; 
		
		/* Handle: connectionHandle
		 * A handle to the SQLite database connection.  This is just a reference, the <SQLite.Connection> object is
		 * responsible for disposing of it.
		 */
		protected IntPtr connectionHandle;
		}
	}