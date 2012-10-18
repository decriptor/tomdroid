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

using TomDroidSharp.NoteManager;
using TomDroidSharp.R;
using TomDroidSharp.sync.SyncManager;
using TomDroidSharp.sync.SyncService;
using TomDroidSharp.sync.web.OAuthConnection;
using TomDroidSharp.ui.actionbar.ActionBarPreferenceActivity;
using TomDroidSharp.util.FirstNote;
using TomDroidSharp.util.Preferences;
using TomDroidSharp.util.SearchSuggestionProvider;
using TomDroidSharp.util.TLog;

//import java.io.File;
//import java.util.ArrayList;

namespace TomDroidSharp.ui
{

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

		private Handler	 preferencesMessageHandler	= new PreferencesMessageHandler(this);



		private static ProgressDialog syncProgressDialog;

		
		@SuppressWarnings("deprecation")
		@Override
		protected void onCreate(Bundle savedInstanceState) {
			if (Build.VERSION.SDK_INT < 11)
				requestWindowFeature(Window.FEATURE_CUSTOM_TITLE); // added for actionbarcompat
			
			super.onCreate(savedInstanceState);
			
			this.activity = this;
			SyncManager.setActivity(this);
			SyncManager.setHandler(this.preferencesMessageHandler);
			
			addPreferencesFromResource(R.xml.preferences);
			
			// Fill the Preferences fields
			baseSize = (EditTextPreference)findPreference(Preferences.Key.BASE_TEXT_SIZE.getName());
			defaultSort = (ListPreference)findPreference(Preferences.Key.SORT_ORDER.getName());
			syncServer = (EditTextPreference)findPreference(Preferences.Key.SYNC_SERVER.getName());
			syncService = (ListPreference)findPreference(Preferences.Key.SYNC_SERVICE.getName());
			sdLocation = (EditTextPreference)findPreference(Preferences.Key.SD_LOCATION.getName());
			clearSearchHistory = (Preference)findPreference(Preferences.Key.CLEAR_SEARCH_HISTORY.getName());
			delNotes = (Preference)findPreference(Preferences.Key.DEL_ALL_NOTES.getName());
			delRemoteNotes = (Preference)findPreference(Preferences.Key.DEL_REMOTE_NOTES.getName());
			backupNotes = (Preference)findPreference(Preferences.Key.BACKUP_NOTES.getName());
			autoBackup = (Preference)findPreference(Preferences.Key.AUTO_BACKUP_NOTES.getName());
			
			// Set the default values if nothing exists
			setDefaults();
			
			// Fill the services combo list
			fillServices();
			
			// Fill the services combo list
			fillSortOrders();
			
			// Enable or disable the server field depending on the selected sync service
			setServer(syncService.getValue());
			
			syncService.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
				
				public boolean onPreferenceChange(Preference preference, Object newValue) {
					string selectedSyncServiceKey = (string)newValue;
					
					// did the selection change?
					if (!syncService.getValue().contentEquals(selectedSyncServiceKey)) {
						TLog.d(TAG, "preference change triggered");
						
						syncServiceChanged(selectedSyncServiceKey);
					}
					return true;
				}
			});
	 		
			// force reauthentication if the sync server changes
			syncServer.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {

				public boolean onPreferenceChange(Preference preference,
						Object serverUri) {
					
					if (serverUri == null) {
						Toast.makeText(PreferencesActivity.this,
								getstring(R.string.prefServerEmpty),
								Toast.LENGTH_SHORT).show();
						return false;
					}
					
					if (!URLUtil.isValidUrl(serverUri.tostring())){
						noValidEntry(serverUri.tostring());
						return false;
					}
					syncServer.setSummary((string)serverUri);
					
					// TODO is this necessary? hasn't it changed already?
					Preferences.putstring(Preferences.Key.SYNC_SERVER, (string)serverUri);

					reauthenticate();
					return true;
				}
				
			});
			
			// Change the Folder Location
			sdLocation.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {

				public boolean onPreferenceChange(Preference preference, Object locationUri) {

					if (locationUri.equals(Preferences.getstring(Preferences.Key.SD_LOCATION))) { 
						return false;
					}
					if ((locationUri.tostring().contains("\t")) || (locationUri.tostring().contains("\n"))) { 
						noValidEntry(locationUri.tostring());
						return false;
					}
					
					File path = new File(Environment.getExternalStorageDirectory()
							+ "/" + locationUri + "/");

					if(!path.exists()) {
						TLog.w(TAG, "Folder {0} does not exist.", path);
						folderNotExisting(path.tostring());
						return false;
					}
					
					Preferences.putstring(Preferences.Key.SD_LOCATION, locationUri.tostring());
					TLog.d(TAG, "Changed Folder to: " + path.tostring());

					Tomdroid.NOTES_PATH = path.tostring();
					sdLocation.setSummary(Tomdroid.NOTES_PATH);

					resetLocalDatabase();
					return true;
				}
			});
			
			//delete Search History
			clearSearchHistory.setOnPreferenceClickListener(new OnPreferenceClickListener() {
		        public boolean onPreferenceClick(Preference preference) {
		            SearchRecentSuggestions suggestions = new SearchRecentSuggestions(PreferencesActivity.this,
		                    SearchSuggestionProvider.AUTHORITY, SearchSuggestionProvider.MODE);
		            suggestions.clearHistory();
		            	
		        	Toast.makeText(getBaseContext(),
	                        getstring(R.string.deletedSearchHistory),
	                        Toast.LENGTH_LONG).show();
		        	TLog.d(TAG, "Deleted search history.");
		        	
		        	return true;
		        }
		    });

			baseSize.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
				
				public boolean onPreferenceChange(Preference preference, Object newValue) {
					try {
						Float.parseFloat((string)newValue);
						Preferences.putstring(Preferences.Key.BASE_TEXT_SIZE, (string)newValue);
					}
					catch(Exception e) {
			        	Toast.makeText(getBaseContext(),
		                        getstring(R.string.illegalTextSize),
		                        Toast.LENGTH_LONG).show();
			        	TLog.e(TAG, "Illegal text size in preferences");
			        	return false;
					}
					baseSize.setSummary((string)newValue);
					return true;
				}
			});
			defaultSort.setOnPreferenceChangeListener(new OnPreferenceChangeListener() {
				
				public boolean onPreferenceChange(Preference preference, Object newValue) {
					string value = (string) newValue;
					if(value.equals("sort_title"))
						defaultSort.setSummary(getstring(R.string.sortByTitle));
					else
						defaultSort.setSummary(getstring(R.string.sortByDate));
					return true;
				}
			});
			delNotes.setOnPreferenceClickListener(new OnPreferenceClickListener() {
				
		        public boolean onPreferenceClick(Preference preference) {
		        	showDialog(DIALOG_DELETE);
					return true;
				}
			});

			delRemoteNotes.setOnPreferenceClickListener(new OnPreferenceClickListener() {
				
		        public boolean onPreferenceClick(Preference preference) {
		        	showDialog(DIALOG_DEL_REMOTE);
					return true;
				}
			});
			
			
			backupNotes.setOnPreferenceClickListener(new OnPreferenceClickListener() {
				
		        public boolean onPreferenceClick(Preference preference) {
		        	showDialog(DIALOG_BACKUP);

					return true;
				}
			});		
		}

		private void reauthenticate() {

			// don't do anything, we'll authenticate on sync instead
			// save empty config, wiping old config
			
			OAuthConnection auth = new OAuthConnection();
			auth.saveConfiguration();
		}
		
		private void fillServices()
		{
			ArrayList<SyncService> availableServices = SyncManager.getInstance().getServices();
			CharSequence[] entries = new CharSequence[availableServices.size()];
			CharSequence[] entryValues = new CharSequence[availableServices.size()];
			
			for (int i = 0; i < availableServices.size(); i++) {
				entries[i] = availableServices.get(i).getDescription();
				entryValues[i] = availableServices.get(i).getName();
			}
			
			syncService.setEntries(entries);
			syncService.setEntryValues(entryValues);

		}
		
		private void fillSortOrders()
		{
			final CharSequence[] entries = new CharSequence[] {getstring(R.string.prefSortDate), getstring(R.string.prefSortTitle)};
			final CharSequence[] entryValues = new CharSequence[] {"sort_date", "sort_title"};
			
			defaultSort.setEntries(entries);
			defaultSort.setEntryValues(entryValues);

		}
		
		private void setDefaults()
		{
			string defaultServer = (string)Preferences.Key.SYNC_SERVER.getDefault();
			syncServer.setDefaultValue(defaultServer);
			if(syncServer.getText() == null)
				syncServer.setText(defaultServer);
			syncServer.setSummary(Preferences.getstring(Preferences.Key.SYNC_SERVER));

			string defaultService = (string)Preferences.Key.SYNC_SERVICE.getDefault();
			syncService.setDefaultValue(defaultService);
			if(syncService.getValue() == null)
				syncService.setValue(defaultService);
			
			string defaultLocation = (string)Preferences.Key.SD_LOCATION.getDefault();
			sdLocation.setDefaultValue(defaultLocation);
			if(sdLocation.getText() == null)
				sdLocation.setText(defaultLocation);

			string defaultSize = (string)Preferences.Key.BASE_TEXT_SIZE.getDefault();
			baseSize.setDefaultValue(defaultSize);
			baseSize.setSummary(Preferences.getstring(Preferences.Key.BASE_TEXT_SIZE));
			if(baseSize.getText() == null)
				baseSize.setText(defaultSize);
			
			string defaultOrder = (string)Preferences.Key.SORT_ORDER.getDefault();
			string sortOrder = Preferences.getstring(Preferences.Key.SORT_ORDER);
			defaultSort.setDefaultValue(defaultOrder);
			if(defaultSort.getValue() == null)
				defaultSort.setValue(defaultOrder);
			if(sortOrder.equals("sort_title"))
				defaultSort.setSummary(getstring(R.string.sortByTitle));
			else
				defaultSort.setSummary(getstring(R.string.sortByDate));
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
			dialogstring = string.format(getstring(R.string.prefFolderCreated), path);
			showDialog(DIALOG_FOLDER_ERROR);
		}
		
		private void noValidEntry(string entry) {
			dialogstring = string.format(getstring(R.string.prefNoValidEntry), entry);
			showDialog(DIALOG_FOLDER_ERROR);
		}

		//TODO use LocalStorage wrapper from two-way-sync branch when it get's merged
		private void resetLocalDatabase() {
			getContentResolver().delete(Tomdroid.CONTENT_URI, null, null);
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
			Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().format3339(false));
			
			// add a first explanatory note
			NoteManager.putNote(this, FirstNote.createFirstNote(this));
			
			string text = getstring(R.string.messageDatabaseReset);
			Toast.makeText(activity, text, Toast.LENGTH_SHORT).show();
		}
		
		private void resetRemoteService() {
			showDialog(DIALOG_SYNC);
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
			
			Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().format3339(false));
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);

		}

		public class PreferencesMessageHandler string  Handler {
			
			private Activity activity;
			
			public PreferencesMessageHandler(Activity activity) {
				this.activity = activity;
			}
		
			@Override
			public void handleMessage(Message msg) {
		
				string serviceDescription = SyncManager.getInstance().getCurrentService().getDescription();
				string text = "";

				TLog.d(TAG, "PreferencesMessageHandler message: {0}",msg.what);

				switch (msg.what) {
					case SyncService.REMOTE_NOTES_DELETED:
						text = getstring(R.string.messageRemoteNotesDeleted);
						text = string.format(text,serviceDescription);
						Toast.makeText(activity, text, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.NOTES_BACKED_UP:
						text = getstring(R.string.messageNotesBackedUp);
						Toast.makeText(activity, text, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.NOTES_RESTORED:
						text = getstring(R.string.messageNotesRestored);
						Toast.makeText(activity, text, Toast.LENGTH_SHORT).show();
						break;
				}
				syncProgressDialog.dismiss();
			}
		}

		@Override
		public boolean onOptionsItemSelected(MenuItem item) {
			if(item.getItemId() == android.R.id.home) {
		        	// app icon in action bar clicked; go home
	                Intent intent = new Intent(this, Tomdroid.class);
	                intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
	                startActivity(intent);
	            	return true;
			}
			return super.onOptionsItemSelected(item);
		}
		
		protected Dialog onCreateDialog(int id) {
		    Dialog dialog;
	    	AlertDialog alertDialog; 
		    switch(id) {
			    case DIALOG_SYNC:
					string serviceDescription = SyncManager.getInstance().getCurrentService().getDescription();
					syncProgressDialog = new ProgressDialog(this);
					syncProgressDialog.setTitle(string.format(getstring(R.string.syncing),serviceDescription));
					syncProgressDialog.setMessage(getstring(R.string.syncing_connect));
					syncProgressDialog.setProgressStyle(ProgressDialog.STYLE_HORIZONTAL);
					syncProgressDialog.setButton(ProgressDialog.BUTTON_NEGATIVE, getstring(R.string.cancel), new DialogInterface.OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							syncProgressDialog.cancel();
						}
					});
					syncProgressDialog.setOnCancelListener(new DialogInterface.OnCancelListener() {

						public void onCancel(DialogInterface dialog) {
							SyncManager.getInstance().cancel();
						}
						
					});
			        syncProgressDialog.setIndeterminate(true);
			        return syncProgressDialog;
			    case DIALOG_DELETE:
			    	alertDialog = new AlertDialog.Builder(this)
			        .setIcon(android.R.drawable.ic_dialog_alert)
			        .setTitle(R.string.delete_all)
			        .setMessage(R.string.delete_all_message)
			        .setPositiveButton(R.string.yes, new DialogInterface.OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
			            	resetLocalDatabase();
			           }

			        })
			        .setNegativeButton(R.string.no, null)
			        .create();
			        return alertDialog;

			    case DIALOG_DEL_REMOTE:
					alertDialog = new AlertDialog.Builder(this)
			        .setIcon(android.R.drawable.ic_dialog_alert)
			        .setTitle(R.string.delete_remote_notes)
			        .setMessage(R.string.delete_remote_notes_message)
			        .setPositiveButton(R.string.yes, new DialogInterface.OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
			            	resetRemoteService();
			           }

			        })
			        .setNegativeButton(R.string.no, null)
			        .create();
					return alertDialog;
					
			    case DIALOG_BACKUP:
					alertDialog = new AlertDialog.Builder(activity)
			        .setIcon(android.R.drawable.ic_dialog_alert)
			        .setTitle(R.string.backup_notes_title)
			        .setMessage(R.string.backup_notes)
			        .setPositiveButton(R.string.yes, new DialogInterface.OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
			        		showDialog(DIALOG_SYNC);
			            	SyncManager.getService("sdcard").backupNotes();
			           }

			        })
			        .setNegativeButton(R.string.no, null)
			        .create();
					return alertDialog;
			    case DIALOG_CONNECT_FAILED:
					alertDialog = new AlertDialog.Builder(this)
					.setMessage(getstring(R.string.prefSyncConnectionFailed))
					.setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							dialog.dismiss();
						}})
					.create();
					return alertDialog;
					
			    case DIALOG_FOLDER_ERROR:
			    case DIALOG_INVALID_ENTRY:
					alertDialog = new AlertDialog.Builder(this)
					.setTitle(getstring(R.string.error))
					.setMessage(dialogstring)
					.setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							dialog.dismiss();
						}})
					.create();
					return alertDialog;
			    default:
			        dialog = null;
			    }
		    return dialog;
		}
	}
}