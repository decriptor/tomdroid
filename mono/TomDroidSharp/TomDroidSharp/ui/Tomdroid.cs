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

//import java.io.File;
//import java.util.regex.Matcher;
//import java.util.regex.Pattern;

using TomDroidSharp;
using TomDroidSharp.R;
using TomDroidSharp.sync;
using TomDroidSharp.util;
using TomDroidSharp.ui.actionbar;
using TomDroidSharp.util;
using TomDroidSharp.xml;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Database;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Text;
using Android.Text.Util;
using Android.Text.Format;
using Android.Views;
using Android.Widget;

//import android.annotation.TargetApi;

namespace TomDroidSharp.ui
{
	[Activity (Label = "TomDroidSharp", MainLauncher = true)]
	public class Tomdroid : ActionBarListActivity {

		// Global definition for Tomdroid
		public static readonly string	AUTHORITY			= "org.tomdroidsharp.notes";
		public static readonly Uri		CONTENT_URI			= Uri.Parse("content://" + AUTHORITY + "/notes");
		public static readonly string	CONTENT_TYPE		= "vnd.android.cursor.dir/vnd.tomdroid.note";
		public static readonly string	CONTENT_ITEM_TYPE	= "vnd.android.cursor.item/vnd.tomdroid.note";
		public static readonly string	PROJECT_HOMEPAGE	= "http://www.launchpad.net/tomdroid/";
		public static readonly string CALLED_FROM_SHORTCUT_EXTRA = "org.tomdroidsharp.CALLED_FROM_SHORTCUT";
	    public static readonly string SHORTCUT_NAME = "org.tomdroidsharp.SHORTCUT_NAME";
		
	    private static readonly int DIALOG_SYNC = 0;
		private static readonly int DIALOG_ABOUT = 1;
		private static readonly int DIALOG_FIRST_RUN = 2;
		private static readonly int DIALOG_NOT_FOUND = 3;
		public static readonly int DIALOG_PARSE_ERROR = 4;
		private static readonly int DIALOG_REVERT_ALL = 5;
		private static readonly int DIALOG_AUTH_PROGRESS = 6;
		private static readonly int DIALOG_CONNECT_FAILED = 7;
		private static readonly int DIALOG_DELETE_NOTE = 8;
		private static readonly int DIALOG_REVERT_NOTE = 9;
		private static readonly int DIALOG_SYNC_ERRORS = 10;
		private static readonly int DIALOG_SEND_CHOOSE = 11;
		private static readonly int DIALOG_VIEW_TAGS = 12;
		private static readonly int DIALOG_NOT_FOUND_SHORTCUT = 13;

		private static string dialogstring;
		private static Note dialogNote;
		private static bool dialogBoolean;
		private static int dialogInt;
		private static int dialogInt2;
		private EditText dialogInput;
		private int dialogPosition;

		public int syncTotalNotes;
		public int syncProcessedNotes;

		// config parameters
		public static string	NOTES_PATH				= null;
		
		// Set this to false for release builds, the reason should be obvious
		public static readonly bool	CLEAR_PREFERENCES	= false;

		// Logging info
		private static readonly string	TAG					= "TomdroidSharp";

		public static Uri getNoteIntentUri(long noteId) {
	        return Uri.Parse(CONTENT_URI + "/" + noteId);
	    }

		private View main;
		
		// UI to data model glue
		private TextView			listEmptyView;
		private ListAdapter			adapter;

		// UI feedback handler
		private Handler	 syncMessageHandler	= new SyncMessageHandler(this);

		// sync variables
		private bool creating = true;
		private static ProgressDialog authProgressDialog;
		
		// UI for tablet
		private LinearLayout rightPane;
		private TextView content;
		private TextView title;
		
		// other tablet-based variables

		private Note note;
		private SpannablestringBuilder noteContent;
		private Uri uri;
		private int lastIndex = -1;
		public MenuItem syncMenuItem;
		public static Tomdroid context;

		// for searches
		
		private Intent intent;
		private string Query;
		
		/** Called when the activity is created. */
		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Preferences.init(this, CLEAR_PREFERENCES);
			context = this;
			SyncManager.setActivity(this);
			SyncManager.setHandler(this.syncMessageHandler);
			
	        main =  View.Inflate(this, R.layout.main, null);
			
	        SetContentView(main);
			
			// get the Path to the notes-folder from Preferences
			NOTES_PATH = Environment.getExternalStorageDirectory()
					+ "/" + Preferences.getstring(Preferences.Key.SD_LOCATION) + "/";
			
			// did we already show the warning and got destroyed by android's activity killer?
			if (Preferences.getBoolean(Preferences.Key.FIRST_RUN)) {
				TLog.i(TAG, "Tomdroid is first run.");
				
				// add a first explanatory note
				NoteManager.putNote(this, FirstNote.createFirstNote(this));
				
				// Warn that this is a "will eat your babies" release
				showDialog(DIALOG_FIRST_RUN);

			}
			
			this.intent = getIntent();

		    if (Intent.ACTION_SEARCH.equals(intent.getAction())) {
		    	this.setTitle(getstring(R.string.app_name) + " - " + getstring(R.string.SearchResultTitle));
		    	Query = intent.getstringExtra(SearchManager.Query);
		    	
		    	//adds Query to search history suggestions
		        SearchRecentSuggestions suggestions = new SearchRecentSuggestions(this,
		                SearchSuggestionProvider.AUTHORITY, SearchSuggestionProvider.MODE);
		        suggestions.saveRecentQuery(Query, null);
			}
		    
			string defaultSortOrder = Preferences.getstring(Preferences.Key.SORT_ORDER);
			NoteManager.setSortOrder(defaultSortOrder);
			
		    // set list adapter
		    updateNotesList(Query, -1);
		    
			// add note to pane for tablet
			rightPane = (LinearLayout) findViewById(R.id.right_pane);
			registerForContextMenu(findViewById(android.R.id.list));

			// check if receiving note
			if(getIntent().hasExtra("view_note")) {
				uri = getIntent().getData();
				getIntent().setData(null);
				Intent i = new Intent(Intent.ACTION_VIEW, uri, this, ViewNote);
				startActivity(i);
			}
			
			if(rightPane != null) {
				content = (TextView) findViewById(R.id.content);
				title = (TextView) findViewById(R.id.title);
				
				// this we will call on resume as well.
				updateTextAttributes();
				showNoteInPane(0);
			}
			
			// set the view shown when the list is empty
			updateEmptyList(Query);
		}

		@TargetApi(11)
		@Override
		public bool onCreateOptionsMenu(Menu menu) {

			// Create the menu based on what is defined in res/menu/main.xml
			MenuInflater inflater = getMenuInflater();
			inflater.inflate(R.menu.main, menu);

	    	string sortOrder = NoteManager.getSortOrder();
			if(sortOrder == null) {
				menu.findItem(R.id.menuSort).setTitle(R.string.sortByTitle);
			} else if(sortOrder.equals("sort_title")) {
				menu.findItem(R.id.menuSort).setTitle(R.string.sortByDate);
			} else {
				menu.findItem(R.id.menuSort).setTitle(R.string.sortByTitle);
			}

	        // Calling super after populating the menu is necessary here to ensure that the
	       	// action bar helpers have a chance to handle this event.
			return super.onCreateOptionsMenu(menu);
			
		}

		@Override
		public bool onOptionsItemSelected(MenuItem item) {
			switch (item.getItemId()) {
	        	case android.R.id.home:
	        		if (Intent.ACTION_SEARCH.equals(intent.getAction())) {
	        			// app icon in action bar clicked in search results; go home
	        			Intent intent = new Intent(this, Tomdroid.class);
	        			intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
	        			startActivity(intent);
	        		}
	        		return true;
				case R.id.menuAbout:
					showDialog(DIALOG_ABOUT);
					return true;
				case R.id.menuSync:
					startSyncing(true);
					return true;
				case R.id.menuNew:
					newNote();
					return true;
				case R.id.menuSort:
					string sortOrder = NoteManager.toggleSortOrder();
					if(sortOrder.equals("sort_title")) {
						item.setTitle(R.string.sortByDate);
					} else {
						item.setTitle(R.string.sortByTitle);
					}
					updateNotesList(Query, lastIndex);
					return true;
				case R.id.menuRevert:
					showDialog(DIALOG_REVERT_ALL);
					return true;
				case R.id.menuPrefs:
					startActivity(new Intent(this, PreferencesActivity.class));
					return true;
					
				case R.id.menuSearch:
					startSearch(null, false, null, false);
					return true;

				// tablet
				case R.id.menuEdit:
					if(note != null)
						startEditNote();
					return true;
				case R.id.menuDelete:
					if(note != null) {
				    	dialogstring = note.getGuid(); // why can't we put it in the bundle?  deletes the wrong note!?
						dialogInt = lastIndex;
						showDialog(DIALOG_DELETE_NOTE);
					}
					return true;
				case R.id.menuImport:
					// Create a new Intent for the file picker activity
					Intent intent = new Intent(this, FilePickerActivity.class);
					
					// Set the initial directory to be the sdcard
					//intent.putExtra(FilePickerActivity.EXTRA_FILE_PATH, Environment.getExternalStorageDirectory());
					
					// Show hidden files
					//intent.putExtra(FilePickerActivity.EXTRA_SHOW_HIDDEN_FILES, true);
					
					// Only make .png files visible
					//List<string> extensions = new List<string>();
					//extensions.add(".png");
					//intent.putExtra(FilePickerActivity.EXTRA_ACCEPTED_FILE_EXTENSIONS, extensions);
					
					// Start the activity
					startActivityForResult(intent, 5718);
					return true;
			}
			return super.onOptionsItemSelected(item);
		}

		
		@Override
		public void onCreateContextMenu(ContextMenu menu, View v,
				ContextMenuInfo menuInfo) {
			MenuInflater inflater = getMenuInflater();

			long noteId = ((AdapterContextMenuInfo)menuInfo).id;
			dialogPosition = ((AdapterContextMenuInfo)menuInfo).position;

			Uri intentUri = Uri.parse(Tomdroid.CONTENT_URI+"/"+noteId);
	        dialogNote = NoteManager.getNote(this, intentUri);
	        
	        if(dialogNote.getTags().contains("system:deleted"))
	        	inflater.inflate(R.menu.main_longclick_deleted, menu);
	        else
	        	inflater.inflate(R.menu.main_longclick, menu);
	        
		    menu.setHeaderTitle(getstring(R.string.noteOptions));
			super.onCreateContextMenu(menu, v, menuInfo);
		}

		@Override
		public bool onContextItemSelected(MenuItem item) {
			AdapterContextMenuInfo info = (AdapterContextMenuInfo)item.getMenuInfo();
			long noteId = info.id;

			Uri intentUri = Uri.parse(Tomdroid.CONTENT_URI+"/"+noteId);

	        switch (item.getItemId()) {
	            case R.id.menu_send:
	            	dialogstring = intentUri.tostring();
	            	showDialog(DIALOG_SEND_CHOOSE);
					return true;
				case R.id.view:
					this.ViewNote(noteId);
					break;
				case R.id.edit:
					this.startEditNote(noteId);
					break;
				case R.id.tags:
					showDialog(DIALOG_VIEW_TAGS);
					break;
				case R.id.revert:
					this.revertNote(note.getGuid());
					break;
				case R.id.delete:
					dialogstring = dialogNote.getGuid();
					dialogInt = dialogPosition;
					showDialog(DIALOG_DELETE_NOTE);
					return true;
				case R.id.undelete:
					undeleteNote(dialogNote);
					return true;
				case R.id.create_shortcut:
	                readonly NoteViewShortcutsHelper helper = new NoteViewShortcutsHelper(this);
	                sendBroadcast(helper.getBroadcastableCreateShortcutIntent(intentUri, dialogNote.getTitle()));
	                break;
				default:
					break;
			}
			
			return super.onContextItemSelected(item);
		}

		public void onResume() {
			super.onResume();
			Intent intent = this.getIntent();

			SyncService currentService = SyncManager.getInstance().getCurrentService();

			if (currentService.needsAuth() && intent != null) {
				Uri uri = intent.getData();

				if (uri != null && uri.getScheme().equals("tomdroid")) {
					TLog.i(TAG, "Got url : {0}", uri.tostring());
					
					showDialog(DIALOG_AUTH_PROGRESS);

					Handler handler = new Handler() {

						@Override
						public void handleMessage(Message msg) {
							if(authProgressDialog != null)
								authProgressDialog.dismiss();
							if(msg.what == SyncService.AUTH_COMPLETE)
								startSyncing(true);
						}

					};

					((ServiceAuth) currentService).remoteAuthComplete(uri, handler);
				}
			}

			SyncManager.setActivity(this);
			SyncManager.setHandler(this.syncMessageHandler);
			
			// tablet refresh
			if(rightPane != null) {
				updateTextAttributes();
				if(!creating)
					showNoteInPane(lastIndex);
			}
			else 
				updateNotesList(Query, lastIndex);
			
			// set the view shown when the list is empty
			updateEmptyList(Query);
			creating = false;
		}

		@Override
		protected Dialog onCreateDialog(int id) {
		    super.onCreateDialog (id);
		    readonly Activity activity = this;
			AlertDialog alertDialog;
			ProgressDialog progressDialog = new ProgressDialog(this);
			SyncService currentService = SyncManager.getInstance().getCurrentService();
			string serviceDescription = currentService.getDescription();
	    	AlertDialog.Builder builder = new AlertDialog.Builder(this);

			switch(id) {
			    case DIALOG_SYNC:
					progressDialog.setIndeterminate(true);
					progressDialog.setTitle(string.format(getstring(R.string.syncing),serviceDescription));
					progressDialog.setMessage(dialogstring);
					progressDialog.setOnCancelListener(new DialogInterface.OnCancelListener() {
		    			
						public void onCancel(DialogInterface dialog) {
							SyncManager.getInstance().cancel();
						}
						
					});
					progressDialog.setButton(ProgressDialog.BUTTON_NEGATIVE, getstring(R.string.cancel), new DialogInterface.OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							progressDialog.cancel();
						}
					});
			    	return progressDialog;
			    case DIALOG_AUTH_PROGRESS:
			    	authProgressDialog = new ProgressDialog(this);
			    	authProgressDialog.setTitle("");
			    	authProgressDialog.setMessage(getstring(R.string.prefSyncCompleteAuth));
			    	authProgressDialog.setIndeterminate(true);
			    	authProgressDialog.setCancelable(false);
			        return authProgressDialog;
			    case DIALOG_ABOUT:
					// grab version info
					string ver;
					try {
						ver = getPackageManager().getPackageInfo(getPackageName(), 0).versionName;
					} catch (NameNotFoundException e) {
						e.printStackTrace();
						ver = "Not found!";
						return null;
					}
			    	
			    	// format the string
					string aboutDialogFormat = getstring(R.string.strAbout);
					string aboutDialogStr = string.format(aboutDialogFormat, getstring(R.string.app_desc), // App description
							getstring(R.string.author), // Author name
							ver // Version
							);

					// build and show the dialog
					return new AlertDialog.Builder(this).setMessage(aboutDialogStr).setTitle(getstring(R.string.titleAbout))
							.setIcon(R.drawable.icon).setNegativeButton(getstring(R.string.btnProjectPage), new OnClickListener() {
								public void onClick(DialogInterface dialog, int which) {
									startActivity(new Intent(Intent.ACTION_VIEW, Uri
											.parse(Tomdroid.PROJECT_HOMEPAGE)));
									dialog.dismiss();
								}
							}).setPositiveButton(getstring(R.string.btnOk), new OnClickListener() {
								public void onClick(DialogInterface dialog, int which) {
									dialog.dismiss();
								}
							}).create();
			    case DIALOG_FIRST_RUN:
			    	return new AlertDialog.Builder(this).setMessage(getstring(R.string.strWelcome)).setTitle(
							getstring(R.string.titleWelcome)).setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							Preferences.putBoolean(Preferences.Key.FIRST_RUN, false);
							dialog.dismiss();
						}
					}).setIcon(R.drawable.icon).create();
			    case DIALOG_NOT_FOUND:
				    addCommonNoteNotFoundDialogElements(builder);
				    return builder.create();
			    case DIALOG_NOT_FOUND_SHORTCUT:
				    addCommonNoteNotFoundDialogElements(builder);
			        readonly Intent removeIntent = new NoteViewShortcutsHelper(this).getRemoveShortcutIntent(dialogstring, uri);
			        builder.setPositiveButton(getstring(R.string.btnRemoveShortcut), new OnClickListener() {
			            public void onClick(DialogInterface dialogInterface, readonly int i) {
			                sendBroadcast(removeIntent);
			                finish();
			            }
			        });
				    return builder.create();
			    case DIALOG_PARSE_ERROR:
			    	return new AlertDialog.Builder(this)
					.setMessage(getstring(R.string.messageErrorNoteParsing))
					.setTitle(getstring(R.string.error))
					.setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							showNote(true);
						}})
					.create();
			    case DIALOG_REVERT_ALL:
			    	return new AlertDialog.Builder(this)
			        .setIcon(android.R.drawable.ic_dialog_alert)
			        .setTitle(R.string.revert_notes)
			        .setMessage(R.string.revert_notes_message)
			    	.setPositiveButton(getstring(R.string.yes), new OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
			        		Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
			        		Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().Format3339(false));
			            	startSyncing(false);
			           }

			        })
			        .setNegativeButton(R.string.no, null)
			        .create();
			    case DIALOG_CONNECT_FAILED:
			    	return new AlertDialog.Builder(this)
					.setMessage(getstring(R.string.prefSyncConnectionFailed))
					.setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							dialog.dismiss();
						}})
					.create();
			    case DIALOG_DELETE_NOTE:
			    	return new AlertDialog.Builder(this)
			        .setIcon(android.R.drawable.ic_dialog_alert)
			        .setTitle(R.string.delete_note)
			        .setMessage(R.string.delete_message)
			        .setPositiveButton(getstring(R.string.yes), null)
			        .setNegativeButton(R.string.no, null)
			        .create();
			    case DIALOG_REVERT_NOTE:
			    	return new AlertDialog.Builder(this)
			        .setIcon(android.R.drawable.ic_dialog_alert)
			        .setTitle(R.string.revert_note)
			        .setMessage(R.string.revert_note_message)
			        .setPositiveButton(getstring(R.string.yes), null)
			        .setNegativeButton(R.string.no, null)
			        .create();
			    case DIALOG_SYNC_ERRORS:
			    	return new AlertDialog.Builder(activity)
					.setTitle(getstring(R.string.error))
			    	.setMessage(dialogstring)
			        .setPositiveButton(getstring(R.string.yes), null)
					.setNegativeButton(getstring(R.string.close), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) { finishSync(); }
					}).create();
			    case DIALOG_SEND_CHOOSE:
	                readonly Uri intentUri = Uri.parse(dialogstring);
	                return new AlertDialog.Builder(activity)
					.setMessage(getstring(R.string.sendChoice))
					.setTitle(getstring(R.string.sendChoiceTitle))
			        .setPositiveButton(getstring(R.string.btnSendAsFile), null)
					.setNegativeButton(getstring(R.string.btnSendAsText), null)
					.create();
			    case DIALOG_VIEW_TAGS:
			    	dialogInput = new EditText(this);
			    	return new AlertDialog.Builder(activity)
			    	.setMessage(getstring(R.string.edit_tags))
			    	.setTitle(string.format(getstring(R.string.note_x_tags),dialogNote.getTitle()))
			    	.setView(dialogInput)
			    	.setNegativeButton(R.string.btnCancel, new DialogInterface.OnClickListener() {
						public void onClick(DialogInterface dialog, int whichButton) {
							removeDialog(DIALOG_VIEW_TAGS);
						}
			    	})
			    	.setPositiveButton(R.string.btnOk, null)
			    	.create();
			    default:
			    	alertDialog = null;
			    }
			return alertDialog;
		}

		@Override
		protected void onPrepareDialog(int id, readonly Dialog dialog) {
		    super.onPrepareDialog (id, dialog);
		    readonly Activity activity = this;
		    switch(id) {
		    	case DIALOG_SYNC:
					SyncService currentService = SyncManager.getInstance().getCurrentService();
					string serviceDescription = currentService.getDescription();
		    		((ProgressDialog) dialog).setTitle(string.format(getstring(R.string.syncing),serviceDescription));
		    		((ProgressDialog) dialog).setMessage(dialogstring);
		    		((ProgressDialog) dialog).setOnCancelListener(new DialogInterface.OnCancelListener() {
		    			
						public void onCancel(DialogInterface dialog) {
							SyncManager.getInstance().cancel();
						}
						
					});
		    		break;
			    case DIALOG_NOT_FOUND_SHORTCUT:
			        readonly Intent removeIntent = new NoteViewShortcutsHelper(this).getRemoveShortcutIntent(dialogstring, uri);
			        ((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.btnRemoveShortcut), new OnClickListener() {
			            public void onClick( DialogInterface dialogInterface, readonly int i) {
			                sendBroadcast(removeIntent);
			                finish();
			            }
			        });
			        break;
			    case DIALOG_REVERT_ALL:
			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.yes), new OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
			        		Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
			        		Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().Format3339(false));
			            	startSyncing(false);
			           }

			        });
				    break;
			    case DIALOG_DELETE_NOTE:
			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.yes), new OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
			        		deleteNote(dialogstring, dialogInt);
			           }

			        });
				    break;
			    case DIALOG_REVERT_NOTE:
			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.yes), new OnClickListener() {

			            public void onClick(DialogInterface dialog, int which) {
							SyncManager.getInstance().pullNote(dialogstring);
			           }

			        });
				    break;
			    case DIALOG_SYNC_ERRORS:
			    	((AlertDialog) dialog).setMessage(dialogstring);
			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.btnSavetoSD), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							if(!dialogBoolean) {
								Toast.makeText(activity, activity.getstring(R.string.messageCouldNotSave),
										Toast.LENGTH_SHORT).show();
							}
							finishSync();
						}
					});
				    break;
			    case DIALOG_SEND_CHOOSE:
	                readonly Uri intentUri = Uri.parse(dialogstring);
			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.btnSendAsFile), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) {
							(new Send(activity, intentUri, true)).send();

						}
					});
			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_NEGATIVE, getstring(R.string.btnSendAsText), new OnClickListener() {
						public void onClick(DialogInterface dialog, int which) { 
			                (new Send(activity, intentUri, false)).send();
						}
					});
				    break;
			    case DIALOG_VIEW_TAGS:
			    	((AlertDialog) dialog).setTitle(string.format(getstring(R.string.note_x_tags),dialogNote.getTitle()));
			    	dialogInput.setText(dialogNote.getTags());

			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, getstring(R.string.btnOk), new DialogInterface.OnClickListener() {
			    		public void onClick(DialogInterface dialog, int whichButton) {
			    			string value = dialogInput.getText().tostring();
				    		dialogNote.setTags(value);
				    		dialogNote.setLastChangeDate();
							NoteManager.putNote(activity, dialogNote);
							removeDialog(DIALOG_VIEW_TAGS);
			    		}
			    	});
			    	break;
			}
		}
		
		@Override
		protected void onListItemClick(ListView l, View v, int position, long id) {
			super.onListItemClick(l, v, position, id);
			if (rightPane != null) {
				if(position == lastIndex) // same index, edit
					this.startEditNote();
				else
					showNoteInPane(position);
			}
			else {
				Cursor item = (Cursor) adapter.getItem(position);
				long noteId = item.getInt(item.getColumnIndexOrThrow(Note.ID));
					this.ViewNote(noteId);
			}
		}
		
		// called when rotating screen
		@Override
		public void onConfigurationChanged(Configuration newConfig)
		{
		    super.onConfigurationChanged(newConfig);
	        main =  View.inflate(this, R.layout.main, null);
	        SetContentView(main);

	        if (Integer.parseInt(Build.VERSION.SDK) >= 11) {
	            Honeycomb.invalidateOptionsMenuWrapper(this); 
	        }
			
			registerForContextMenu(findViewById(android.R.id.list));

			// add note to pane for tablet
			rightPane = (LinearLayout) findViewById(R.id.right_pane);
			
			if(rightPane != null) {
				content = (TextView) findViewById(R.id.content);
				title = (TextView) findViewById(R.id.title);
				updateTextAttributes();
				showNoteInPane(lastIndex);
			}
			else
				updateNotesList(Query,-1);
			
			// set the view shown when the list is empty
			updateEmptyList(Query);
		}

		private void updateNotesList(string aquery, int aposition) {
		    // adapter that binds the ListView UI to the notes in the note manager
			adapter = NoteManager.getListAdapter(this, aquery, rightPane != null ? aposition : -1);
			setListAdapter(adapter);
		}
		
		private void updateEmptyList(string aquery) {
			// set the view shown when the list is empty
			listEmptyView = (TextView) findViewById(R.id.list_empty);
			if (rightPane == null) {
				if (aquery != null) {
					listEmptyView.setText(getstring(R.string.strNoResults, aquery)); }
				else if (adapter.getCount() != 0) {
					listEmptyView.setText(getstring(R.string.strListEmptyWaiting)); }
				else {
					listEmptyView.setText(getstring(R.string.strListEmptyNoNotes)); }
			} else {
				if (aquery != null) {
					title.setText(getstring(R.string.strNoResults, aquery)); }
				else if (adapter.getCount() != 0) {
					title.setText(getstring(R.string.strListEmptyWaiting)); }
				else {
					title.setText(getstring(R.string.strListEmptyNoNotes)); }
			}
			getListView().setEmptyView(listEmptyView);
		}
		
		private void updateTextAttributes() {
			float baseSize = Float.parseFloat(Preferences.getstring(Preferences.Key.BASE_TEXT_SIZE));
			content.setTextSize(baseSize);
			title.setTextSize(baseSize*1.3f);

			title.setTextColor(Color.BLUE);
			title.setPaintFlags(title.getPaintFlags() | Paint.UNDERLINE_TEXT_FLAG);
			title.setBackgroundColor(0xffffffff);

			content.setBackgroundColor(0xffffffff);
			content.setTextColor(Color.DKGRAY);
		}
		private void showNoteInPane(int position) {
			if(rightPane == null)
				return;
			
			if(position == -1)
				position = 0;

	        title.setText("");
	        content.setText("");
			
	     // save index and top position

	        int index = getListView().getFirstVisiblePosition();
	        View v = getListView().getChildAt(0);
	        int top = (v == null) ? 0 : v.getTop();

	        updateNotesList(Query, position);

	    // restore
		
			getListView().setSelectionFromTop(index, top);
			
			if(position >= adapter.getCount())
				position = 0;
			
			Cursor item = (Cursor) adapter.getItem(position);
			if (item == null || item.getCount() == 0) {
	            TLog.d(TAG, "Index {0} not found in list", position);
				return;
			}
			TLog.d(TAG, "Getting note {0}", position);

			long noteId = item.getInt(item.getColumnIndexOrThrow(Note.ID));	
			uri = Uri.parse(CONTENT_URI + "/" + noteId);

	        note = NoteManager.getNote(this, uri);
			TLog.v(TAG, "Note guid: {0}", note.getGuid());

	        if(note != null) {
	        	TLog.d(TAG, "note {0} found", position);
	            noteContent = new NoteContentBuilder().setCaller(noteContentHandler).setInputSource(note.getXmlContent()).setTitle(note.getTitle()).build();
	    		lastIndex = position;
	        } else {
	            TLog.d(TAG, "The note {0} doesn't exist", uri);
			    readonly bool proposeShortcutRemoval;
			    readonly bool calledFromShortcut = getIntent().getBooleanExtra(CALLED_FROM_SHORTCUT_EXTRA, false);
			    readonly string shortcutName = getIntent().getstringExtra(SHORTCUT_NAME);
			    proposeShortcutRemoval = calledFromShortcut && uri != null && shortcutName != null;
			
			    if (proposeShortcutRemoval) {
			    	dialogstring = shortcutName;
		            showDialog(DIALOG_NOT_FOUND_SHORTCUT);
			    }
			    else
		            showDialog(DIALOG_NOT_FOUND);

	        }
		}
		private void showNote(bool xml) {
			
			if(xml) {
				content.setText(note.getXmlContent());
				title.setText((CharSequence) note.getTitle());
				this.setTitle(this.getTitle() + " - XML");
				return;
			}

			LinkInternalSpan[] links = noteContent.getSpans(0, noteContent.length(), LinkInternalSpan.class);
			MatchFilter noteLinkMatchFilter = LinkInternalSpan.getNoteLinkMatchFilter(noteContent, links);

			// show the note (spannable makes the TextView able to output styled text)
			content.setText(noteContent, TextView.BufferType.SPANNABLE);

			// add links to stuff that is understood by Android except phone numbers because it's too aggressive
			// TODO this is SLOWWWW!!!!
			
			int linkFlags = 0;
			
			if(Preferences.getBoolean(Preferences.Key.LINK_EMAILS))
				linkFlags |= Linkify.EMAIL_ADDRESSES;
			if(Preferences.getBoolean(Preferences.Key.LINK_URLS))
				linkFlags |= Linkify.WEB_URLS;
			if(Preferences.getBoolean(Preferences.Key.LINK_ADDRESSES))
				linkFlags |= Linkify.MAP_ADDRESSES;
			
			Linkify.addLinks(content, linkFlags);

			// Custom phone number linkifier (fixes lp:512204)
			if(Preferences.getBoolean(Preferences.Key.LINK_PHONES))
				Linkify.addLinks(content, LinkifyPhone.PHONE_PATTERN, "tel:", LinkifyPhone.sPhoneNumberMatchFilter, Linkify.sPhoneNumberTransformFilter);

			// This will create a link every time a note title is found in the text.
			// The pattern contains a very dumb (title1)|(title2) escaped correctly
			// Then we transform the url from the note name to the note id to avoid characters that mess up with the URI (ex: ?)
			if(Preferences.getBoolean(Preferences.Key.LINK_TITLES)) {
				Pattern pattern = NoteManager.buildNoteLinkifyPattern(this, note.getTitle());
		
				if(pattern != null) {
					Linkify.addLinks(
						content,
						pattern,
						Tomdroid.CONTENT_URI+"/",
						noteLinkMatchFilter,
						noteTitleTransformFilter
					);
		
					// content.setMovementMethod(LinkMovementMethod.getInstance());
				}
			}
			title.setText((CharSequence) note.getTitle());
		}
		
		private void addCommonNoteNotFoundDialogElements(AlertDialog.Builder builder) {
		    builder.setMessage(getstring(R.string.messageNoteNotFound))
		            .setTitle(getstring(R.string.titleNoteNotFound))
		            .setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
		                public void onClick(DialogInterface dialog, int which) {
		                    dialog.dismiss();
		                    finish();
		                }
		            });
		}	
		
		private Handler noteContentHandler = new Handler() {
		
			@Override
			public void handleMessage(Message msg) {
		
				//parsed ok - show
				if(msg.what == NoteContentBuilder.PARSE_OK) {
					showNote(false);
		
				//parsed not ok - error
				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {
		
					showDialog(DIALOG_PARSE_ERROR);
		    	}
			}
		};

		// custom transform filter that takes the note's title part of the URI and translate it into the note id
		// this was done to avoid problems with invalid characters in URI (ex: ? is the Query separator but could be in a note title)
		public TransformFilter noteTitleTransformFilter = new TransformFilter() {
		
			public string transformUrl(Matcher m, string str) {
		
				int id = NoteManager.getNoteId(Tomdroid.this, str);
		
				// return something like content://org.tomdroid.notes/notes/3
				return Tomdroid.CONTENT_URI.tostring()+"/"+id;
			}
		};
		
		@SuppressWarnings("deprecation")
		private void startSyncing(bool push) {

			string serverUri = Preferences.getstring(Preferences.Key.SYNC_SERVER);
			SyncService currentService = SyncManager.getInstance().getCurrentService();
			
			if (currentService.needsAuth()) {
		
				// service needs authentication
				TLog.i(TAG, "Creating dialog");

				showDialog(DIALOG_AUTH_PROGRESS);
		
				Handler handler = new Handler() {
		
					@Override
					public void handleMessage(Message msg) {
		
						bool wasSuccessful = false;
						Uri authorizationUri = (Uri) msg.obj;
						if (authorizationUri != null) {
		
							Intent i = new Intent(Intent.ACTION_VIEW, authorizationUri);
							startActivity(i);
							wasSuccessful = true;
		
						} else {
							// Auth failed, don't update the value
							wasSuccessful = false;
						}
		
						if (authProgressDialog != null)
							authProgressDialog.dismiss();
		
						if (wasSuccessful) {
							resetLocalDatabase();
						} else {
							showDialog(DIALOG_CONNECT_FAILED);
						}
					}
				};

				((ServiceAuth) currentService).getAuthUri(serverUri, handler);
			}
			else {
				syncProcessedNotes = 0;
				syncTotalNotes = 0;
				dialogstring = getstring(R.string.syncing_connect);
		        showDialog(DIALOG_SYNC);
		        SyncManager.getInstance().startSynchronization(push); // push by default
			}
		}
		
		//TODO use LocalStorage wrapper from two-way-sync branch when it get's merged
		private void resetLocalDatabase() {
			ContentResolver.delete(Tomdroid.CONTENT_URI, null, null);
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
			
			// first explanatory note will be deleted on sync
			//NoteManager.putNote(this, FirstNote.createFirstNote());
		}

		public void ViewNote(long noteId) {
			Uri intentUri = getNoteIntentUri(noteId);
			Intent i = new Intent(Intent.ACTION_VIEW, intentUri, this, ViewNote.class);
			startActivity(i);
		}
		
		protected void startEditNote() {
			 Intent i = new Intent(Intent.ACTION_VIEW, uri, this, EditNote.class);
			startActivity(i);
		}
		
		protected void startEditNote(long noteId) {
			Uri intentUri = getNoteIntentUri(noteId);
			 Intent i = new Intent(Intent.ACTION_VIEW, intentUri, this, EditNote.class);
			startActivity(i);
		}

		public void newNote() {
			
			// add a new note
			
			Note note = NewNote.createNewNote(this, "", "");
			Uri uri = NoteManager.putNote(this, note);
			
			// recreate listAdapter
			
			updateNotesList(Query, 0);

			// show new note and update list

			showNoteInPane(0);
			
			// view new note
			
			Intent i = new Intent(Intent.ACTION_VIEW, uri, this, EditNote.class);
			startActivity(i);

			
		}
		private void deleteNote(string guid, int position) {
			NoteManager.deleteNote(this, guid);
			showNoteInPane(position);
		}
		
		private void undeleteNote(Note anote) {
			NoteManager.undeleteNote(this, anote);
			updateNotesList(Query,lastIndex);
		}
			
		@SuppressWarnings("deprecation")
		private void revertNote( string guid) {
			dialogstring = guid;
			showDialog(DIALOG_REVERT_NOTE);
		}

		public class SyncMessageHandler string  Handler {
		
			private Activity activity;
			
			public SyncMessageHandler(Activity activity) {
				this.activity = activity;
			}
		
			@Override
			public void handleMessage(Message msg) {
		
				SyncService currentService = SyncManager.getInstance().getCurrentService();
				string serviceDescription = currentService.getDescription();
				string message = "";
				bool dismiss = false;

				switch (msg.what) {
					case SyncService.AUTH_COMPLETE:
						message = getstring(R.string.messageAuthComplete);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.AUTH_FAILED:
						dismiss = true;
						message = getstring(R.string.messageAuthFailed);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.PARSING_COMPLETE:
						 ErrorList errors = (ErrorList)msg.obj;
						if(errors == null || errors.isEmpty()) {
							message = getstring(R.string.messageSyncComplete);
							message = string.format(message,serviceDescription);
							Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
							finishSync();
						} else {
							TLog.v(TAG, "syncErrors: {0}", TextUtils.join("\n",errors.toArray()));
							dialogstring = getstring(R.string.messageSyncError);
							dialogBoolean = errors.save();
							showDialog(DIALOG_SYNC_ERRORS);
						}
						break;
					case SyncService.CONNECTING_FAILED:
						dismiss = true;
						message = getstring(R.string.messageSyncConnectingFailed);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.PARSING_FAILED:
						dismiss = true;
						message = getstring(R.string.messageSyncParseFailed);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.PARSING_NO_NOTES:
						dismiss = true;
						message = getstring(R.string.messageSyncNoNote);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
						
					case SyncService.NO_INTERNET:
						dismiss = true;
						Toast.makeText(activity, getstring(R.string.messageSyncNoConnection),Toast.LENGTH_SHORT).show();
						break;
						
					case SyncService.NO_SD_CARD:
						dismiss = true;
						Toast.makeText(activity, activity.getstring(R.string.messageNoSDCard),
								Toast.LENGTH_SHORT).show();
						break;
					case SyncService.SYNC_CONNECTED:
						dialogstring = getstring(R.string.gettings_notes);
						showDialog(DIALOG_SYNC);
						break;
					case SyncService.BEGIN_PROGRESS:
						syncTotalNotes = msg.arg1;
						syncProcessedNotes = 0;
						dialogstring = getstring(R.string.syncing_local);
						showDialog(DIALOG_SYNC);
						break;
					case SyncService.SYNC_PROGRESS:
						if(msg.arg1 == 90) {
							dialogstring = getstring(R.string.syncing_remote);						
							showDialog(DIALOG_SYNC);
						}
						break;
					case SyncService.NOTE_DELETED:
						message = getstring(R.string.messageSyncNoteDeleted);
						message = string.format(message,serviceDescription);
						//Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
		
					case SyncService.NOTE_PUSHED:
						message = getstring(R.string.messageSyncNotePushed);
						message = string.format(message,serviceDescription);
						//Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();

						break;
					case SyncService.NOTE_PULLED:
						message = getstring(R.string.messageSyncNotePulled);
						message = string.format(message,serviceDescription);
						//Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
															
					case SyncService.NOTE_DELETE_ERROR:
						dismiss = true;
						Toast.makeText(activity, activity.getstring(R.string.messageSyncNoteDeleteError), Toast.LENGTH_SHORT).show();
						break;
		
					case SyncService.NOTE_PUSH_ERROR:
						dismiss = true;
						Toast.makeText(activity, activity.getstring(R.string.messageSyncNotePushError), Toast.LENGTH_SHORT).show();
						break;
					case SyncService.NOTE_PULL_ERROR:
						dismiss = true;
						message = getstring(R.string.messageSyncNotePullError);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
					case SyncService.IN_PROGRESS:
						Toast.makeText(activity, activity.getstring(R.string.messageSyncAlreadyInProgress), Toast.LENGTH_SHORT).show();
						dismiss = true;
						break;
					case SyncService.NOTES_BACKED_UP:
						Toast.makeText(activity, activity.getstring(R.string.messageNotesBackedUp), Toast.LENGTH_SHORT).show();
						break;
					case SyncService.SYNC_CANCELLED:
						dismiss = true;
						message = getstring(R.string.messageSyncCancelled);
						message = string.format(message,serviceDescription);
						Toast.makeText(activity, message, Toast.LENGTH_SHORT).show();
						break;
					default:
						break;
		
				}
				if(dismiss)
					removeDialog(DIALOG_SYNC);
			}
		}

		protected void  onActivityResult (int requestCode, int resultCode, Intent  data) {
			TLog.d(TAG, "onActivityResult called with result {0}", resultCode);
			
			// returning from file picker
			if(data != null && data.hasExtra(FilePickerActivity.EXTRA_FILE_PATH)) {
				// Get the file path
				File f = new File(data.getstringExtra(FilePickerActivity.EXTRA_FILE_PATH));
				Uri noteUri = Uri.fromFile(f);
				Intent intent = new Intent(this, Receive.class);
				intent.setData(noteUri);
				startActivity(intent);
			}
			else { // returning from sync conflict
				SyncService currentService = SyncManager.getInstance().getCurrentService();
				currentService.resolvedConflict(requestCode);			
			}
		}
		
		public void finishSync() {
			TLog.v(TAG, "Finishing Sync");
			
			removeDialog(DIALOG_SYNC);
			
			if(rightPane != null)
				showNoteInPane(lastIndex);
		}
	}
}