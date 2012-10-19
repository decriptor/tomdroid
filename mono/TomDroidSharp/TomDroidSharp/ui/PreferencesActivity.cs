/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009, Benoit Garret <benoit.garret_launchpad@gadz.org>
 * Copyright 2010, 2011 Olivier Bilodeau <olivier@bottomlesspit.org>
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
using Android.OS;
using Android.Preferences;
using Android.Text.Format;
using Android.Webkit;
using Android.Widget;

using TomDroidSharp.ui.actionbar;
using Android.Views;
using TomDroidSharp.util;
using System.Collections.Generic;

namespace TomDroidSharp.ui
{
	[Activity (Label = "PreferencesActivity")]
	public class PreferencesActivity : ActionBarPreferenceActivity {
		
		private static readonly string TAG = "PreferencesActivity";
		
	    private static readonly int DIALOG_SYNC = 0;
	    private static readonly int DIALOG_DELETE = 1;
	    private static readonly int DIALOG_DEL_REMOTE = 2;
	    private static readonly int DIALOG_BACKUP = 3;
	    private static readonly int DIALOG_CONNECT_FAILED = 4;
	    private static readonly int DIALOG_FOLDER_ERROR = 5;
	    private static readonly int DIALOG_INVALID_ENTRY = 6;
	    
	    private string dialogstring;
		
		// TODO: put the various preferences in fields and figure out what to do on activity suspend/resume
		private EditTextPreference baseSize = null;
		private ListPreference defaultSort = null;
		private EditTextPreference syncServer = null;
		private ListPreference syncService = null;
		private EditTextPreference sdLocation = null;
		private Preference delNotes = null;
		private Preference clearSearchHistory = null;
		private Preference backupNotes = null;
		private Preference delRemoteNotes = null;
		private Preference autoBackup = null;

		private Activity activity;

		private Handler preferencesMessageHandler = new PreferencesMessageHandler(this);



		private static ProgressDialog syncProgressDialog;

		
		//@SuppressWarnings("deprecation")
		protected override void onCreate(Bundle savedInstanceState)
		{
			if (Build.VERSION.SdkInt < 11)
				requestWindowFeature(Window.FEATURE_CUSTOM_TITLE); // added for actionbarcompat
			
			base.OnCreate(savedInstanceState);
			
			this.activity = this;
			SyncManager.setActivity(this);
			SyncManager.setHandler(this.preferencesMessageHandler);
			
			addPreferencesFromResource(Resource.xml.preferences);
			
			// Fill the Preferences fields
			baseSize = FindPreference<EditTextPreference>(Preferences.Key.BASE_TEXT_SIZE.getName());
			defaultSort = FindPreference<ListPreference>(Preferences.Key.SORT_ORDER.getName());
			syncServer = FindPreference<EditTextPreference>(Preferences.Key.SYNC_SERVER.getName());
			syncService = FindPreference<ListPreference>(Preferences.Key.SYNC_SERVICE.getName());
			sdLocation = FindPreference<EditTextPreference>(Preferences.Key.SD_LOCATION.getName());
			clearSearchHistory = FindPreference<Preference>(Preferences.Key.CLEAR_SEARCH_HISTORY.getName());
			delNotes = FindPreference<Preference>(Preferences.Key.DEL_ALL_NOTES.getName());
			delRemoteNotes = FindPreference<Preference>(Preferences.Key.DEL_REMOTE_NOTES.getName());
			backupNotes = FindPreference<Preference>(Preferences.Key.BACKUP_NOTES.getName());
			autoBackup = FindPreference<Preference>(Preferences.Key.AUTO_BACKUP_NOTES.getName());
			
			// Set the default values if nothing Exists
			setDefaults();
			
			// Fill the services combo list
			fillServices();
			
			// Fill the services combo list
			fillSortOrders();
			
			// Enable or disable the server field depending on the selected sync service
			setServer(syncService.getValue());
			
//			syncService.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
//				
//				public bool onPreferenceChange(Preference preference, Object newValue) {
//					string selectedSyncServiceKey = (string)newValue;
//					
//					// did the selection change?
//					if (!syncService.getValue().contentEquals(selectedSyncServiceKey)) {
//						TLog.d(TAG, "preference change triggered");
//						
//						syncServiceChanged(selectedSyncServiceKey);
//					}
//					return true;
//				}
//			});
	 		
			// force reauthentication if the sync server changes
//			syncServer.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
//
//				public bool onPreferenceChange(Preference preference,
//						Object serverUri) {
//					
//					if (serverUri == null) {
//						Toast.MakeText(PreferencesActivity.this,
//								GetString(Resource.String.prefServerEmpty),
//								ToastLength.Short).Show();
//						return false;
//					}
//					
//					if (!URLUtil.isValidUrl(serverUri.ToString())){
//						noValidEntry(serverUri.ToString());
//						return false;
//					}
//					syncServer.setSummary((string)serverUri);
//					
//					// TODO is this necessary? hasn't it changed already?
//					Preferences.putstring(Preferences.Key.SYNC_SERVER, (string)serverUri);
//
//					reauthenticate();
//					return true;
//				}
//				
//			});
			
			// Change the Folder Location
//			sdLocation.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
//
//				public bool onPreferenceChange(Preference preference, Object locationUri) {
//
//					if (locationUri.equals(Preferences.GetString(Preferences.Key.SD_LOCATION))) { 
//						return false;
//					}
//					if ((locationUri.ToString().contains("\t")) || (locationUri.ToString().contains("\n"))) { 
//						noValidEntry(locationUri.ToString());
//						return false;
//					}
//					
//					File path = new File(Environment.getExternalStorageDirectory()
//							+ "/" + locationUri + "/");
//
//					if(!path.Exists()) {
//						TLog.w(TAG, "Folder {0} does not exist.", path);
//						folderNotExisting(path.ToString());
//						return false;
//					}
//					
//					Preferences.putstring(Preferences.Key.SD_LOCATION, locationUri.ToString());
//					TLog.d(TAG, "Changed Folder to: " + path.ToString());
//
//					Tomdroid.NOTES_PATH = path.ToString();
//					sdLocation.setSummary(Tomdroid.NOTES_PATH);
//
//					resetLocalDatabase();
//					return true;
//				}
//			});
			
			//delete Search History
//			clearSearchHistory.setOnPreferenceClickListener(new OnPreferenceClickListener() {
//		        public bool onPreferenceClick(Preference preference) {
//		            SearchRecentSuggestions suggestions = new SearchRecentSuggestions(PreferencesActivity.this,
//		                    SearchSuggestionProvider.AUTHORITY, SearchSuggestionProvider.MODE);
//		            suggestions.clearHistory();
//		            	
//		        	Toast.MakeText(getBaseContext(),
//	                        GetString(Resource.String.deletedSearchHistory),
//	                        Toast.LENGTH_LONG).Show();
//		        	TLog.d(TAG, "Deleted search history.");
//		        	
//		        	return true;
//		        }
//		    });

//			baseSize.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
				
//				public bool onPreferenceChange(Preference preference, Object newValue) {
//					try {
//						Float.parseFloat((string)newValue);
//						Preferences.putstring(Preferences.Key.BASE_TEXT_SIZE, (string)newValue);
//					}
//					catch(Exception e) {
//			        	Toast.MakeText(getBaseContext(),
//		                        GetString(Resource.String.illegalTextSize),
//		                        Toast.LENGTH_LONG).Show();
//			        	TLog.e(TAG, "Illegal text size in preferences");
//			        	return false;
//					}
//					baseSize.setSummary((string)newValue);
//					return true;
//				}
//			});
//			defaultSort.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
//				
//				public bool onPreferenceChange(Preference preference, Object newValue) {
//					string value = (string) newValue;
//					if(value.equals("sort_title"))
//						defaultSort.setSummary(GetString(Resource.String.sortByTitle));
//					else
//						defaultSort.setSummary(GetString(Resource.String.sortByDate));
//					return true;
//				}
//			});
//			delNotes.setOnPreferenceClickListener(new OnPreferenceClickListener() {
//				
//		        public bool onPreferenceClick(Preference preference) {
//		        	ShowDialog(DIALOG_DELETE);
//					return true;
//				}
//			});

//			delRemoteNotes.setOnPreferenceClickListener(new OnPreferenceClickListener() {
//				
//		        public bool onPreferenceClick(Preference preference) {
//		        	ShowDialog(DIALOG_DEL_REMOTE);
//					return true;
//				}
//			});
//			
//			
//			backupNotes.setOnPreferenceClickListener(new OnPreferenceClickListener() {
//				
//		        public bool onPreferenceClick(Preference preference) {
//		        	ShowDialog(DIALOG_BACKUP);
//
//					return true;
//				}
//			});		
		}

		private void reauthenticate() {

			// don't do anything, we'll authenticate on sync instead
			// save empty config, wiping old config
			
			OAuthConnection auth = new OAuthConnection();
			auth.saveConfiguration();
		}
		
		private void fillServices()
		{
			List<SyncService> availableServices = SyncManager.getInstance().getServices();
			CharSequence[] entries = new CharSequence[availableServices.Count];
			CharSequence[] entryValues = new CharSequence[availableServices.Count];
			
			for (int i = 0; i < availableServices.Count; i++) {
				entries[i] = availableServices.get(i).getDescription();
				entryValues[i] = availableServices.get(i).getName();
			}
			
			syncService.setEntries(entries);
			syncService.setEntryValues(entryValues);

		}
		
		private void fillSortOrders()
		{
			CharSequence[] entries = new CharSequence[] {GetString(Resource.String.prefSortDate), GetString(Resource.String.prefSortTitle)};
			CharSequence[] entryValues = new CharSequence[] {"sort_date", "sort_title"};
			
			defaultSort.setEntries(entries);
			defaultSort.setEntryValues(entryValues);

		}
		
		private void setDefaults()
		{
			string defaultServer = (string)Preferences.Key.SYNC_SERVER.getDefault();
			syncServer.setDefaultValue(defaultServer);
			if(syncServer.getText() == null)
				syncServer.SetText(defaultServer);
			syncServer.setSummary(Preferences.GetString(Preferences.Key.SYNC_SERVER));

			string defaultService = (string)Preferences.Key.SYNC_SERVICE.getDefault();
			syncService.setDefaultValue(defaultService);
			if(syncService.getValue() == null)
				syncService.setValue(defaultService);
			
			string defaultLocation = (string)Preferences.Key.SD_LOCATION.getDefault();
			sdLocation.setDefaultValue(defaultLocation);
			if(sdLocation.getText() == null)
				sdLocation.SetText(defaultLocation);

			string defaultSize = (string)Preferences.Key.BASE_TEXT_SIZE.getDefault();
			baseSize.setDefaultValue(defaultSize);
			baseSize.setSummary(Preferences.GetString(Preferences.Key.BASE_TEXT_SIZE));
			if(baseSize.getText() == null)
				baseSize.SetText(defaultSize);
			
			string defaultOrder = (string)Preferences.Key.SORT_ORDER.getDefault();
			string sortOrder = Preferences.GetString(Preferences.Key.SORT_ORDER);
			defaultSort.setDefaultValue(defaultOrder);
			if(defaultSort.getValue() == null)
				defaultSort.setValue(defaultOrder);
			if(sortOrder.equals("sort_title"))
				defaultSort.setSummary(GetString(Resource.String.sortByTitle));
			else
				defaultSort.setSummary(GetString(Resource.String.sortByDate));
		}

		private void setServer(string syncServiceKey) {

			SyncManager.getInstance();
			SyncService service = SyncManager.getService(syncServiceKey);

			if (service == null)
				return;

			syncServer.setEnabled(service.needsServer());
			syncService.setSummary(service.getDescription());
			backupNotes.setEnabled(!service.needsLocation()); // if not using sd card, allow backup
			autoBackup.setEnabled(!service.needsLocation()); // if not using sd card, allow backup
			sdLocation.setSummary(Tomdroid.NOTES_PATH);
		}
			
		private void folderNotExisting(string path) {
			dialogstring = string.Format(GetString(Resource.String.prefFolderCreated), path);
			ShowDialog(DIALOG_FOLDER_ERROR);
		}
		
		private void noValidEntry(string entry) {
			dialogstring = string.Format(GetString(Resource.String.prefNoValidEntry), entry);
			ShowDialog(DIALOG_FOLDER_ERROR);
		}

		//TODO use LocalStorage wrapper from two-way-sync branch when it get's merged
		private void resetLocalDatabase() {
			ContentResolver.Delete(Tomdroid.CONTENT_URI, null, null);
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
			Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().Format3339(false));
			
			// add a first explanatory note
			NoteManager.putNote(this, FirstNote.createFirstNote(this));
			
			string text = GetString(Resource.String.messageDatabaseReset);
			Toast.MakeText(activity, text, ToastLength.Short).Show();
		}
		
		private void resetRemoteService() {
			ShowDialog(DIALOG_SYNC);
			SyncManager.getInstance().getCurrentService().deleteAllNotes();
		}
		
		/**
		 * Housekeeping when a syncServer changes
		 * @param syncServiceKey - key of the new sync service 
		 */
		private void syncServiceChanged(string syncServiceKey) {
			
			setServer(syncServiceKey);
			
			// TODO this should be refactored further, notice that setServer performs the same operations 
			
			SyncManager.getInstance();
			if (SyncManager.getService(syncServiceKey) == null)
				return;
			
			// not resetting database, since now we may have new notes, and want to move them from one service to another, etc.
			
			// reset last sync date, so we can push local notes to the service - to pull instead, we have "revert all"
			
			Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().Format3339(false));
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);

		}

		public class PreferencesMessageHandler : Handler {
			
			private Activity activity;
			
			public PreferencesMessageHandler(Activity activity) {
				this.activity = activity;
			}
		
			public override void handleMessage(Message msg) {
		
				string serviceDescription = SyncManager.getInstance().getCurrentService().getDescription();
				string text = "";

				TLog.d(TAG, "PreferencesMessageHandler message: {0}",msg.What);

				switch (msg.What) {
					case SyncService.REMOTE_NOTES_DELETED:
						text = GetString(Resource.String.messageRemoteNotesDeleted);
						text = string.Format(text,serviceDescription);
						Toast.MakeText(activity, text, ToastLength.Short).Show();
						break;
					case SyncService.NOTES_BACKED_UP:
						text = GetString(Resource.String.messageNotesBackedUp);
						Toast.MakeText(activity, text, ToastLength.Short).Show();
						break;
					case SyncService.NOTES_RESTORED:
						text = GetString(Resource.String.messageNotesRestored);
						Toast.MakeText(activity, text, ToastLength.Short).Show();
						break;
				}
				syncProgressDialog.Dismiss();
			}
		}

		public override bool onOptionsItemSelected(IMenuItem item) {
			if(item.ItemId == Resource.Id.home) {
		        	// app icon in action bar clicked; go home
	                Intent intent = new Intent(this, typeof(Tomdroid));
	                intent.AddFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
	                StartActivity(intent);
	            	return true;
			}
			return base.OnOptionsItemSelected(item);
		}
		
		protected Dialog OnCreateDialog(int id) {
		    Dialog dialog;
	    	AlertDialog alertDialog; 
		    switch(id) {
			    case DIALOG_SYNC:
					string serviceDescription = SyncManager.getInstance().getCurrentService().getDescription();
					syncProgressDialog = new ProgressDialog(this);
					syncProgressDialog.setTitle(string.format(GetString(Resource.String.syncing),serviceDescription));
					syncProgressDialog.setMessage(GetString(Resource.String.syncing_connect));
					syncProgressDialog.setProgressStyle(ProgressDialog.STYLE_HORIZONTAL);
//					syncProgressDialog.setButton(ProgressDialog.BUTTON_NEGATIVE, GetString(Resource.String.cancel), new DialogInterface.OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							syncProgressDialog.cancel();
//						}
//					});
//					syncProgressDialog.setOnCancelListener(new DialogInterface.OnCancelListener() {
//
//						public void onCancel(DialogInterface dialog) {
//							SyncManager.getInstance().cancel();
//						}
//						
//					});
			        syncProgressDialog.setIndeterminate(true);
			        return syncProgressDialog;
			    case DIALOG_DELETE:
			    	alertDialog = new AlertDialog.Builder(this)
			        .setIcon(android.Resource.Drawable.ic_dialog_alert)
			        .setTitle(Resource.String.delete_all)
			        .setMessage(Resource.String.delete_all_message)
//			        .setPositiveButton(Resource.String.yes, new DialogInterface.OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//			            	resetLocalDatabase();
//			           }
//
//			        })
			        .setNegativeButton(Resource.String.no, null)
			        .create();
			        return alertDialog;

			    case DIALOG_DEL_REMOTE:
					alertDialog = new AlertDialog.Builder(this)
			        .setIcon(android.Resource.Drawable.ic_dialog_alert)
			        .setTitle(Resource.String.delete_remote_notes)
			        .setMessage(Resource.String.delete_remote_notes_message)
//			        .setPositiveButton(Resource.String.yes, new DialogInterface.OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//			            	resetRemoteService();
//			           }
//
//			        })
			        .setNegativeButton(Resource.String.no, null)
			        .create();
					return alertDialog;
					
			    case DIALOG_BACKUP:
					alertDialog = new AlertDialog.Builder(activity)
			        .setIcon(android.Resource.Drawable.ic_dialog_alert)
			        .setTitle(Resource.String.backup_notes_title)
			        .setMessage(Resource.String.backup_notes)
//			        .setPositiveButton(Resource.String.yes, new DialogInterface.OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//			        		ShowDialog(DIALOG_SYNC);
//			            	SyncManager.getService("sdcard").backupNotes();
//			           }
//
//			        })
			        .setNegativeButton(Resource.String.no, null)
			        .create();
					return alertDialog;
			    case DIALOG_CONNECT_FAILED:
					alertDialog = new AlertDialog.Builder(this)
					.setMessage(GetString(Resource.String.prefSyncConnectionFailed))
//					.setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							dialog.dismiss();
//						}})
					.create();
					return alertDialog;
					
			    case DIALOG_FOLDER_ERROR:
			    case DIALOG_INVALID_ENTRY:
					alertDialog = new AlertDialog.Builder(this)
					.setTitle(GetString(Resource.String.error))
					.setMessage(dialogstring)
//					.setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							dialog.dismiss();
//						}})
					.create();
					return alertDialog;
			    default:
			        dialog = null;
			    }
		    return dialog;
		}
	}
}