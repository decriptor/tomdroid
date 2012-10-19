/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009, 2010, 2011 Olivier Bilodeau <olivier@bottomlesspit.org>
 * Copyright 2009, Benoit Garret <benoit.garret_launchpad@gadz.org>
 * Copyright 2010, Rodja Trappe <mail@rodja.net>
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Android.App;
using Android.OS;
using Android.Provider;
using Android.Text.Format;
using Android.Util;

using TomDroidSharp;
using TomDroidSharp.Sync;
using TomDroidSharp.util;

using Java.IO;
using TomDroidSharp.ui;
using Java.Util.Regex;
using Java.Lang;
using TomDroidSharp.Util;

namespace TomDroidSharp.Sync.sd
{
	public class SdCardSyncService : SyncService {
		
		private static Java.Util.Regex.Pattern note_content = Java.Util.Regex.Pattern.Compile("<note-content[^>]+>(.*)<\\/note-content>", Java.Util.Regex.Pattern.CASE_INSENSITIVE+Pattern.DOTALL);

		// list of notes to sync
		private List<Note> syncableNotes = new List<Note>();

		// logging related
		private readonly static string TAG = "SdCardSyncService";
		
		public SdCardSyncService(Activity activity, Handler handler) : base(activity,handler) {
		}
		
		public override int getDescriptionAsId() {
			return Resource.String.prefSDCard;
		}

		public override string getName() {
			return "sdcard";
		}

		public override bool needsServer() {
			return false;
		}
		
		public override bool needsLocation() {
			return true;
		}
		
		public override bool needsAuth() {
			return false;
		}

		protected override void getNotesForSync(bool push)
		{
			SetSyncProgress(0);
			
			this.push = push;
			
			// start loading local notes
			TLog.v(TAG, "Loading local notes");
			
			File path = new File(Tomdroid.NOTES_PATH);
			
			if (!path.Exists())
				path.Mkdir();
			
			TLog.i(TAG, "Path {0} Exists: {1}", path, path.Exists());
			
			// Check a second time, if not the most likely cause is the volume doesn't exist
			if(!path.Exists()) {
				TLog.w(TAG, "Couldn't create {0}", path);
				SendMessage(NO_SD_CARD);
				SetSyncProgress(100);
				return;
			}
			
			File[] fileList = path.ListFiles(new NotesFilter());

			if(cancelled) {
				doCancel();
				return; 
			}		

			// If there are no notes, just start the sync
			if (fileList == null || fileList.Length == 0) {
				TLog.i(TAG, "There are no notes in {0}", path);
				PrepareSyncableNotes(syncableNotes);
				return;
			}

		// get all remote notes for sync
			
			// every but the last note
			for(int i = 0; i < fileList.Length-1; i++) {
				if(cancelled) {
					doCancel();
					return; 
				}
				// TODO better progress reporting from within the workers
				
				// give a filename to a thread and ask to parse it
				SyncInThread(new Worker(fileList[i], false, push));
	        }

			if(cancelled) {
				doCancel();
				return; 
			}
			
			// last task, warn it so it will know to start sync
			SyncInThread(new Worker(fileList[fileList.Length-1], true, push));
		}
		
		/**
		 * Simple filename filter that grabs files ending with .note
		 * TODO move into its own static class in a util package
		 */
		private class NotesFilter : IFilenameFilter {
			public bool accept(File dir, string name) {
				return (name.EndsWith(".note"));
			}
		}
		
		/**
		 * The worker spawns a new note, parse the file its being given by the executor.
		 */
		// TODO change type to callable to be able to throw exceptions? (if you throw make sure to display an alert only once)
		// http://java.sun.com/j2se/1.5.0/docs/api/java/util/concurrent/Callable.html
		private class Worker : IRunnable {
			
			// the note to be loaded and parsed
			private Note note = new Note();
			private File file;
			private bool isLast;
			char[] buffer = new char[0x1000];
			bool push;
			public Worker(File f, bool isLast, bool push) {
				file = f;
				this.isLast = isLast;
				this.push = push;
			}

			public void run() {
				
				note.setFileName(file.AbsolutePath);
				// the note guid is not stored in the xml but in the filename
				note.setGuid(file.Name.Replace(".note", ""));
				
				// Try reading the file first
				string contents = "";
				try {
					contents = readFile(file,buffer);
				} catch (IOException e) {
					e.PrintStackTrace();
					TLog.w(TAG, "Something went wrong trying to read the note");
					SendMessage(PARSING_FAILED, ErrorList.createError(note, e));
					onWorkDone();
					return;
				}

				try {
					// Parsing
			    	// XML 
			    	// Get a SAXParser from the SAXPArserFactory
			        SAXParserFactory spf = SAXParserFactory.newInstance();
			        SAXParser sp = spf.newSAXParser();
			
			        // Get the XMLReader of the SAXParser we created
			        XMLReader xr = sp.getXMLReader();

			        // Create a new ContentHandler, send it this note to fill and apply it to the XML-Reader
			        NoteHandler xmlHandler = new NoteHandler(note);
			        xr.setContentHandler(xmlHandler);

			        // Create the proper input source
			        StringReader sr = new StringReader(contents);
			        InputSource inputSource = new InputSource(sr);
			        
					TLog.d(TAG, "parsing note. filename: {0}", file.Name());
					xr.parse(inputSource);

				// TODO wrap and throw a new exception here
				} catch (System.Exception e) {
					e.PrintStackTrace();
					if(e as TimeFormatException) TLog.e(TAG, "Problem parsing the note's date and time");
					SendMessage(PARSING_FAILED, ErrorList.createErrorWithContents(note, e, contents));
					onWorkDone();
					return;
				}
				
				// FIXME here we are re-reading the whole note just to grab note-content out, there is probably a better way to do this (I'm talking to you xmlpull.org!)
				Matcher m = note_content.Matcher(contents);
				if (m.Find()) {
					note.setXmlContent(NoteManager.stripTitleFromContent(m.Group(1),note.getTitle()));
				} else {
					TLog.w(TAG, "Something went wrong trying to grab the note-content out of a note");
					SendMessage(PARSING_FAILED, ErrorList.createErrorWithContents(note, "Something went wrong trying to grab the note-content out of a note", contents));
					onWorkDone();
					return;
				}
				
				syncableNotes.add(note);
				onWorkDone();
			}
			
			private void onWorkDone(){
				if (isLast) {
					PrepareSyncableNotes(syncableNotes);
				}
			}
		}

		private static string readFile(File file, char[] buffer)
		{
			StringBuilder outFile = new StringBuilder();
			try {
			
			int read;
			Reader reader = new InputStreamReader(new FileInputStream(file), "UTF-8");
			
			do {
			  read = reader.Read(buffer, 0, buffer.Length);
			  if (read > 0) {
			    outFile.Append(buffer, 0, read);
			  }
			}
			while (read >= 0);
			
			reader.Close();
			}
			catch(IOException io)
			{

			}
			return outFile.ToString();
		}

		// this function either deletes or pushes, based on existence of deleted tag
		public override void pushNotes(List<Note> notes) {
			if(notes.Count == 0)
				return;

			foreach(Note note in notes)
			{
				if(note.getTags().Contains("system:deleted")) // deleted note
					deleteNote(note.getGuid());
				else
					pushNote(note);
			}
			finishSync(true);
		}

		// this function is a shell to allow backup function to push as well but send a different message... may not be necessary any more...
		private void pushNote(Note note){
			TLog.v(TAG, "pushing note to sdcard");
			
			int message = doPushNote(note);

			SendMessage(message);
		}

		// actually pushes a note to sdcard, with optional subdirectory (e.g. backup)
		private static int doPushNote(Note note) {

			Note rnote = new Note();
			try {
				File path = new File(Tomdroid.NOTES_PATH);
				
				if (!path.Exists())
					path.Mkdir();
				
				TLog.i(TAG, "Path {0} Exists: {1}", path, path.Exists());
				
				// Check a second time, if not the most likely cause is the volume doesn't exist
				if(!path.Exists()) {
					TLog.w(TAG, "Couldn't create {0}", path);
					return NO_SD_CARD;
				}
				
				path = new File(Tomdroid.NOTES_PATH + "/"+note.getGuid() + ".note");
		
				note.createDate = note.getLastChangeDate().Format3339(false);
				note.cursorPos = 0;
				note.width = 0;
				note.height = 0;
				note.X = -1;
				note.Y = -1;
				
				if (path.Exists()) { // update existing note
		
					// Try reading the file first
					string contents = "";
					try {
						char[] buffer = new char[0x1000];
						contents = readFile(path,buffer);
					} catch (IOException e) {
						e.PrintStackTrace();
						TLog.w(TAG, "Something went wrong trying to read the note");
						return PARSING_FAILED;
					}
		
					try {
						// Parsing
				    	// XML 
				    	// Get a SAXParser from the SAXPArserFactory
						SAXParserFactory spf = SAXParserFactory.newInstance();
				        SAXParser sp = spf.newSAXParser();
				
				        // Get the XMLReader of the SAXParser we created
				        XMLReader xr = sp.getXMLReader();
		
				        // Create a new ContentHandler, send it this note to fill and apply it to the XML-Reader
				        NoteHandler xmlHandler = new NoteHandler(rnote);
				        xr.setContentHandler(xmlHandler);
		
				        // Create the proper input source
				        StringReader sr = new StringReader(contents);
				        InputSource inputSource = new InputSource(sr);
				        
						TLog.d(TAG, "parsing note. filename: {0}", path.Name());
						xr.parse(inputSource);
		
					// TODO wrap and throw a new exception here
					} catch (Exception e) {
						e.PrintStackTrace();
						if(e as TimeFormatException) TLog.e(TAG, "Problem parsing the note's date and time");
						return PARSING_FAILED;
					}
		
					note.createDate = rnote.createDate;
					note.cursorPos = rnote.cursorPos;
					note.width = rnote.width;
					note.height = rnote.height;
					note.X = rnote.X;		
					note.Y = rnote.Y;
					
					note.setTags(rnote.getTags());
				}
				
				string xmlOutput = note.getXmlFileString();
				
				path.CreateNewFile();
				FileOutputStream fOut = new FileOutputStream(path);
				OutputStreamWriter myOutWriter = 
										new OutputStreamWriter(fOut);
				myOutWriter.Append(xmlOutput);
				myOutWriter.Close();
				fOut.Close();	
		
			}
			catch (Exception e) {
				TLog.e(TAG, "push to sd card didn't work");
				return NOTE_PUSH_ERROR;
			}
			return NOTE_PUSHED;
		}

		private void deleteNote(string guid){
			try {
				File path = new File(Tomdroid.NOTES_PATH + "/" + guid + ".note");
				path.Delete();
			}
			catch (Exception e) {
				TLog.e(TAG, "delete from sd card didn't work");
				SendMessage(NOTE_DELETE_ERROR);
				return;
			}
			SendMessage(NOTE_DELETED);

		}
		
		// pull note used for revert
		protected override void pullNote(string guid) {
			// start loading local notes
			TLog.v(TAG, "pulling remote note");
			
			File path = new File(Tomdroid.NOTES_PATH);
			
			if (!path.Exists())
				path.Mkdir();
			
			TLog.i(TAG, "Path {0} Exists: {1}", path, path.Exists());
			
			// Check a second time, if not the most likely cause is the volume doesn't exist
			if(!path.Exists()) {
				TLog.w(TAG, "Couldn't create {0}", path);
				SendMessage(NO_SD_CARD);
				return;
			}
			
			path = new File(Tomdroid.NOTES_PATH + guid + ".note");

			SyncInThread(new Worker(path, false, false));
			
		}
		
		// backup function accessed via preferences
		public override void backupNotes() {
			Note[] notes = NoteManager.getAllNotesAsNotes(activity, true);
			if(notes != null && notes.Length > 0) 
				foreach (Note note in notes)
					doPushNote(note);
			SendMessage(NOTES_BACKED_UP);
		}

		// auto backup function on save
		public static void backupNote(Note note) {
			doPushNote(note);
		}
		
		public override void finishSync(bool refresh) {
			// delete leftover local notes
			NoteManager.purgeDeletedNotes(activity);
			
			Time now = new Time();
			now.SetToNow();
			string nowstring = now.Format3339(false);
			Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, nowstring);

			setSyncProgress(100);
			if (refresh)
				SendMessage(PARSING_COMPLETE);
		}

		public override void deleteAllNotes() {
			try {
				File path = new File(Tomdroid.NOTES_PATH);
				File[] fileList = path.ListFiles(new NotesFilter());
				
				for(int i = 0; i < fileList.Length-1; i++) {
					fileList[i].Delete();
		        }
			}
			catch (Exception e) {
				TLog.e(TAG, "delete from sd card didn't work");
				SendMessage(NOTE_DELETE_ERROR);
				return;
			}
			TLog.d(TAG, "notes deleted from SD Card");
			SendMessage(REMOTE_NOTES_DELETED);
		}

		protected override void localSyncComplete() {
		}
	}
}