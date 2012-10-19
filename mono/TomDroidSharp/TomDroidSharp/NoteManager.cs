/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009, 2010, 2011 Olivier Bilodeau <olivier@bottomlesspit.org>
 * Copyright 2009, 2010 Benoit Garret <benoit.garret_launchpad@gadz.org>
 * Copyright 2011 Stefan Hammer <j.4@gmx.at>
 * 
 * This file is part of Tomdroid.
 * 
 * Tomdroid is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Tomdroid is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Tomdroid.  If not, see <http://www.gnu.org/licenses/>.
 */

using Android.App;
using Android.Content;
using Android.Database;
using Android.Net;
using Android.Text;
using Android.Text.Format;
using Android.Widget;

//import java.util.List;
//import java.util.Collections;
//import java.util.regex.Matcher;
//import java.util.regex.Pattern;
using TomDroidSharp.ui;
using TomDroidSharp.util;
using Java.Util.Regex;
using System.Collections.Generic;
using System.Text;

namespace TomDroidSharp
{
	//@SuppressWarnings("deprecation")
	public class NoteManager {
		
		public static readonly string[] FULL_PROJECTION = { Note.ID, Note.TITLE, Note.FILE, Note.NOTE_CONTENT, Note.MODIFIED_DATE, Note.GUID, Note.TAGS };
		public static readonly string[] LIST_PROJECTION = { Note.ID, Note.TITLE, Note.MODIFIED_DATE, Note.TAGS };
		public static readonly string[] DATE_PROJECTION = { Note.ID, Note.GUID, Note.MODIFIED_DATE };
		public static readonly string[] TITLE_PROJECTION = { Note.TITLE, Note.GUID };
		public static readonly string[] GUID_PROJECTION = { Note.ID, Note.GUID };
		public static readonly string[] ID_PROJECTION = { Note.ID };
		public static readonly string[] EMPTY_PROJECTION = {};
		
		// static properties
		private static readonly string TAG = "NoteManager";
	 	
		private static string sortOrder;

		// column name for sortOrder
		private static string sortOrderBy;

		public static void setSortOrder(string orderBy) {
			sortOrderBy = orderBy;
			if(orderBy.Equals("sort_title")) {
				sortOrder = Note.TITLE + " ASC";
			} else {
				sortOrder = Note.MODIFIED_DATE + " DESC";
			}
		}

		public static string getSortOrder() {
			return sortOrderBy;
		}

		// gets a note from the content provider, based on guid
		public static Note getNoteByGuid(Activity activity, string guid) {

			Uri notes = Tomdroid.CONTENT_URI;
			
			string[] whereArgs = new string[1];
			whereArgs[0] = guid;
			
			// The note identifier is the guid
			ContentResolver cr = activity.ContentResolver;
			ICursor cursor = cr.Query(notes,
	                FULL_PROJECTION,  
	                Note.GUID + "= ?",
	                whereArgs,
	                null);
			activity.StartManagingCursor(cursor);
			if (cursor == null || cursor.Count == 0) {
				cursor.Close();
				return null;
			}
			else {
				cursor.MoveToFirst();
				string noteContent = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.NOTE_CONTENT));
				string noteTitle = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TITLE));
				string noteChangeDate = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.MODIFIED_DATE));
				string noteTags = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TAGS));
				string noteGUID = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.GUID));
				int noteDbid = cursor.GetInt(cursor.GetColumnIndexOrThrow(Note.ID));
				
				Note note = new Note();
				note.setTitle(noteTitle);
				note.setXmlContent(stripTitleFromContent(noteContent, noteTitle));
				note.setLastChangeDate(noteChangeDate);
				note.addTag(noteTags);
				note.setGuid(noteGUID);
				note.setDbId(noteDbid);
				cursor.Close();
				return note;
			}
		}
		
		// gets a note from the content provider
		public static Note getNote(Activity activity, Uri uri) {
			
			Note note = null;
			
			// can we find a matching note?
			ICursor cursor = activity.ManagedQuery(uri, FULL_PROJECTION, null, null, null);
			// cursor must not be null and must return more than 0 entry 
			if (!(cursor == null || cursor.Count == 0)) {
				
				// create the note from the cursor
				cursor.MoveToFirst();
				string noteContent = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.NOTE_CONTENT));
				string noteTitle = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TITLE));
				string noteChangeDate = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.MODIFIED_DATE));
				string noteTags = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TAGS));
				string noteGUID = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.GUID));
				int noteDbid = cursor.GetInt(cursor.GetColumnIndexOrThrow(Note.ID));
				
				note = new Note();
				note.setTitle(noteTitle);
				note.setXmlContent(stripTitleFromContent(noteContent, noteTitle));
				note.setLastChangeDate(noteChangeDate);
				note.setTags(noteTags);
				note.setGuid(noteGUID);
				note.setDbId(noteDbid);
			}
			cursor.Close();
			return note;
		}

		// check in a note Exists in the content provider
		public static bool noteExists(Activity activity, string guid) {
			Uri notes = Tomdroid.CONTENT_URI;
			
			string[] whereArgs = new string[1];
			whereArgs[0] = guid;

			// The note identifier is the guid
			ContentResolver cr = activity.ContentResolver;
			ICursor cursor = cr.Query(notes,
	                ID_PROJECTION,  
	                Note.GUID + "= ?",
	                whereArgs,
	                null);
			activity.StartManagingCursor(cursor);
			return (cursor != null && cursor.Count != 0);
		}
		
		// puts a note in the content provider
		// return uri
		public static Uri putNote(Activity activity, Note note) {
			
			// verify if the note is already in the content provider
			
			// TODO make the Query prettier (use querybuilder)
			Uri notes = Tomdroid.CONTENT_URI;
			string[] whereArgs = new string[1];
			whereArgs[0] = note.getGuid();
			
			// The note identifier is the guid
			ContentResolver cr = activity.ContentResolver;
			ICursor managedCursor = cr.Query(notes,
	                LIST_PROJECTION,  
	                Note.GUID + "= ?",
	                whereArgs,
	                null);
			activity.StartManagingCursor(managedCursor);

			string title = note.getTitle();
			string xmlContent = note.getXmlContent();
			string plainContent = stringConverter.encode(Html.FromHtml(title + "\n" + xmlContent).ToString());
			
			// Preparing the values to be either inserted or updated
			// depending on the result of the previous Query
			ContentValues values = new ContentValues();
			values.Put(Note.TITLE, title);
			values.Put(Note.FILE, note.getFileName());
			values.Put(Note.GUID, note.getGuid().ToString());
			// Notice that we store the date in UTC because sqlite doesn't handle RFC3339 timezone information
			values.Put(Note.MODIFIED_DATE, note.getLastChangeDate().Format3339(false));
			values.Put(Note.NOTE_CONTENT, xmlContent);
			values.Put(Note.NOTE_CONTENT_PLAIN, plainContent);
			values.Put(Note.TAGS, note.getTags());
			
			Uri uri = null;
			
			if (managedCursor == null || managedCursor.Count == 0) {

				// This note is not in the database yet we need to insert it
				TLog.v(TAG, "A new note has been detected (not yet in db)");
				
	    		uri = cr.Insert(Tomdroid.CONTENT_URI, values);

	    		TLog.v(TAG, "Note inserted in content provider. ID: {0} TITLE:{1} GUID:{2}", uri, note.getTitle(),
	                    note.getGuid());
			} else {

				TLog.v(TAG, "A local note has been detected (already in db)");

				cr.Update(Tomdroid.CONTENT_URI, values, Note.GUID+" = ?", whereArgs); 

				uri = Uri.Parse(Tomdroid.CONTENT_URI+"/"+getNoteIdByGUID(activity, note.getGuid()));

				TLog.v(TAG, "Note updated in content provider: TITLE:{0} GUID:{1} TAGS:{2}", note.getTitle(), note.getGuid(), note.getTags());
			}
			managedCursor.Close();
			note = getNote(activity, uri);
			return uri;
		}

		// this function removes a "deleted" tag
		public static void undeleteNote(Activity activity, Note note)
		{
			note.removeTag("system:deleted");
			Time now = new Time();
			now.SetToNow();
			note.setLastChangeDate(now);
			putNote(activity,note);
		}
		
		// this function just adds a "deleted" tag, to allow remote delete when syncing
		public static void deleteNote(Activity activity, Note note)
		{
			note.addTag("system:deleted");
			Time now = new Time();
			now.SetToNow();
			note.setLastChangeDate(now);
			putNote(activity,note);
		}
		public static void deleteNote(Activity activity, string guid)
		{
			Note note = getNoteByGuid(activity,guid);
			deleteNote(activity, note);
		}
		
		// this function actually deletes the note locally, called when syncing
		public static bool deleteNote(Activity activity, int id)
		{
			Uri uri = Uri.Parse(Tomdroid.CONTENT_URI+"/"+id);

			ContentResolver cr = activity.ContentResolver;
			int result = cr.Delete(uri, null, null);
			
			if(result > 0) {
				return true;
			}
			else 
				return false;
		}

		// this function deletes deleted notes - if they never existed on the server, we still delete them at sync

		public static void purgeDeletedNotes(Activity activity)
		{
			// get a cursor representing all deleted notes from the NoteProvider
			Uri notes = Tomdroid.CONTENT_URI;
			string where = Note.TAGS + " LIKE '%system:deleted%'";
			ContentResolver cr = activity.ContentResolver;
			int rows = cr.Delete(notes, where, null);
			TLog.v(TAG, "Deleted {0} local notes based on system:deleted tag",rows);
		}

		// this function deletes all notes - called from preferences

		public static void deleteAllNotes(Activity activity)
		{
			// get a cursor representing all deleted notes from the NoteProvider
			Uri notes = Tomdroid.CONTENT_URI;
			ContentResolver cr = activity.ContentResolver;
			int rows = cr.Delete(notes, null, null);
			TLog.v(TAG, "Deleted {0} local notes",rows);
		}

		public static ICursor getAllNotes(Activity activity, bool includeNotebookTemplates) {
			// get a cursor representing all notes from the NoteProvider
			Uri notes = Tomdroid.CONTENT_URI;
			string where = "("+Note.TAGS + " NOT LIKE '%" + "system:deleted" + "%')";
			if (!includeNotebookTemplates) {
				where += " AND (" + Note.TAGS + " NOT LIKE '%" + "system:template" + "%')";
			}
			return activity.ManagedQuery(notes, LIST_PROJECTION, where, null, sortOrder);		
		}

		// this function gets all non-deleted notes as notes in an array
		
		public static Note[] getAllNotesAsNotes(Activity activity, bool includeNotebookTemplates) {
			Uri uri = Tomdroid.CONTENT_URI;
			string where = "("+Note.TAGS + " NOT LIKE '%" + "system:deleted" + "%')";
			string orderBy;
			if (!includeNotebookTemplates) {
				where += " AND (" + Note.TAGS + " NOT LIKE '%" + "system:template" + "%')";
			}
			orderBy = Note.MODIFIED_DATE + " DESC";
			ICursor cursor = activity.ManagedQuery(uri, FULL_PROJECTION, where, null, orderBy);
			if (cursor == null || cursor.Count == 0) {
				TLog.d(TAG, "no notes in cursor");
				return null;
			}
			TLog.d(TAG, "{0} notes in cursor",cursor.Count);
			Note[] notes = new Note[cursor.Count];
			cursor.MoveToFirst();
			int key = 0;

			while(!cursor.IsAfterLast) {
				string noteContent = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.NOTE_CONTENT));
				string noteTitle = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TITLE));
				string noteChangeDate = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.MODIFIED_DATE));
				string noteTags = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TAGS));
				string noteGUID = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.GUID));
				int noteDbid = cursor.GetInt(cursor.GetColumnIndexOrThrow(Note.ID));
				
				Note note = new Note();
				note.setTitle(noteTitle);
				note.setXmlContent(stripTitleFromContent(noteContent, noteTitle));
				note.setLastChangeDate(noteChangeDate);
				note.addTag(noteTags);
				note.setGuid(noteGUID);
				note.setDbId(noteDbid);
				notes[key++] = note;
				cursor.MoveToNext();
			}
			cursor.Close();
			return notes;
		}	

		public static IListAdapter getListAdapter(Activity activity, string querys, int selectedIndex) {
			
			bool includeNotebookTemplates = Preferences.GetBoolean(Preferences.Key.INCLUDE_NOTE_TEMPLATES);
			bool includeDeletedNotes = Preferences.GetBoolean(Preferences.Key.INCLUDE_DELETED_NOTES);
			
			int optionalQueries = 0;
			if(!includeNotebookTemplates)
				optionalQueries++;
			if(!includeDeletedNotes)
				optionalQueries++;
			
			string[] qargs = null;
			string where = "";
			int count = 0;
			
			if (querys != null ) {
				// sql statements to search notes
				string[] Query = querys.Split(" ");
				qargs = new string[Query.Length+optionalQueries];
				foreach (string str in Query) {
					qargs[count++] = "%"+stringConverter.encode(str)+"%"; 
					where = where + (where.Length > 0? " AND ":"")+"("+Note.NOTE_CONTENT_PLAIN+" LIKE ?)";
				}	
			}
			else
				qargs = new string[optionalQueries];

			if (!includeDeletedNotes) {
				where += (where.Length > 0? " AND ":"")+"(" + Note.TAGS + " NOT LIKE ?)";
				qargs[count++] = "%system:deleted%";
			}
			if (!includeNotebookTemplates) {
				where += (where.Length > 0? " AND ":"")+"(" + Note.TAGS + " NOT LIKE ?)";
				qargs[count++] = "%system:template%";
			}

			// get a cursor representing all notes from the NoteProvider
			Uri notes = Tomdroid.CONTENT_URI;

			ContentResolver cr = activity.ContentResolver;
			ICursor notesCursor = cr.Query(notes,
					LIST_PROJECTION,  
					where,
					qargs,
					sortOrder);
			activity.StartManagingCursor(notesCursor);

			// set up an adapter binding the TITLE field of the cursor to the list item
			string[] from = new string[] { Note.TITLE };
			int[] to = new int[] { Resource.Id.note_title };
			return new NoteListCursorAdapter(activity, Resource.layout.main_list_item, notesCursor, from, to, selectedIndex);
		}
		
		public static IListAdapter getListAdapter(Activity activity, int selectedIndex) {
			
			return getListAdapter(activity, null, selectedIndex);
		}
		public static IListAdapter getListAdapter(Activity activity, string querys) {
			
			return getListAdapter(activity, querys, -1);
		}
		public static IListAdapter getListAdapter(Activity activity) {
			
			return getListAdapter(activity, null, -1);
		}

		// gets the titles of the notes present in the db, used in ViewNote.buildLinkifyPattern()
		public static ICursor getTitles(Activity activity) {
			
			string where = Note.TAGS + " NOT LIKE '%system:deleted%'";
			// get a cursor containing the notes titles
			return activity.ManagedQuery(Tomdroid.CONTENT_URI, TITLE_PROJECTION, where, null, null);
		}
		
		// gets the ids of the notes present in the db, used in SyncService.deleteNotes()
		public static ICursor getGuids(Activity activity) {
			
			// get a cursor containing the notes guids
			return activity.ManagedQuery(Tomdroid.CONTENT_URI, GUID_PROJECTION, null, null, null);
		}
		
		public static int getNoteId(Activity activity, string title) {
			
			int id = 0;
			
			// get the notes ids
			string[] whereArgs = { title.ToUpper() };
			ICursor cursor = activity.ManagedQuery(Tomdroid.CONTENT_URI, ID_PROJECTION, "UPPER("+Note.TITLE+")=?", whereArgs, null);
			
			// cursor must not be null and must return more than 0 entry 
			if (!(cursor == null || cursor.Count == 0)) {
				
				cursor.MoveToFirst();
				id = cursor.GetInt(cursor.GetColumnIndexOrThrow(Note.ID));
			}
			else {
				// TODO send an error to the user
				TLog.d(TAG, "Cursor returned null or 0 notes");
			}
			
			return id;
		}

		public static int getNoteIdByGUID(Activity activity, string guid) {
			int id = 0;
			
			// get the notes ids
			string[] whereArgs = { guid };
			ICursor cursor = activity.ManagedQuery(Tomdroid.CONTENT_URI, ID_PROJECTION, Note.GUID+"=?", whereArgs, null);
			
			// cursor must not be null and must return more than 0 entry 
			if (!(cursor == null || cursor.Count == 0)) {
				
				cursor.MoveToFirst();
				id = cursor.GetInt(cursor.GetColumnIndexOrThrow(Note.ID));
			}
			else {
				// TODO send an error to the user
				TLog.d(TAG, "Cursor returned null or 0 notes");
			}
			
			return id;
		}
		
			
		/**
		 * stripTitleFromContent
		 * Because of an historic oddity in Tomboy's note format, a note's title is in a <title> tag but is also repeated
		 * in the <note-content> tag. This method strips it from <note-content>.
		 * @param noteContent
		 */
		public static string stripTitleFromContent(string xmlContent, string title) {
			// get rid of the title that is doubled in the note's content
			// using quote to escape potential regexp chars in pattern
			Pattern stripTitle = Pattern.Compile("^\\s*"+Pattern.Quote(XmlUtils.escape(title))+"\\n\\n"); 

			Matcher m = stripTitle.Matcher(xmlContent);
			if (m.Find()) {
				xmlContent = xmlContent.Substring(m.End(), xmlContent.Length);
				TLog.d(TAG, "stripped the title from note-content");
			}
			
			return xmlContent;
		}
		
		/**
		 * getNewNotes
		 * get a guid list of notes that are newer than latest sync date 
		 * @param activity
		 */
		public static ICursor getNewNotes(Activity activity) {
			ICursor cursor = activity.ManagedQuery(Tomdroid.CONTENT_URI, DATE_PROJECTION, "strftime('%s', "+Note.MODIFIED_DATE+") > strftime('%s', '"+Preferences.GetString(Preferences.Key.LATEST_SYNC_DATE)+"')", null, null);	
					
			return cursor;
		}

		/**
		 * validateNoteTitle
		 * check title against titles that exist in database, returning modified title if necessary 
		 * @param activity - the calling activity
		 * @param noteTitle - the title to check
		 * @param guid - the note's guid, to avoid checking against itself
		 * @return new title
		 */
		public static string validateNoteTitle(Activity activity, string noteTitle, string guid) {

			string origTitle = noteTitle;

			// check for empty titles, set to Resource.String.NewNoteTitle
			
			if (noteTitle == null || noteTitle.Replace(" ","").Equals("")) {
				noteTitle = activity.GetString(Resource.String.NewNoteTitle);
				origTitle = noteTitle; // have to set this too!
			}

			// check for duplicate titles - add number to end

			ICursor cursor = getTitles(activity);
			
			// cursor must not be null and must return more than 0 entry 
			if (!(cursor == null || cursor.Count == 0)) {
				
				List<string> titles = new List<string>();

				cursor.MoveToFirst();
				do {
					string aguid = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.GUID));
					if(!guid.Equals(aguid)) // skip this note
						titles.Add(cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TITLE)));
				} while (cursor.MoveToNext());
				
				// sort to get {"Note","Note 2", "Note 3", ... }
				Collections.sort(titles);

				int inc = 2;
				foreach(string atitle in titles) {
					if(atitle.Length == 0)
						continue;
					
					if(atitle.EqualsIgnoreCase(noteTitle)) {
						if(inc == 1)  // first match, matching "Note", set to "Note 2"
							noteTitle = noteTitle + " 2";
						else // later match, matching "Note X", set to "Note X+1"
							noteTitle = origTitle + " " + inc;
						inc++;
					}
				}
			}
			
			return noteTitle;
		}

		
		/**
		 * Builds a regular expression pattern that will match any of the note title currently in the collection.
		 * Useful for the Linkify to create the links to the notes.
		 * @return regexp pattern
		 */
		public static Pattern buildNoteLinkifyPattern(Activity activity, string noteTitle)  {
		
			StringBuilder sb = new StringBuilder();
			ICursor cursor = getTitles(activity);
		
			// cursor must not be null and must return more than 0 entry
			if (!(cursor == null || cursor.Count == 0)) {
		
				string title;
		
				cursor.MoveToFirst();
		
				do {
					title = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TITLE));
					if(title.Length == 0 || title.Equals(noteTitle))
						continue;
					// Pattern.quote() here make sure that special characters in the note's title are properly escaped
					sb.Append("("+Pattern.Quote(title)+")|");
		
				} while (cursor.MoveToNext());
				
				// if only empty titles, return
				if (sb.Length == 0)
					return null;
				
				// get rid of the last | that is not needed (I know, its ugly.. better idea?)
				string pt = sb.ToString().Substring(0, sb.Length-1);
		
				// return a compiled match pattern
				return Pattern.Compile(pt, Pattern.CaseInsensitive);
		
			} else {
		
				// TODO send an error to the user
				TLog.d(TAG, "Cursor returned null or 0 notes");
			}
		
			return null;
		}
		
		public static string toggleSortOrder() {
			string orderBy = getSortOrder();
			if(orderBy == null) {
				orderBy = "sort_title";
			} else if(orderBy.Equals("sort_title")) {
				orderBy = "sort_date";
			} else {
				orderBy = "sort_title";
			}
			setSortOrder(orderBy);
			return orderBy;
		}
	}
}