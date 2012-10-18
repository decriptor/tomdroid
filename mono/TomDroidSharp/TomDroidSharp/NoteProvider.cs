/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009 Olivier Bilodeau <olivier@bottomlesspit.org>
 * Copyright 2009 Benoit Garret <benoit.garret_launchpad@gadz.org>
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
/*
 * Parts of this file is Copyright (C) 2007 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/*
 * This file was inspired by com.example.android.notepad.NotePadProvider 
 * available in the Android SDK. 
 */

using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.Net;
using Android.Text;

using TomDroidSharp;
using TomDroidSharp.util;

namespace TomDroidSharp
{
	public class NoteProvider : ContentProvider {
		
		// ContentProvider stuff
		// --	
		private static readonly string DATABASE_NAME = "tomdroid-notes.db";
		private static readonly string DB_TABLE_NOTES = "notes";
		private static readonly int DB_VERSION = 4;
		
	    private static HashMap<string, string> notesProjectionMap;

	    private static readonly int NOTES = 1;
	    private static readonly int NOTE_ID = 2;
	    private static readonly int NOTE_TITLE = 3;

	    private static readonly UriMatcher uriMatcher;
	    
	    // Logging info
	    private static readonly string TAG = "NoteProvider";
	       
	    // List of each version's columns
		private static readonly string[][] COLUMNS_VERSION = {
			{ Note.TITLE, Note.FILE, Note.MODIFIED_DATE },
			{ Note.GUID, Note.TITLE, Note.FILE, Note.NOTE_CONTENT, Note.MODIFIED_DATE },
			{ Note.GUID, Note.TITLE, Note.FILE, Note.NOTE_CONTENT, Note.MODIFIED_DATE, Note.TAGS },
			{ Note.GUID, Note.TITLE, Note.FILE, Note.NOTE_CONTENT, Note.NOTE_CONTENT_PLAIN, Note.MODIFIED_DATE, Note.TAGS }
		};

	    /**
	     * This class helps open, create, and upgrade the database file.
	     */
	    private static class DatabaseHelper : SQLiteOpenHelper {

	        DatabaseHelper(Context context) : base(context, DATABASE_NAME, null, DB_VERSION) {
	        }

	        public override void OnCreate(SQLiteDatabase db) {
	            db.ExecSQL("CREATE TABLE " + DB_TABLE_NOTES	 + " ("
	                    + Note.ID + " INTEGER PRIMARY KEY,"
	                    + Note.GUID + " TEXT,"
	                    + Note.TITLE + " TEXT,"
	                    + Note.FILE + " TEXT,"
	                    + Note.NOTE_CONTENT + " TEXT,"
	                    + Note.NOTE_CONTENT_PLAIN + " TEXT,"
	                    + Note.MODIFIED_DATE + " string,"
	                    + Note.TAGS + " string"
	                    + ");");
	        }

	        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {
	        	TLog.d(TAG, "Upgrading database from version {0} to {1}",
	                    oldVersion, newVersion);
	        	Cursor notesCursor;
	        	List<Map<string, string>> db_list = new List<Map<string, string>>();
	        	notesCursor = db.Query(DB_TABLE_NOTES, COLUMNS_VERSION[oldVersion - 1], null, null, null, null, null);
	        	notesCursor.moveToFirst();

	        	if (oldVersion == 1) {
	        		// GUID and NOTE_CONTENT are not saved.
	        		TLog.d(TAG, "Database version {0} is not supported to update, all old datas will be destroyed", oldVersion);
	        		db.ExecSQL("DROP TABLE IF Exists notes");
	        		onCreate(db);
	        		return;
	        	}

				// Get old datas from the SQL
				while(!notesCursor.isAfterLast()) {
					Map<string, string> row = new HashMap<string, string>();
					for(int i = 0; i < COLUMNS_VERSION[oldVersion - 1].Length; i++) {
						row.put(COLUMNS_VERSION[oldVersion - 1][i], notesCursor.getstring(i));
					}

					// create new columns
					if (oldVersion <= 2) {
						row.put(Note.TAGS, "");
					}
					if (oldVersion <= 3) {
						row.put(Note.NOTE_CONTENT_PLAIN, stringConverter.encode(Html.fromHtml(row.get(Note.TITLE) + "\n" + row.get(Note.NOTE_CONTENT)).tostring()));
					}

					db_list.add(row);
					notesCursor.moveToNext();
				}

	            db.ExecSQL("DROP TABLE IF Exists notes");
	            onCreate(db);

				// put rows to the database
				ContentValues row = new ContentValues();
				for(int i = 0; i < db_list.size(); i++) {
					for(int j = 0; j < COLUMNS_VERSION[newVersion - 1].Length; j++) {
						row.put(COLUMNS_VERSION[newVersion - 1][j], db_list.get(i).get(COLUMNS_VERSION[newVersion - 1][j]));
					}
					db.Insert(DB_TABLE_NOTES, null, row);
				}
	        }
	    }

	    private DatabaseHelper dbHelper;

	    public override bool onCreate() {
	        dbHelper = new DatabaseHelper(getContext());
	        return true;
	    }

	    public override Cursor Query(Uri uri, string[] projection, string selection, string[] selectionArgs,
	            string sortOrder) {
	        SQLiteQueryBuilder qb = new SQLiteQueryBuilder();

	        switch (uriMatcher.Match(uri)) {
	        case NOTES:
	            qb.Tables = DB_TABLE_NOTES;
	            qb.SetProjectionMap(notesProjectionMap);
	            break;

	        case NOTE_ID:
	            qb.Tables = DB_TABLE_NOTES;
	            qb.SetProjectionMap(notesProjectionMap);
	            qb.AppendWhere(Note.ID + "=" + uri.PathSegments[1]);
	            break;
	            
	        case NOTE_TITLE:
	        	qb.Tables = DB_TABLE_NOTES;
	        	qb.SetProjectionMap(notesProjectionMap);
	        	// TODO appendWhere + whereArgs instead (new string[] whereArgs = uri.getLas..)?
	        	qb.AppendWhere(Note.TITLE + " LIKE '" + uri.LastPathSegment +"'");
	        	break;

	        default:
	            throw new IllegalArgumentException("Unknown URI " + uri);
	        }

	        // If no sort order is specified use the default
	        string orderBy;
	        if (TextUtils.isEmpty(sortOrder)) {
	      	    string defaultSortOrder;
	    	    defaultSortOrder = Preferences.getstring(Preferences.Key.SORT_ORDER);
	    	    if(defaultSortOrder.equals("sort_title")) {
	    	        orderBy = Note.TITLE + " ASC";
	    	    } else {
	    	        orderBy = Note.MODIFIED_DATE + " DESC";
	    	    }
	        } else {
	            orderBy = sortOrder;
	        }
	        

	        // Get the database and run the Query
	        SQLiteDatabase db = dbHelper.getReadableDatabase();
	        Cursor c = qb.Query(db, projection, selection, selectionArgs, null, null, orderBy);

	        // Tell the cursor what uri to watch, so it knows when its source data changes
	        c.setNotificationUri(getContext().ContentResolver, uri);
	        return c;
	    }

	    public override string getType(Uri uri) {
	        switch (uriMatcher.match(uri)) {
	        case NOTES:
	            return Tomdroid.CONTENT_TYPE;

	        case NOTE_ID:
	            return Tomdroid.CONTENT_ITEM_TYPE;
	            
	        case NOTE_TITLE:
	        	return Tomdroid.CONTENT_ITEM_TYPE;

	        default:
	            throw new IllegalArgumentException("Unknown URI " + uri);
	        }
	    }

	    // TODO the following method is probably never called and probably wouldn't work
	    public override Uri insert(Uri uri, ContentValues initialValues) {
	        // Validate the requested uri
	        if (uriMatcher.match(uri) != NOTES) {
	            throw new IllegalArgumentException("Unknown URI " + uri);
	        }

	        ContentValues values;
	        if (initialValues != null) {
	            values = new ContentValues(initialValues);
	        } else {
	            values = new ContentValues();
	        }

	        // TODO either be identical to Tomboy's time format (if sortable) else make sure that this is documented
	        Long now = Long.valueOf(System.currentTimeMillis());

	        // Make sure that the fields are all set
	        if (values.containsKey(Note.MODIFIED_DATE) == false) {
	            values.put(Note.MODIFIED_DATE, now);
	        }
	        
	        // The guid is the unique identifier for a note so it has to be set.
	        if (values.containsKey(Note.GUID) == false) {
	        	values.put(Note.GUID, UUID.randomUUID().tostring());
	        }

	        // TODO does this make sense?
	        if (values.containsKey(Note.TITLE) == false) {
	            Resources r = Resources.getSystem();
	            values.put(Note.TITLE, r.getstring(android.R.string.untitled));
	        }

	        if (values.containsKey(Note.FILE) == false) {
	            values.put(Note.FILE, "");
	        }
	        
	        if (values.containsKey(Note.NOTE_CONTENT) == false) {
	            values.put(Note.NOTE_CONTENT, "");
	        }

	        SQLiteDatabase db = dbHelper.getWritableDatabase();
	        long rowId = db.insert(DB_TABLE_NOTES, Note.FILE, values); // not so sure I did the right thing here
	        if (rowId > 0) {
	            Uri noteUri = ContentUris.withAppendedId(Tomdroid.CONTENT_URI, rowId);
	            getContext().ContentResolver.notifyChange(noteUri, null);
	            return noteUri;
	        }

	        throw new SQLException("Failed to insert row into " + uri);
	    }

	    public override int delete(Uri uri, string where, string[] whereArgs) {
	        SQLiteDatabase db = dbHelper.getWritableDatabase();
	        int count;
	        switch (uriMatcher.match(uri)) {
	        case NOTES:
	            count = db.delete(DB_TABLE_NOTES, where, whereArgs);
	            break;

	        case NOTE_ID:
	            string noteId = uri.getPathSegments().get(1);
	            count = db.delete(DB_TABLE_NOTES, Note.ID + "=" + noteId
	                    + (!TextUtils.isEmpty(where) ? " AND (" + where + ')' : ""), whereArgs);
	            break;

	        default:
	            throw new IllegalArgumentException("Unknown URI " + uri);
	        }

	        getContext().ContentResolver.notifyChange(uri, null);
	        return count;
	    }

	    public override int update(Uri uri, ContentValues values, string where, string[] whereArgs) {
	        SQLiteDatabase db = dbHelper.getWritableDatabase();
	        int count;
	        switch (uriMatcher.match(uri)) {
	        case NOTES:
	            count = db.update(DB_TABLE_NOTES, values, where, whereArgs);
	            break;

	        case NOTE_ID:
	            string noteId = uri.getPathSegments().get(1);
	            count = db.update(DB_TABLE_NOTES, values, Note.ID + "=" + noteId
	                    + (!TextUtils.isEmpty(where) ? " AND (" + where + ')' : ""), whereArgs);
	            break;

	        default:
	            throw new IllegalArgumentException("Unknown URI " + uri);
	        }

	        getContext().ContentResolver.notifyChange(uri, null);
	        return count;
	    }

	    static {
	        uriMatcher = new UriMatcher(UriMatcher.NO_MATCH);
	        uriMatcher.addURI(Tomdroid.AUTHORITY, "notes", NOTES);
	        uriMatcher.addURI(Tomdroid.AUTHORITY, "notes/#", NOTE_ID);
	        uriMatcher.addURI(Tomdroid.AUTHORITY, "notes/*", NOTE_TITLE);

	        notesProjectionMap = new HashMap<string, string>();
	        notesProjectionMap.put(Note.ID, Note.ID);
	        notesProjectionMap.put(Note.GUID, Note.GUID);
	        notesProjectionMap.put(Note.TITLE, Note.TITLE);
	        notesProjectionMap.put(Note.FILE, Note.FILE);
	        notesProjectionMap.put(Note.NOTE_CONTENT, Note.NOTE_CONTENT);
	        notesProjectionMap.put(Note.NOTE_CONTENT_PLAIN, Note.NOTE_CONTENT_PLAIN);
	        notesProjectionMap.put(Note.TAGS, Note.TAGS);
	        notesProjectionMap.put(Note.MODIFIED_DATE, Note.MODIFIED_DATE);
	    }
	}
}