/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009, Benoit Garret <benoit.garret_launchpad@gadz.org>
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
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Text.Format;

using TomDroidSharp.util;
using TomDroidSharp.Sync;

namespace TomDroidSharp.sync.web
{
	public class SnowySyncService : SyncService, ServiceAuth {
		#region implemented abstract members of SyncService
		public override void pushNotes (System.Collections.Generic.List<Note> notes)
		{
			throw new System.NotImplementedException ();
		}
		#endregion

		private static readonly string TAG = "SnowySyncService";
		private string lastGUID;
		private long latestRemoteRevision = -1;
		private long latestLocalRevision = -1;

		public SnowySyncService(Activity activity, Handler handler) : base(activity, handler) {
		}

		public override int getDescriptionAsId() {
			return Resource.String.prefTomboyWeb;
		}

		public override string getName() {
			return "tomboy-web";
		}

		public bool isConfigured() {
			OAuthConnection auth = getAuthConnection();
			return auth.isAuthenticated();
		}

		public override bool needsServer() {
			return true;
		}

		public override bool needsLocation() {
			return false;
		}

		public override bool needsAuth() {
			OAuthConnection auth = getAuthConnection();
			return !auth.isAuthenticated();
		}

		public void getAuthUri(string server, Handler handler) {

//			execInThread(new Runnable() {
//
//				void run() {
//
//					// Reset the authentication credentials
//					OAuthConnection auth = new OAuthConnection();
//					Uri authUri = null;
//
//					try {
//						authUri = auth.getAuthorizationUrl(server);
//
//					} catch (UnknownHostException e) {
//						TLog.e(TAG, "Internet connection not available");
//						SendMessage(NO_INTERNET);
//					}
//
//					Message message = new Message();
//					message.obj = authUri;
//					handler.SendMessage(message);
//				}
//
//			});
		}

		public void remoteAuthComplete(Uri uri, Handler handler) {

//			execInThread(new Runnable() {
//
//				public void run() {
//
//					try {
//						// TODO: might be intelligent to show something like a
//						// progress dialog
//						// else the user might try to sync before the authorization
//						// process
//						// is complete
//						OAuthConnection auth = getAuthConnection();
//						bool result = auth.getAccess(uri
//								.getQueryParameter("oauth_verifier"));
//
//						if (result) {
//							TLog.i(TAG, "The authorization process is complete.");
//							handler.sendEmptyMessage(AUTH_COMPLETE);
//							return;
//							//sync(true);
//						} else {
//							TLog.e(TAG,
//									"Something went wrong during the authorization process.");
//							SendMessage(AUTH_FAILED);
//						}
//					} catch (UnknownHostException e) {
//						TLog.e(TAG, "Internet connection not available");
//						SendMessage(NO_INTERNET);
//					}
//
//					// We don't care what we send, just Remove the dialog
//					handler.sendEmptyMessage(0);
//				}
//			});
		}

		public new bool isSyncable()
		{
			return base.isSyncable() && isConfigured();
		}

		protected override void getNotesForSync(bool push) {
			this.push = push;
			
			// start loading snowy notes
			setSyncProgress(0);
			this.lastGUID = null;

			TLog.v(TAG, "Loading Snowy notes");

			string userRef = Preferences
					.GetString(Preferences.Key.SYNC_SERVER_USER_API);

//			SyncInThread(new Runnable() {
//
//
//				public void run() {
//
//					OAuthConnection auth = getAuthConnection();
//					latestRemoteRevision = (int)Preferences.getLong(Preferences.Key.LATEST_SYNC_REVISION);
//
//					try {
//						TLog.v(TAG, "contacting " + userRef);
//						string rawResponse = auth.get(userRef);
//						if(cancelled) {
//							doCancel();
//							return; 
//						}
//						if (rawResponse == null) {
//							SendMessage(CONNECTING_FAILED);
//							setSyncProgress(100);
//							return;
//						}
//
//						setSyncProgress(30);
//
//						try {
//							JSONObject response = new JSONObject(rawResponse);
//
//							// get notes list without content, to check for revision
//							
//							string notesUrl = response.getJSONObject("notes-ref").GetString("api-ref");
//							rawResponse = auth.get(notesUrl);
//							response = new JSONObject(rawResponse);
//							
//							latestLocalRevision = (Long)Preferences.getLong(Preferences.Key.LATEST_SYNC_REVISION);
//							
//							setSyncProgress(35);
//
//							latestRemoteRevision = response.getLong("latest-sync-revision");
//							SendMessage(LATEST_REVISION,(int)latestRemoteRevision,0);
//							TLog.d(TAG, "old latest sync revision: {0}, remote latest sync revision: {1}", latestLocalRevision, latestRemoteRevision);
//
//							Cursor newLocalNotes = NoteManager.getNewNotes(activity); 
//							
//							if (latestRemoteRevision <= latestLocalRevision && newLocalNotes.getCount() == 0) { // same sync revision + no new local notes = no need to sync
//								TLog.v(TAG, "old sync revision on server, cancelling");
//								finishSync(true);
//								return;
//							}
//
//							// don't get notes if older revision - only pushing notes
//							
//							if (push && latestRemoteRevision <= latestLocalRevision) {
//								TLog.v(TAG, "old sync revision on server, pushing new notes");
//								
//								JSONArray notes = response.getJSONArray("notes");
//								List<string> notesList = new List<string>();
//								for (int i = 0; i < notes.Length; i++)
//									notesList.add(notes.getJSONObject(i).optstring("guid"));
//								prepareSyncableNotes(newLocalNotes);
//								setSyncProgress(50);
//								return;
//							}
//							
//							// get notes list with content to find changes
//							
//							TLog.v(TAG, "contacting " + notesUrl);
//							SendMessage(SYNC_CONNECTED);
//							rawResponse = auth.get(notesUrl + "?include_notes=true");
//							if(cancelled) {
//								doCancel();
//								return; 
//							}
//							response = new JSONObject(rawResponse);
//							latestRemoteRevision = response.getLong("latest-sync-revision");
//							SendMessage(LATEST_REVISION,(int)latestRemoteRevision,0);
//
//							JSONArray notes = response.getJSONArray("notes");
//							setSyncProgress(50);
//
//							TLog.v(TAG, "number of notes: {0}", notes.Length);
//
//							List<Note> notesList = new List<Note>();
//
//							for (int i = 0; i < notes.Length; i++)
//								notesList.add(new Note(notes.getJSONObject(i)));
//
//							if(cancelled) {
//								doCancel();
//								return; 
//							}						
//							
//							// close cursor
//							newLocalNotes.close();
//							prepareSyncableNotes(notesList);
//							
//						} catch (JSONException e) {
//							TLog.e(TAG, e, "Problem parsing the server response");
//							SendMessage(PARSING_FAILED,
//									ErrorList.createErrorWithContents(
//											"JSON parsing", "json", e, rawResponse));
//							setSyncProgress(100);
//							return;
//						}
//					} catch (java.net.UnknownHostException e) {
//						TLog.e(TAG, "Internet connection not available");
//						SendMessage(NO_INTERNET);
//						setSyncProgress(100);
//						return;
//					}
//					if(cancelled) {
//						doCancel();
//						return; 
//					}
//					if (isSyncable())
//						finishSync(true);
//				}
//			});
		}

		public void finishSync(bool refresh) {

			// delete leftover local notes
			NoteManager.purgeDeletedNotes(activity);
			
			Time now = new Time();
			now.SetToNow();
			string nowstring = now.Format3339(false);
			Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, nowstring);
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, latestRemoteRevision);

			setSyncProgress(100);
			if (refresh)
				SendMessage(PARSING_COMPLETE);
		}

		private OAuthConnection getAuthConnection() {

			// TODO: there needs to be a way to reset these values, otherwise cannot
			// change server!

			OAuthConnection auth = new OAuthConnection();

			auth.accessToken = Preferences.GetString(Preferences.Key.ACCESS_TOKEN);
			auth.accessTokenSecret = Preferences
					.GetString(Preferences.Key.ACCESS_TOKEN_SECRET);
			auth.requestToken = Preferences
					.GetString(Preferences.Key.REQUEST_TOKEN);
			auth.requestTokenSecret = Preferences
					.GetString(Preferences.Key.REQUEST_TOKEN_SECRET);
			auth.oauth10a = Preferences.GetBoolean(Preferences.Key.OAUTH_10A);
			auth.authorizeUrl = Preferences
					.GetString(Preferences.Key.AUTHORIZE_URL);
			auth.accessTokenUrl = Preferences
					.GetString(Preferences.Key.ACCESS_TOKEN_URL);
			auth.requestTokenUrl = Preferences
					.GetString(Preferences.Key.REQUEST_TOKEN_URL);
			auth.rootApi = Preferences
					.GetString(Preferences.Key.SYNC_SERVER_ROOT_API);
			auth.userApi = Preferences
					.GetString(Preferences.Key.SYNC_SERVER_USER_API);

			return auth;
		}

		// push syncable notes
		public override void pushNotes(List<Note> notes) {
			if(notes.Count == 0)
				return;
			if(cancelled) {
				doCancel();
				return; 
			}		
			string userRef = Preferences
					.GetString(Preferences.Key.SYNC_SERVER_USER_API);
			
			long newRevision = Preferences.getLong(Preferences.Key.LATEST_SYNC_REVISION)+1;
					
//			SyncInThread(new Runnable() {
//				public void run() {
//					OAuthConnection auth = getAuthConnection();
//					try {
//						TLog.v(TAG, "pushing {0} notes to remote service, sending rev #{1}",notes.Count, newRevision);
//						string rawResponse = auth.get(userRef);
//						if(cancelled) {
//							doCancel();
//							return; 
//						}		
//						try {
//							TLog.v(TAG, "creating JSON");
//
//							JSONObject data = new JSONObject();
//							data.put("latest-sync-revision", newRevision);
//							JSONArray Jnotes = new JSONArray();
//							for(Note note : notes) {
//								JSONObject Jnote = new JSONObject();
//								Jnote.put("guid", note.getGuid());
//								
//								if(note.getTags().contains("system:deleted")) // deleted note
//									Jnote.put("command","delete");
//								else { // changed note
//									Jnote.put("title", note.getTitle());
//									Jnote.put("note-content", note.getXmlContent());
//									Jnote.put("note-content-version", "0.1");
//									Jnote.put("last-change-date", note.getLastChangeDate().Format3339(false));
//								}
//								Jnotes.put(Jnote);
//							}
//							data.put("note-changes", Jnotes);
//
//							JSONObject response = new JSONObject(rawResponse);
//							if(cancelled) {
//								doCancel();
//								return; 
//							}		
//							string notesUrl = response.getJSONObject("notes-ref")
//									.GetString("api-ref");
//
//							TLog.v(TAG, "put url: {0}", notesUrl);
//							
//							if(cancelled) {
//								doCancel();
//								return; 
//							}	
//							
//							TLog.v(TAG, "pushing data to remote service: {0}",data.ToString());
//							response = new JSONObject(auth.put(notesUrl,
//									data.ToString()));
//
//							TLog.v(TAG, "put response: {0}", response.ToString());
//							latestRemoteRevision = response.getLong("latest-sync-revision");
//							SendMessage(LATEST_REVISION,(int)latestRemoteRevision,0);
//
//						} catch (JSONException e) {
//							TLog.e(TAG, e, "Problem parsing the server response");
//							SendMessage(NOTE_PUSH_ERROR,
//									ErrorList.createErrorWithContents(
//											"JSON parsing", "json", e, rawResponse));
//							return;
//						}
//					} catch (java.net.UnknownHostException e) {
//						TLog.e(TAG, "Internet connection not available");
//						SendMessage(NO_INTERNET);
//						return;
//					}
//					// success, finish sync
//					finishSync(true);
//				}
//
//			});
		}

		protected override void pullNote(string guid) {

			// start loading snowy notes

			TLog.v(TAG, "pulling remote note");

			string userRef = Preferences
					.GetString(Preferences.Key.SYNC_SERVER_USER_API);

//			SyncInThread(new Runnable() {
//
//				public void run() {
//
//					OAuthConnection auth = getAuthConnection();
//
//					try {
//						TLog.v(TAG, "contacting " + userRef);
//						string rawResponse = auth.get(userRef);
//
//						try {
//							JSONObject response = new JSONObject(rawResponse);
//							string notesUrl = response.getJSONObject("notes-ref")
//									.GetString("api-ref");
//
//							TLog.v(TAG, "contacting " + notesUrl + guid);
//
//							rawResponse = auth.get(notesUrl + guid
//									+ "?include_notes=true");
//
//							response = new JSONObject(rawResponse);
//							JSONArray notes = response.getJSONArray("notes");
//							JSONObject jsonNote = notes.getJSONObject(0);
//
//							TLog.v(TAG, "parsing remote note");
//
//							insertNote(new Note(jsonNote));
//
//						} catch (JSONException e) {
//							TLog.e(TAG, e, "Problem parsing the server response");
//							SendMessage(NOTE_PULL_ERROR,
//									ErrorList.createErrorWithContents(
//											"JSON parsing", "json", e, rawResponse));
//							return;
//						}
//
//					} catch (java.net.UnknownHostException e) {
//						TLog.e(TAG, "Internet connection not available");
//						SendMessage(NO_INTERNET);
//						return;
//					}
//
//					SendMessage(NOTE_PULLED);
//				}
//			});
		}
		public void deleteAllNotes() {

			TLog.v(TAG, "Deleting Snowy notes");

			string userRef = Preferences.GetString(Preferences.Key.SYNC_SERVER_USER_API);
			
			long newRevision;
			
			if(latestLocalRevision > latestRemoteRevision)
				newRevision = latestLocalRevision+1;
			else
				newRevision = latestRemoteRevision+1;
			
//			SyncInThread(new Runnable() {
//
//				public void run() {
//
//					OAuthConnection auth = getAuthConnection();
//
//					try {
//						TLog.v(TAG, "contacting " + userRef);
//						string rawResponse = auth.get(userRef);
//						if (rawResponse == null) {
//							return;
//						}
//						try {
//							JSONObject response = new JSONObject(rawResponse);
//							string notesUrl = response.getJSONObject("notes-ref").GetString("api-ref");
//
//							TLog.v(TAG, "contacting " + notesUrl);
//							response = new JSONObject(auth.get(notesUrl));
//
//							JSONArray notes = response.getJSONArray("notes");
//							setSyncProgress(50);
//
//							TLog.v(TAG, "number of notes: {0}", notes.Length);
//							
//							List<string> guidList = new List<string>();
//
//							for (int i = 0; i < notes.Length; i++) {
//								JSONObject ajnote = notes.getJSONObject(i);
//								guidList.add(ajnote.GetString("guid"));
//							}
//
//							TLog.v(TAG, "creating JSON");
//
//							JSONObject data = new JSONObject();
//							data.put("latest-sync-revision",newRevision);
//							JSONArray Jnotes = new JSONArray();
//							for(string guid : guidList) {
//								JSONObject Jnote = new JSONObject();
//								Jnote.put("guid", guid);
//								Jnote.put("command","delete");
//								Jnotes.put(Jnote);
//							}
//							data.put("note-changes", Jnotes);
//
//							response = new JSONObject(auth.put(notesUrl,data.ToString()));
//
//							TLog.v(TAG, "delete response: {0}", response.ToString());
//
//							
//							// reset latest sync date so we can push our notes again
//							
//							latestRemoteRevision = (int)response.getLong("latest-sync-revision");
//							Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, latestRemoteRevision);
//							Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE,new Time().Format3339(false));
//							
//						} catch (JSONException e) {
//							TLog.e(TAG, e, "Problem parsing the server response");
//							SendMessage(PARSING_FAILED,
//									ErrorList.createErrorWithContents(
//											"JSON parsing", "json", e, rawResponse));
//							setSyncProgress(100);
//							return;
//						}
//					} catch (java.net.UnknownHostException e) {
//						TLog.e(TAG, "Internet connection not available");
//						SendMessage(NO_INTERNET);
//						setSyncProgress(100);
//						return;
//					}
//					SendMessage(REMOTE_NOTES_DELETED);
//				}
//			});
		}

		public override void backupNotes() {
			// TODO Auto-generated method stub
			
		}

		protected override void localSyncComplete() {
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, latestRemoteRevision);
		}
	}
}