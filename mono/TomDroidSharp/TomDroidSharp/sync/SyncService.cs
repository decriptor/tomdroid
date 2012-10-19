/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009, Olivier Bilodeau <olivier@bottomlesspit.org>
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

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Text;

using TomDroidSharp;
using Java.Lang;
using TomDroidSharp.Util;
using Android.Text.Format;
using TomDroidSharp.util;
using TomDroidSharp.ui;

namespace TomDroidSharp.Sync
{
	public abstract class SyncService
	{	
		private static readonly string TAG = "SyncService";
		
		public Activity activity;
		//private readonly ExecutorService pool;
		private readonly static int poolSize = 1;
		
		private Handler handler;
		protected static bool push;
		
		/**
		 * Contains the synchronization errors. These are stored while synchronization occurs
		 * and sent to the main UI along with the PARSING_COMPLETE message.
		 */
		private ErrorList syncErrors;
		private int syncProgress = 100;

		public bool cancelled = false;

		// syncing arrays
		private List<string> remoteGuids;
		private List<Note> pushableNotes;
		private List<Note> pullableNotes;
		private List<Note[]> comparableNotes;
		private List<Note> deleteableNotes;
		private List<Note[]> conflictingNotes;
		
		// number of conflicting notes
		private int conflictCount;
		private int resolvedCount;
		
		// handler messages
		public readonly static int PARSING_COMPLETE = 1;
		public readonly static int PARSING_FAILED = 2;
		public readonly static int PARSING_NO_NOTES = 3;
		public readonly static int NO_INTERNET = 4;
		public readonly static int NO_SD_CARD = 5;
		public readonly static int SYNC_PROGRESS = 6;
		public readonly static int NOTE_DELETED = 7;
		public readonly static int NOTE_PUSHED = 8;
		public readonly static int NOTE_PULLED = 9;
		public readonly static int NOTE_PUSH_ERROR = 10;
		public readonly static int NOTE_DELETE_ERROR = 11;
		public readonly static int NOTE_PULL_ERROR = 12;
		public readonly static int NOTES_PUSHED = 13;
		public readonly static int BEGIN_PROGRESS = 14;
		public readonly static int INCREMENT_PROGRESS = 15;
		public readonly static int IN_PROGRESS = 16;
		public readonly static int NOTES_BACKED_UP = 17;
		public readonly static int NOTES_RESTORED = 18;
		public readonly static int CONNECTING_FAILED = 19;
		public readonly static int AUTH_COMPLETE = 20;
		public readonly static int AUTH_FAILED = 21;
		public readonly static int REMOTE_NOTES_DELETED = 22;
		public readonly static int SYNC_CANCELLED = 23;
		public readonly static int LATEST_REVISION = 24;
		public readonly static int SYNC_CONNECTED = 25;
		
		public SyncService(Activity activity, Handler handler) {
			
			this.activity = activity;
			this.handler = handler;
			pool = Executors.newFixedThreadPool(poolSize);
		}

		public void startSynchronization(bool push) {
			
			syncErrors = null;
			
			if (syncProgress != 100){
				SendMessage(IN_PROGRESS);
				return;
			}
			
			// deleting "First Note"
			Note firstNote = NoteManager.getNoteByGuid(activity, "8f837a99-c920-4501-b303-6a39af57a714");
			if(firstNote != null)
				NoteManager.deleteNote(activity, firstNote.getDbId());
			
			getNotesForSync(push);
		}
		
		protected abstract void getNotesForSync(bool push);
		public abstract bool needsServer();
		public abstract bool needsLocation();
		public abstract bool needsAuth();
		
		/**
		 * @return An unique identifier, not visible to the user.
		 */
		
		public abstract string getName();
		
		/**
		 * @return An human readable name, used in the preferences to distinguish the different sync services.
		 */
		public abstract int getDescriptionAsId();
		
		public string getDescription() {
			return activity.GetString(getDescriptionAsId());
		}
		/**
		 * Execute code in a separate thread.
		 * Use this for blocking and/or cpu intensive operations and thus avoid blocking the UI.
		 * 
		 * @param r The Runner subclass to execute
		 */
		
		protected void execInThread(Runnable r) {
			
			pool.execute(r);
		}

		/**
		 * Execute code in a separate thread.
		 * Any exception thrown by the thread will be added to the error list
		 * @param r The runner subclass to execute
		 */
		protected void SyncInThread(Runnable r) {
//			Runnable task = new Runnable() {
//				public void run() {
//					try {
//						r.run();
//					} catch(Exception e) {
//						TLog.e(TAG, e, "Problem syncing in thread");
//						SendMessage(PARSING_FAILED, ErrorList.createError("System Error", "system", e));
//					}
//				}
//			};
			
			execInThread(task);
		}
		
		/**
		 * Insert last note in the content provider.
		 * 
		 * @param note The note to insert.
		 */
		
		protected void insertNote(Note note) {
			NoteManager.putNote(this.activity, note);
			SendMessage(INCREMENT_PROGRESS );
		}	

		// syncing based on updated local notes only
		protected void prepareSyncableNotes(ICursor localGuids)
		{
			remoteGuids = new List<string>();
			pushableNotes = new List<Note>();
			pullableNotes = new List<Note>();
			comparableNotes = new List<Note[]>();
			deleteableNotes = new List<Note>();
			conflictingNotes = new List<Note[]>();
			
			localGuids.MoveToFirst();
			do {
				Note note = NoteManager.getNoteByGuid(activity, localGuids.GetString(localGuids.GetColumnIndexOrThrow(Note.GUID)));
				
				if(!note.getTags().Contains("system:template")) // don't push templates TODO: find out what's wrong with this, if anything
					pushableNotes.Add(note);
			} while (localGuids.MoveToNext());
			
			if(cancelled) {
				doCancel();
				return; 
			}		
			
			doSyncNotes();
		}

		
		// syncing with remote changes
		protected void prepareSyncableNotes(List<Note> notesList) {

			remoteGuids = new List<string>();
			pushableNotes = new List<Note>();
			pullableNotes = new List<Note>();
			comparableNotes = new List<Note[]>();
			deleteableNotes = new List<Note>();
			conflictingNotes = new List<Note[]>();
			
			// check if remote notes are already in local

			foreach(Note remoteNote in notesList)
			{
				Note localNote = NoteManager.getNoteByGuid(activity,remoteNote.getGuid());
				remoteGuids.Add(remoteNote.getGuid());
				if(localNote == null) {
					
					// check to make sure there is no note with this title, otherwise show conflict dialogue

					ICursor cursor = NoteManager.getTitles(activity);
					
					if (!(cursor == null || cursor.Count == 0)) {

						cursor.MoveToFirst();
						do {
							string atitle = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.TITLE));
							if(atitle.Equals(remoteNote.getTitle())) {
								string aguid = cursor.GetString(cursor.GetColumnIndexOrThrow(Note.GUID));
								localNote = NoteManager.getNoteByGuid(activity, aguid);
								break;
							}
						} while (cursor.MoveToNext());
					}
					cursor.Close();
					
					if(localNote == null)
						pullableNotes.Add(remoteNote);
					else { // compare two different notes with same title
						remoteGuids.Add(localNote.getGuid()); // add to avoid catching it below
						Note[] compNotes = {localNote, remoteNote};
						comparableNotes.Add(compNotes);
					}
				}
				else {
					Note[] compNotes = {localNote, remoteNote};
					comparableNotes.Add(compNotes);
				}
			}

			if(cancelled) {
				doCancel();
				return; 
			}
			
			// get non-remote notes; if newer than last sync, push, otherwise delete
			
			ICursor localGuids = NoteManager.getGuids(this.activity);
			if (!(localGuids == null || localGuids.Count == 0)) {
				
				string localGuid;
				
				localGuids.MoveToFirst();
				do {
					localGuid = localGuids.GetString(localGuids.GetColumnIndexOrThrow(Note.GUID));
					
					if(!remoteGuids.Contains(localGuid)) {
						Note note = NoteManager.getNoteByGuid(this.activity, localGuid);
						string syncDatestring = Preferences.GetString(Preferences.Key.LATEST_SYNC_DATE);
						Time syncDate = new Time();
						syncDate.Parse3339(syncDatestring);
						int compareSync = Time.Compare(syncDate, note.getLastChangeDate());
						if(compareSync > 0) // older than last sync, means it's been deleted from server
							deleteableNotes.Add(note);
						else if(!note.getTags().Contains("system:template")) // don't push templates TODO: find out what's wrong with this, if anything
							pushableNotes.Add(note);
					}
					
				} while (localGuids.MoveToNext());

			}
			TLog.d(TAG, "Notes to pull: {0}, Notes to push: {1}, Notes to delete: {2}, Notes to compare: {3}",pullableNotes.Count,pushableNotes.Count,deleteableNotes.Count,comparableNotes.Count);

			if(cancelled) {
				doCancel();
				return; 
			}


		// deal with notes in both - compare and push, pull or diff
			
			syncDatestring = Preferences.GetString(Preferences.Key.LATEST_SYNC_DATE);
			syncDate = new Time();
			syncDate.Parse3339(syncDatestring);

			foreach (var notes in comparableNotes)
			{	
				Note localNote = notes[0];
				Note remoteNote = notes[1];

			// if different guids, means conflicting titles

				if(!remoteNote.getGuid().Equals(localNote.getGuid())) {
					TLog.i(TAG, "adding conflict of two different notes with same title");
					conflictingNotes.Add(notes);
					continue;
				}
				
				if(cancelled) {
					doCancel();
					return; 
				}
				
				int compareSyncLocal = Time.Compare(syncDate, localNote.getLastChangeDate());
				int compareSyncRemote = Time.Compare(syncDate, remoteNote.getLastChangeDate());
				int compareBoth = Time.Compare(localNote.getLastChangeDate(), remoteNote.getLastChangeDate());

			// if not two-way and not same date, overwrite the local version
			
				if(!push && compareBoth != 0) {
					TLog.i(TAG, "Different note dates, overwriting local note");
					pullableNotes.Add(remoteNote);
					continue;
				}

			// begin compare

				if(cancelled) {
					doCancel();
					return; 
				}
				
				// check date difference
				
				TLog.v(TAG, "compare both: {0}, compare local: {1}, compare remote: {2}", compareBoth, compareSyncLocal, compareSyncRemote);
				if(compareBoth != 0)
					TLog.v(TAG, "Different note dates");
				if((compareSyncLocal < 0 && compareSyncRemote < 0) || (compareSyncLocal > 0 && compareSyncRemote > 0))
					TLog.v(TAG, "both either older or newer");
					
				if(compareBoth != 0 && ((compareSyncLocal < 0 && compareSyncRemote < 0) || (compareSyncLocal > 0 && compareSyncRemote > 0))) { // sync conflict!  both are older or newer than last sync
					TLog.i(TAG, "Note Conflict: TITLE:{0} GUID:{1}", localNote.getTitle(), localNote.getGuid());
					conflictingNotes.Add(notes);
				}
				else if(compareBoth > 0) // local newer, bundle in pushable
					pushableNotes.Add(localNote);
				else if(compareBoth < 0) { // local older, pull immediately, no need to bundle
					TLog.i(TAG, "Local note is older, updating in content provider TITLE:{0} GUID:{1}", localNote.getTitle(), localNote.getGuid());
					pullableNotes.Add(remoteNote);
				}
				else { // both same date
					if(localNote.getTags().Contains("system:deleted") && push) { // deleted, bundle for remote deletion
						TLog.i(TAG, "Notes are same date, deleted, deleting remote: TITLE:{0} GUID:{1}", localNote.getTitle(), localNote.getGuid());
						pushableNotes.Add(localNote);
					}
					else { // do nothing
						TLog.i(TAG, "Notes are same date, doing nothing: TITLE:{0} GUID:{1}", localNote.getTitle(), localNote.getGuid());
						// NoteManager.putNote(activity, remoteNote);
					}
				}
			}
			if(conflictingNotes.IsEmpty())
				doSyncNotes();
			else 
				fixConflictingNotes();
		}

		// fix conflicting notes, putting sync on pause, to be resumed once conflicts are all resolved
		private void fixConflictingNotes() {
			
			conflictCount = 0;
			resolvedCount = 0;
			foreach(var notes in conflictingNotes)
			{	
				Note localNote = notes[0];
				Note remoteNote = notes[1];
				int compareBoth = Time.Compare(localNote.getLastChangeDate(), remoteNote.getLastChangeDate());
				
				TLog.v(TAG, "note conflict... showing resolution dialog TITLE:{0} GUID:{1}", localNote.getTitle(), localNote.getGuid());
				
				// send everything to Tomdroid so it can show Sync Dialog
				
			    Bundle bundle = new Bundle();	
				bundle.PutString("title",remoteNote.getTitle());
				bundle.PutString("file",remoteNote.getFileName());
				bundle.PutString("guid",remoteNote.getGuid());
				bundle.PutString("date",remoteNote.getLastChangeDate().Format3339(false));
				bundle.PutString("content", remoteNote.getXmlContent());
				bundle.PutString("tags", remoteNote.getTags());
				bundle.PutInt("datediff", compareBoth);
				
				// put local guid if conflicting titles

				if(!remoteNote.getGuid().Equals(localNote.getGuid()))
					bundle.PutString("localGUID", localNote.getGuid());
				
				Intent intent = new Intent(activity.ApplicationContext, typeof(CompareNotes));	
				intent.PutExtras(bundle);
		
				// let activity know each time the conflict is resolved, to let the service know to increment resolved conflicts.
				// once all conflicts are resolved, start sync
				activity.StartActivityForResult(intent, conflictCount++);
			}	
		}
		
		// actually do sync
		private void doSyncNotes() {

		// init progress bar
			
			int totalNotes = pullableNotes.Count+pushableNotes.Count+deleteableNotes.Count;
			
			if(totalNotes > 0) {
				SendMessage(BEGIN_PROGRESS,totalNotes,0);
			}
			else { // quit
				setSyncProgress(100);
				SendMessage(PARSING_COMPLETE);
				return;
			}

			if(cancelled) {
				doCancel();
				return; 
			}

		// deal with notes that are not in local content provider - always pull
		
			foreach (Note note in pullableNotes)
				insertNote(note);

			setSyncProgress(70);

			if(cancelled) {
				doCancel();
				return; 
			}

		// deal with deleteable notes
			
			deleteNotes(deleteableNotes);
			
		// deal with notes not in remote service - push or delete
			
			if(pushableNotes.isEmpty())
				finishSync(true);
			else {
				// notify service that local syncing is complete, so it can update sync revision to remote
				localSyncComplete();
				setSyncProgress(90);

				// if one-way sync, delete pushable notes, else push
				if(!push) {
					deleteNotes(pushableNotes);
					finishSync(true);
				}
				else
					pushNotes(pushableNotes);
				
			} 
		}

		protected void deleteNotes(List<Note> notes)
		{
			foreach(Note note in notes)
				NoteManager.deleteNote(this.activity, note.getDbId());
		}

		/**
		 * Send a message to the main UI.
		 * 
		 * @param message The message id to send, the PARSING_* or NO_INTERNET attributes can be used.
		 */
		
		protected void SendMessage(int message) {
			
			if(!SendMessage(message, null)) {
				handler.SendEmptyMessage(message);
			}
		}
		protected void SendMessage(int message_id, int arg1, int arg2) {
			Message message = handler.ObtainMessage(message_id);
			message.Arg1 = arg1;
			message.Arg2 = arg2;
			handler.SendMessage(message);
		}	
		protected bool SendMessage(int message_id, Dictionary<string, object> payload) {

			Message message;
			switch(message_id) {
				case PARSING_FAILED:
				case NOTE_PUSH_ERROR:
				case NOTE_DELETE_ERROR:
				case NOTE_PULL_ERROR:
				case PARSING_COMPLETE:
					if(payload == null && syncErrors == null)
						return false;
					if(syncErrors == null)
						syncErrors = new ErrorList();
					syncErrors.Add(payload);
					message = handler.ObtainMessage(message_id, syncErrors);
					handler.SendMessage(message);
					return true;
			}
			return false;
		}
		
		/**
		 * Update the synchronization progress
		 * 
		 * TODO: rename to distinguish from new progress?
		 * 
		 * @param progress new progress (syncProgress is old)
		 */
		
		public void setSyncProgress(int progress) {
//			synchronized (TAG) {
//				TLog.v(TAG, "sync progress: {0}", progress);
//				Message progressMessage = new Message();
//				progressMessage.what = SYNC_PROGRESS;
//				progressMessage.arg1 = progress;
//				progressMessage.arg2 = syncProgress;
//
//				handler.SendMessage(progressMessage);
//				syncProgress = progress;
//			}
		}
		
		protected int getSyncProgress()
		{
//			synchronized (TAG) {
//				return syncProgress;
//			}
		}

		public bool isSyncable() {
			return getSyncProgress() == 100;
		}

		// new methods to T Edit
		
		protected abstract void pullNote(string guid);

		public abstract void finishSync(bool refresh);

		public abstract void pushNotes(List<Note> notes);

		public abstract void backupNotes();

		public abstract void deleteAllNotes();

		protected abstract void localSyncComplete();

		public void setCancelled(bool cancel) {
			this.cancelled  = cancel;
		}

		public bool doCancel() {
			TLog.v(TAG, "sync cancelled");
			
			setSyncProgress(100);
			SendMessage(SYNC_CANCELLED);
			
			return true;
		}

		public void resolvedConflict(int requestCode) {
			resolvedCount++;
			if(resolvedCount == conflictCount)
				doSyncNotes();
		}

		public void addPullable(Note note) {
			this.pullableNotes.Add(note);
		}

		public void addPushable(Note note) {
			this.pushableNotes.Add(note);
		}

		public void addDeleteable(Note note) {
			this.deleteableNotes.Add(note);
		}
	}
}