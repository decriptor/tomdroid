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

using TomDroidSharp.ui.actionbar;
using TomDroidSharp.util;
using System.Text;
using TomDroidSharp.Sync;
using TomDroidSharp.Util;
using TomDroidSharp.xml;
using Java.IO;

namespace TomDroidSharp.ui
{
	[Activity (Label = "TomDroidSharp", MainLauncher = true)]
	public class Tomdroid : ActionBarListActivity
	{
	
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
		public static string NOTES_PATH = null;
		
		// Set this to false for release builds, the reason should be obvious
		public static readonly bool	CLEAR_PREFERENCES = false;

		// Logging info
		private const string TAG = "TomDroidSharp";

		public static Uri getNoteIntentUri(long noteId) {
	        return Uri.Parse(CONTENT_URI + "/" + noteId);
	    }

		private View main;
		
		// UI to data model glue
		private TextView listEmptyView;
		private IListAdapter adapter;

		// UI feedback handler
		private Handler syncMessageHandler	= new SyncMessageHandler(this);

		// sync variables
		private bool creating = true;
		private static ProgressDialog authProgressDialog;
		
		// UI for tablet
		private LinearLayout rightPane;
		private TextView content;
		private TextView title;
		
		// other tablet-based variables

		private Note note;
		private StringBuilder noteContent;
		private Uri uri;
		private int lastIndex = -1;
		public IMenuItem syncMenuItem;
		public static Tomdroid context;

		// for searches
		
		private Intent intent;
		private string Query;
		
		/** Called when the activity is created. */
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Preferences.init(this, CLEAR_PREFERENCES);
			context = this;
			SyncManager.setActivity(this);
			SyncManager.setHandler(this.syncMessageHandler);
			
	        main =  View.Inflate(this, Resource.Layout.main, null);
			
	        SetContentView(main);
			
			// get the Path to the notes-folder from Preferences
			NOTES_PATH = Environment.ExternalStorageDirectory
					+ "/" + Preferences.GetString(Preferences.Key.SD_LOCATION) + "/";
			
			// did we already show the warning and got destroyed by android's activity killer?
			if (Preferences.GetBoolean(Preferences.Key.FIRST_RUN)) {
				TLog.i(TAG, "Tomdroid is first run.");
				
				// add a first explanatory note
				NoteManager.putNote(this, FirstNote.createFirstNote(this));
				
				// Warn that this is a "will eat your babies" release
				ShowDialog(DIALOG_FIRST_RUN);
			}
			
			this.intent = Intent;

		    if (Intent.ActionSearch.Equals(intent.Action)) {
		    	this.SetTitle(GetString(Resource.String.app_name) + " - " + GetString(Resource.String.SearchResultTitle));
		    	Query = intent.GetStringExtra(SearchManager.Query);
		    	
		    	//adds Query to search history suggestions
		        SearchRecentSuggestions suggestions = new SearchRecentSuggestions(this, SearchSuggestionProvider.AUTHORITY,
				                                                                  		SearchSuggestionProvider.MODE);
		        suggestions.SaveRecentQuery(Query, null);
			}
		    
			string defaultSortOrder = Preferences.GetString(Preferences.Key.SORT_ORDER);
			NoteManager.setSortOrder(defaultSortOrder);
			
		    // set list adapter
		    updateNotesList(Query, -1);
		    
			// add note to pane for tablet
			rightPane = FindViewById<LinearLayout>(Resource.Id.right_pane);
			RegisterForContextMenu(FindViewById(Resource.Id.list));

			// check if receiving note
			if(Intent.HasExtra("view_note")) {
				uri = Intent.Data;
				Intent.Data = null;
				Intent i = new Intent(Intent.ActionView, uri, this, ViewNote);
				StartActivity(i);
			}
			
			if(rightPane != null) {
				content = (TextView) FindViewById(Resource.Id.content);
				title = (TextView) FindViewById(Resource.Id.title);
				
				// this we will call on resume as well.
				updateTextAttributes();
				showNoteInPane(0);
			}
			
			// set the view shown when the list is empty
			updateEmptyList(Query);
		}

		//@TargetApi(11)
		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			// Create the menu based on what is defined in res/menu/main.xml
			MenuInflater inflater = getMenuInflater();
			inflater.Inflate(Resource.menu.main, menu);

	    	string sortOrder = NoteManager.getSortOrder();
			if(sortOrder == null) {
				menu.FindItem(Resource.Id.menuSort).SetTitle(Resource.String.sortByTitle);
			} else if(sortOrder.Equals("sort_title")) {
				menu.FindItem(Resource.Id.menuSort).SetTitle(Resource.String.sortByDate);
			} else {
				menu.FindItem(Resource.Id.menuSort).SetTitle(Resource.String.sortByTitle);
			}

	        // Calling base after populating the menu is necessary here to ensure that the
	       	// action bar helpers have a chance to handle this event.
			return base.onCreateOptionsMenu(menu);
			
		}

		public override void OnOptionsMenuClosed (IMenu menu)
		{
			switch (item.ItemId) {
	        	case Resource.Id.home:
	        		if (Intent.ACTION_SEARCH.Equals(intent.Action)) {
	        			// app icon in action bar clicked in search results; go home
	        			Intent intent = new Intent(this, typeof(Tomdroid));
	        			intent.AddFlags(ActivityFlags.ClearTop);
	        			StartActivity(intent);
	        		}
	        		return true;
				case Resource.Id.menuAbout:
					ShowDialog(DIALOG_ABOUT);
					return true;
				case Resource.Id.menuSync:
					StartSyncing(true);
					return true;
				case Resource.Id.menuNew:
					newNote();
					return true;
				case Resource.Id.menuSort:
					string sortOrder = NoteManager.toggleSortOrder();
					if(sortOrder.Equals("sort_title")) {
						item.SetTitle(Resource.String.sortByDate);
					} else {
						item.SetTitle(Resource.String.sortByTitle);
					}
					updateNotesList(Query, lastIndex);
					return true;
				case Resource.Id.menuRevert:
					ShowDialog(DIALOG_REVERT_ALL);
					return true;
				case Resource.Id.menuPrefs:
					StartActivity(new Intent(this, typeof(PreferencesActivity)));
					return true;
					
				case Resource.Id.menuSearch:
					StartSearch(null, false, null, false);
					return true;

				// tablet
				case Resource.Id.menuEdit:
					if(note != null)
						startEditNote();
					return true;
				case Resource.Id.menuDelete:
					if(note != null) {
				    	dialogstring = note.getGuid(); // why can't we put it in the bundle?  deletes the wrong note!?
						dialogInt = lastIndex;
						ShowDialog(DIALOG_DELETE_NOTE);
					}
					return true;
				case Resource.Id.menuImport:
					// Create a new Intent for the file picker activity
					Intent intent = new Intent(this, typeof(FilePickerActivity));
					
					// Set the initial directory to be the sdcard
					//intent.PutExtra(FilePickerActivity.EXTRA_FILE_PATH, Environment.getExternalStorageDirectory());
					
					// Show hidden files
					//intent.PutExtra(FilePickerActivity.EXTRA_SHOW_HIDDEN_FILES, true);
					
					// Only make .png files visible
					//List<string> extensions = new List<string>();
					//extensions.add(".png");
					//intent.PutExtra(FilePickerActivity.EXTRA_ACCEPTED_FILE_EXTENSIONS, extensions);
					
					// Start the activity
					StartActivityForResult(intent, 5718);
					return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		public override void OnCreateContextMenu (IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
		{
			MenuInflater inflater = getMenuInflater();

			long noteId = ((AdapterContextMenuInfo)menuInfo).id;
			dialogPosition = ((AdapterContextMenuInfo)menuInfo).position;

			Uri intentUri = Uri.Parse(Tomdroid.CONTENT_URI+"/"+noteId);
	        dialogNote = NoteManager.getNote(this, intentUri);
	        
	        if(dialogNote.getTags().Contains("system:deleted"))
	        	inflater.Inflate(Resource.menu.main_longclick_deleted, menu);
	        else
	        	inflater.Inflate(Resource.menu.main_longclick, menu);
	        
			menu.SetHeaderTitle(GetString(Resource.String.noteOptions));
			base.OnCreateContextMenu(menu, v, menuInfo);
		}

		public override bool OnContextItemSelected (IMenuItem item)
		{
			AdapterContextMenuInfo info = (AdapterContextMenuInfo)item.MenuInfo;
			long noteId = info.id;

			Uri intentUri = Uri.Parse(Tomdroid.CONTENT_URI+"/"+noteId);

	        switch (item.ItemId) {
	            case Resource.Id.menu_send:
	            	dialogstring = intentUri.ToString();
	            	ShowDialog(DIALOG_SEND_CHOOSE);
					return true;
				case Resource.Id.view:
					this.ViewNote(noteId);
					break;
				case Resource.Id.edit:
					this.startEditNote(noteId);
					break;
				case Resource.Id.tags:
					ShowDialog(DIALOG_VIEW_TAGS);
					break;
				case Resource.Id.revert:
					this.revertNote(note.getGuid());
					break;
				case Resource.Id.delete:
					dialogstring = dialogNote.getGuid();
					dialogInt = dialogPosition;
					ShowDialog(DIALOG_DELETE_NOTE);
					return true;
				case Resource.Id.undelete:
					undeleteNote(dialogNote);
					return true;
				case Resource.Id.create_shortcut:
	                NoteViewShortcutsHelper helper = new NoteViewShortcutsHelper(this);
	                SendBroadcast(helper.getBroadcastableCreateShortcutIntent(intentUri, dialogNote.getTitle()));
	                break;
				default:
					break;
			}
			
			return base.OnContextItemSelected(item);
		}

		protected override void OnResume()
		{
			base.OnResume();
			Intent intent = this.Intent;

			SyncService currentService = SyncManager.getInstance().getCurrentService();

			if (currentService.needsAuth() && intent != null) {
				Uri uri = intent.Data;

				if (uri != null && uri.Scheme.Equals("tomdroid")) {
					TLog.i(TAG, "Got url : {0}", uri.ToString());
					
					ShowDialog(DIALOG_AUTH_PROGRESS);

//					Handler handler = new Handler() {

//						public override void handleMessage(Message msg) {
//							if(authProgressDialog != null)
//								authProgressDialog.dismiss();
//							if(msg.what == SyncService.AUTH_COMPLETE)
//								startSyncing(true);
//						}
//
//					};

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

		protected override Dialog OnCreateDialog(int id)
		{
		    base.OnCreateDialog (id);
		    Activity activity = this;
			AlertDialog alertDialog;
			ProgressDialog progressDialog = new ProgressDialog(this);
			SyncService currentService = SyncManager.getInstance().getCurrentService();
			string serviceDescription = currentService.getDescription();
	    	AlertDialog.Builder builder = new AlertDialog.Builder(this);

			switch(id) {
			    case DIALOG_SYNC:
					progressDialog.SetIndeterminate(true);
					progressDialog.SetTitle(string.Format(GetString(Resource.String.syncing),serviceDescription));
					progressDialog.SetMessage(dialogstring);
//					progressDialog.setOnCancelListener(new DialogInterface.OnCancelListener() {
//		    			
//						public void onCancel(DialogInterface dialog) {
//							SyncManager.getInstance().cancel();
//						}
//						
//					});
//					progressDialog.setButton(ProgressDialog.BUTTON_NEGATIVE, GetString(Resource.String.cancel), new DialogInterface.OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							progressDialog.cancel();
//						}
//					});
			    	return progressDialog;
			    case DIALOG_AUTH_PROGRESS:
			    	authProgressDialog = new ProgressDialog(this);
			    	authProgressDialog.SetTitle("");
			    	authProgressDialog.SetMessage(GetString(Resource.String.prefSyncCompleteAuth));
			    	authProgressDialog.SetIndeterminate(true);
			    	authProgressDialog.SetCancelable(false);
			        return authProgressDialog;
			    case DIALOG_ABOUT:
					// grab version info
					string ver;
					try {
					ver = PackageManager.GetPackageInfo(PackageName, 0).VersionName;
					} catch (NameNotFoundException e) {
						e.PrintStackTrace();
						ver = "Not found!";
						return null;
					}
			    	
			    	// format the string
					string aboutDialogFormat = GetString(Resource.String.strAbout);
					string aboutDialogStr = string.Format(aboutDialogFormat, GetString(Resource.String.app_desc), // App description
							GetString(Resource.String.author), // Author name
							ver // Version
							);

					// build and show the dialog
					var ad = new AlertDialog.Builder(this).SetMessage(aboutDialogStr)
														  .SetTitle(Resources.GetString(GetString (Resource.String.btnProjectPage)))
					                              		  .SetIcon(Resource.Drawable.Icon)
						;
														
//							.setNegativeButton(GetString(Resource.String.btnProjectPage), new OnClickListener() {
//								public void onClick(DialogInterface dialog, int which) {
//									StartActivity(new Intent(Intent.ACTION_VIEW, Uri
//											.Parse(Tomdroid.PROJECT_HOMEPAGE)));
//									dialog.dismiss();
//								}
//							}).setPositiveButton(GetString(Resource.String.btnOk), new OnClickListener() {
//								public void onClick(DialogInterface dialog, int which) {
//									dialog.dismiss();
//								}
					return ad.Create();
			    case DIALOG_FIRST_RUN:
//					.setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							Preferences.putBoolean(Preferences.Key.FIRST_RUN, false);
//							dialog.dismiss();
//						}
//					})

				var adFirstRun = new AlertDialog.Builder(this).SetMessage(Resources.GetString(Resource.String.strWelcome))
													  .SetTitle(Resources.GetString(Resource.String.titleWelcome))
						.SetIcon(Resource.Drawable.Icon);
				return adFirstRun.Create();
			    case DIALOG_NOT_FOUND:
				    addCommonNoteNotFoundDialogElements(builder);
				    return builder.Create();
			    case DIALOG_NOT_FOUND_SHORTCUT:
				    addCommonNoteNotFoundDialogElements(builder);
			        Intent removeIntent = new NoteViewShortcutsHelper(this).getRemoveShortcutIntent(dialogstring, uri);
//			        builder.setPositiveButton(GetString(Resource.String.btnRemoveShortcut), new OnClickListener() {
//			            public void onClick(DialogInterface dialogInterface, readonly int i) {
//			                sendBroadcast(removeIntent);
//			                Finish();
//			            }
//			        });
				    return builder.Create();
			    case DIALOG_PARSE_ERROR:
			    	return new AlertDialog.Builder(this)
					.SetMessage(GetString(Resource.String.messageErrorNoteParsing))
					.setTitle(GetString(Resource.String.error))
//					.setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							showNote(true);
//						}})
					.create();
			    case DIALOG_REVERT_ALL:
			    	return new AlertDialog.Builder(this)
			        .SetIcon(Resource.Drawable.ic_dialog_alert)
			        .setTitle(Resource.String.revert_notes)
			        .setMessage(Resource.String.revert_notes_message)
//			    	.setPositiveButton(GetString(Resource.String.yes), new OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//			        		Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
//			        		Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().Format3339(false));
//			            	startSyncing(false);
//			           }
//
//			        })
			        .setNegativeButton(Resource.String.no, null)
			        .create();
			    case DIALOG_CONNECT_FAILED:
			    	return new AlertDialog.Builder(this)
					.SetMessage(GetString(Resource.String.prefSyncConnectionFailed))
//					.setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							dialog.dismiss();
//						}})
					.create();
			    case DIALOG_DELETE_NOTE:
			    	return new AlertDialog.Builder(this)
			        .SetIcon(Resource.Drawable.ic_dialog_alert)
			        .setTitle(Resource.String.delete_note)
			        .setMessage(Resource.String.delete_message)
			        .setPositiveButton(GetString(Resource.String.yes), null)
			        .setNegativeButton(Resource.String.no, null)
			        .create();
			    case DIALOG_REVERT_NOTE:
			    	return new AlertDialog.Builder(this)
			        .SetIcon(Resource.Drawable.ic_dialog_alert)
			        .setTitle(Resource.String.revert_note)
			        .setMessage(Resource.String.revert_note_message)
			        .setPositiveButton(GetString(Resource.String.yes), null)
			        .setNegativeButton(Resource.String.no, null)
			        .create();
			    case DIALOG_SYNC_ERRORS:
			    	return new AlertDialog.Builder(activity)
					.SetTitle(Resources.GetString(Resource.String.error))
			    	.SetMessage(dialogstring)
			        .SetPositiveButton(Resources.GetString(Resource.String.yes), null)
//					.setNegativeButton(GetString(Resource.String.close), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) { finishSync(); }
//					})
					.create();
			    case DIALOG_SEND_CHOOSE:
	                Uri intentUri = Uri.Parse(dialogstring);
	                return new AlertDialog.Builder(activity)
					.SetMessage(GetString(Resource.String.sendChoice))
					.SetTitle(GetString(Resource.String.sendChoiceTitle))
			        .SetPositiveButton(Resources.GetString(Resource.String.btnSendAsFile), null)
					.SetNegativeButton(Resources.GetString(Resource.String.btnSendAsText), null)
					.Create();
			    case DIALOG_VIEW_TAGS:
			    	dialogInput = new EditText(this);
			    	return new AlertDialog.Builder(activity)
			    	.SetMessage(GetString(Resource.String.edit_tags))
			    	.SetTitle(string.Format(GetString(Resource.String.note_x_tags),dialogNote.getTitle()))
			    	.SetView(dialogInput)
//			    	.setNegativeButton(Resource.String.btnCancel, new DialogInterface.OnClickListener() {
//						public void onClick(DialogInterface dialog, int whichButton) {
//							removeDialog(DIALOG_VIEW_TAGS);
//						}
//			    	})
			    	.setPositiveButton(Resource.String.btnOk, null)
			    	.create();
			    default:
			    	alertDialog = null;
			    }
			return alertDialog;
		}

		protected override void OnPrepareDialog(int id, Dialog dialog)
		{
		    base.OnPrepareDialog (id, dialog);
		    Activity activity = this;
		    switch(id) {
		    	case DIALOG_SYNC:
					SyncService currentService = SyncManager.getInstance().getCurrentService();
					string serviceDescription = currentService.getDescription();
		    		((ProgressDialog) dialog).SetTitle(string.Format(GetString(Resource.String.syncing),serviceDescription));
		    		((ProgressDialog) dialog).SetMessage(dialogstring);
//		    		((ProgressDialog) dialog).setOnCancelListener(new DialogInterface.OnCancelListener() {
//		    			
//						public void onCancel(DialogInterface dialog) {
//							SyncManager.getInstance().cancel();
//						}
//						
//					});
		    		break;
			    case DIALOG_NOT_FOUND_SHORTCUT:
			        Intent removeIntent = new NoteViewShortcutsHelper(this).getRemoveShortcutIntent(dialogstring, uri);
//			        ((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.btnRemoveShortcut), new OnClickListener() {
//			            public void onClick( DialogInterface dialogInterface, readonly int i) {
//			                sendBroadcast(removeIntent);
//			                Finish();
//			            }
//			        });
			        break;
			    case DIALOG_REVERT_ALL:
//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.yes), new OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//			        		Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
//			        		Preferences.putstring(Preferences.Key.LATEST_SYNC_DATE, new Time().Format3339(false));
//			            	startSyncing(false);
//			           }
//
//			        });
				    break;
			    case DIALOG_DELETE_NOTE:
//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.yes), new OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//			        		deleteNote(dialogstring, dialogInt);
//			           }
//
//			        });
				    break;
			    case DIALOG_REVERT_NOTE:
//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.yes), new OnClickListener() {
//
//			            public void onClick(DialogInterface dialog, int which) {
//							SyncManager.getInstance().pullNote(dialogstring);
//			           }
//
//			        });
				    break;
			    case DIALOG_SYNC_ERRORS:
//			    	((AlertDialog) dialog).setMessage(dialogstring);
//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.btnSavetoSD), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							if(!dialogBoolean) {
//								Toast.MakeText(activity, activity.GetString(Resource.String.messageCouldNotSave),
//										ToastLength.Short).Show();
//							}
//							finishSync();
//						}
//					});
				    break;
			    case DIALOG_SEND_CHOOSE:
//	                readonly Uri intentUri = Uri.Parse(dialogstring);
//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.btnSendAsFile), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) {
//							(new Send(activity, intentUri, true)).send();
//
//						}
//					});
//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_NEGATIVE, GetString(Resource.String.btnSendAsText), new OnClickListener() {
//						public void onClick(DialogInterface dialog, int which) { 
//			                (new Send(activity, intentUri, false)).send();
//						}
//					});
				    break;
			    case DIALOG_VIEW_TAGS:
			    	((AlertDialog) dialog).SetTitle(string.Format(GetString(Resource.String.note_x_tags),dialogNote.getTitle()));
			    	dialogInput.SetText(dialogNote.getTags());

//			    	((AlertDialog) dialog).setButton(Dialog.BUTTON_POSITIVE, GetString(Resource.String.btnOk), new DialogInterface.OnClickListener() {
//			    		public void onClick(DialogInterface dialog, int whichButton) {
//			    			string value = dialogInput.getText().ToString();
//				    		dialogNote.setTags(value);
//				    		dialogNote.setLastChangeDate();
//							NoteManager.putNote(activity, dialogNote);
//							removeDialog(DIALOG_VIEW_TAGS);
//			    		}
//			    	});
			    	break;
			}
		}

		protected override void onListItemClick(ListView l, View v, int position, long id) {
			base.OnListItemClick(l, v, position, id);
			if (rightPane != null) {
				if(position == lastIndex) // same index, edit
					this.startEditNote();
				else
					showNoteInPane(position);
			}
			else {
				ICursor item = (Cursor) adapter.getItem(position);
				long noteId = item.GetInt(item.GetColumnIndexOrThrow(Note.ID));
					this.ViewNote(noteId);
			}
		}
		
		// called when rotating screen
		public override void onConfigurationChanged(Configuration newConfig)
		{
		    base.OnConfigurationChanged(newConfig);
	        main =  View.Inflate(this, Resource.Layout.main, null);
	        SetContentView(main);

	        if (Integer.parseInt(Build.VERSION.Sdk) >= 11) {
	            Honeycomb.invalidateOptionsMenuWrapper(this); 
	        }
			
			registerForContextMenu(FindViewById(Resource.Id.list));

			// add note to pane for tablet
			rightPane = (LinearLayout) FindViewById(Resource.Id.right_pane);
			
			if(rightPane != null) {
				content = (TextView) FindViewById(Resource.Id.content);
				title = (TextView) FindViewById(Resource.Id.title);
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
			SetListAdapter(adapter);
		}
		
		private void updateEmptyList(string aquery) {
			// set the view shown when the list is empty
			listEmptyView = (TextView) FindViewById(Resource.Id.list_empty);
			if (rightPane == null) {
				if (aquery != null) {
					listEmptyView.SetText(GetString(Resource.String.strNoResults, aquery)); }
				else if (adapter.getCount() != 0) {
					listEmptyView.SetText(GetString(Resource.String.strListEmptyWaiting)); }
				else {
					listEmptyView.SetText(GetString(Resource.String.strListEmptyNoNotes)); }
			} else {
				if (aquery != null) {
					title.SetText(GetString(Resource.String.strNoResults, aquery)); }
				else if (adapter.getCount() != 0) {
					title.SetText(GetString(Resource.String.strListEmptyWaiting)); }
				else {
					title.SetText(GetString(Resource.String.strListEmptyNoNotes)); }
			}
			ListView.EmptyView = listEmptyView;
		}
		
		private void updateTextAttributes() {
			float baseSize = Float.parseFloat(Preferences.GetString(Preferences.Key.BASE_TEXT_SIZE));
			content.SetTextSize(baseSize);
			title.SetTextSize(baseSize*1.3f);

			title.SetTextColor(Color.Blue);
			title.PaintFlags = title.PaintFlags | PaintFlags.UnderlineText;
			title.SetBackgroundColor(0xffffffff);

			content.SetBackgroundColor(0xffffffff);
			content.SetTextColor(Color.DarkGray);
		}
		private void showNoteInPane(int position) {
			if(rightPane == null)
				return;
			
			if(position == -1)
				position = 0;

	        title.SetText("");
	        content.SetText("");

	     // save index and top position

			int index = ListView.FirstVisiblePosition;
			View v = ListView.GetChildAt(0);
	        int top = (v == null) ? 0 : v.getTop();

	        updateNotesList(Query, position);

	    // restore
		
			ListView.SetSelectionFromTop(index, top);
			
			if(position >= adapter.getCount())
				position = 0;
			
			ICursor item = (ICursor) adapter.GetItem(position);
			if (item == null || item.getCount() == 0) {
	            TLog.d(TAG, "Index {0} not found in list", position);
				return;
			}
			TLog.d(TAG, "Getting note {0}", position);

			long noteId = item.GetInt(item.getColumnIndexOrThrow(Note.ID));	
			uri = Uri.Parse(CONTENT_URI + "/" + noteId);

	        note = NoteManager.getNote(this, uri);
			TLog.v(TAG, "Note guid: {0}", note.getGuid());

	        if(note != null) {
	        	TLog.d(TAG, "note {0} found", position);
	            noteContent = new NoteContentBuilder().setCaller(noteContentHandler).setInputSource(note.getXmlContent()).setTitle(note.getTitle()).build();
	    		lastIndex = position;
	        } else {
	            TLog.d(TAG, "The note {0} doesn't exist", uri);
			    bool proposeShortcutRemoval;
			    bool calledFromShortcut = Intent.GetBooleanExtra(CALLED_FROM_SHORTCUT_EXTRA, false);
			    string shortcutName = Intent.GetStringExtra(SHORTCUT_NAME);
			    proposeShortcutRemoval = calledFromShortcut && uri != null && shortcutName != null;
			
			    if (proposeShortcutRemoval) {
			    	dialogstring = shortcutName;
		            ShowDialog(DIALOG_NOT_FOUND_SHORTCUT);
			    }
			    else
		            ShowDialog(DIALOG_NOT_FOUND);

	        }
		}
		private void showNote(bool xml) {
			
			if(xml) {
				content.SetText(note.getXmlContent());
				title.SetText((CharSequence) note.getTitle());
				this.Title = Title + " - XML";
				return;
			}

			LinkInternalSpan[] links = noteContent.getSpans(0, noteContent.Length, LinkInternalSpan);
			MatchFilter noteLinkMatchFilter = LinkInternalSpan.getNoteLinkMatchFilter(noteContent, links);

			// show the note (spannable makes the TextView able to output styled text)
			content.SetText(noteContent, TextView.BufferType.Spannable);

			// add links to stuff that is understood by Android except phone numbers because it's too aggressive
			// TODO this is SLOWWWW!!!!
			
			int linkFlags = 0;
			
			if(Preferences.GetBoolean(Preferences.Key.LINK_EMAILS))
				linkFlags |= Linkify.EMAIL_ADDRESSES;
			if(Preferences.GetBoolean(Preferences.Key.LINK_URLS))
				linkFlags |= Linkify.WEB_URLS;
			if(Preferences.GetBoolean(Preferences.Key.LINK_ADDRESSES))
				linkFlags |= Linkify.MAP_ADDRESSES;
			
			Linkify.addLinks(content, linkFlags);

			// Custom phone number linkifier (fixes lp:512204)
			if(Preferences.GetBoolean(Preferences.Key.LINK_PHONES))
				Linkify.addLinks(content, LinkifyPhone.PHONE_PATTERN, "tel:", LinkifyPhone.sPhoneNumberMatchFilter, Linkify.sPhoneNumberTransformFilter);

			// This will create a link every time a note title is found in the text.
			// The pattern contains a very dumb (title1)|(title2) escaped correctly
			// Then we transform the url from the note name to the note id to avoid characters that mess up with the URI (ex: ?)
			if(Preferences.GetBoolean(Preferences.Key.LINK_TITLES)) {
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
			title.SetText((CharSequence) note.getTitle());
		}
		
		private void addCommonNoteNotFoundDialogElements(AlertDialog.Builder builder) {
		    builder.SetMessage(GetString(Resource.String.messageNoteNotFound))
		            .setTitle(GetString(Resource.String.titleNoteNotFound))
//		            .setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//		                public void onClick(DialogInterface dialog, int which) {
//		                    dialog.dismiss();
//		                    Finish();
//		                }
//		            })
					;
		}	
		
//		private Handler noteContentHandler = new Handler() {
		
//			public override void handleMessage(Message msg) {
//		
//				//parsed ok - show
//				if(msg.what == NoteContentBuilder.PARSE_OK) {
//					showNote(false);
//		
//				//parsed not ok - error
//				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {
//		
//					ShowDialog(DIALOG_PARSE_ERROR);
//		    	}
//			}
//		};

		// custom transform filter that takes the note's title part of the URI and translate it into the note id
		// this was done to avoid problems with invalid characters in URI (ex: ? is the Query separator but could be in a note title)
//		public TransformFilter noteTitleTransformFilter = new TransformFilter() {
//		
//			public string transformUrl(Matcher m, string str) {
//		
//				int id = NoteManager.getNoteId(Tomdroid.this, str);
//		
//				// return something like content://org.tomdroid.notes/notes/3
//				return Tomdroid.CONTENT_URI.ToString()+"/"+id;
//			}
//		};
		
		//@SuppressWarnings("deprecation")
		private void startSyncing(bool push) {

			string serverUri = Preferences.GetString(Preferences.Key.SYNC_SERVER);
			SyncService currentService = SyncManager.getInstance().getCurrentService();
			
			if (currentService.needsAuth()) {
		
				// service needs authentication
				TLog.i(TAG, "Creating dialog");

				ShowDialog(DIALOG_AUTH_PROGRESS);
		
//				Handler handler = new Handler() {
//		
//					public override void handleMessage(Message msg) {
//		
//						bool wasSuccessful = false;
//						Uri authorizationUri = (Uri) msg.obj;
//						if (authorizationUri != null) {
//		
//							Intent i = new Intent(Intent.ACTION_VIEW, authorizationUri);
//							StartActivity(i);
//							wasSuccessful = true;
//		
//						} else {
//							// Auth failed, don't update the value
//							wasSuccessful = false;
//						}
//		
//						if (authProgressDialog != null)
//							authProgressDialog.dismiss();
//		
//						if (wasSuccessful) {
//							resetLocalDatabase();
//						} else {
//							ShowDialog(DIALOG_CONNECT_FAILED);
//						}
//					}
//				};

				((ServiceAuth) currentService).getAuthUri(serverUri, handler);
			}
			else {
				syncProcessedNotes = 0;
				syncTotalNotes = 0;
				dialogstring = GetString(Resource.String.syncing_connect);
		        ShowDialog(DIALOG_SYNC);
		        SyncManager.getInstance().startSynchronization(push); // push by default
			}
		}
		
		//TODO use LocalStorage wrapper from two-way-sync branch when it get's merged
		private void resetLocalDatabase() {
			ContentResolver.Delete(Tomdroid.CONTENT_URI, null, null);
			Preferences.putLong(Preferences.Key.LATEST_SYNC_REVISION, 0);
			
			// first explanatory note will be deleted on sync
			//NoteManager.putNote(this, FirstNote.createFirstNote());
		}

		public void ViewNote(long noteId) {
			Uri intentUri = getNoteIntentUri(noteId);
			Intent i = new Android.Content.Intent(Intent.ActionView, intentUri, this, typeof(ViewNote));
			StartActivity(i);
		}
		
		protected void startEditNote() {
			 Intent i = new Intent(Intent.ActionView, uri, this, typeof(EditNote));
			StartActivity(i);
		}
		
		protected void startEditNote(long noteId) {
			Uri intentUri = getNoteIntentUri(noteId);
			 Intent i = new Intent(Intent.ActionView, intentUri, this, typeof(EditNote));
			StartActivity(i);
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
			
			Intent i = new Intent(Intent.ActionView, uri, this, typeof(EditNote));
			StartActivity(i);

			
		}
		private void deleteNote(string guid, int position) {
			NoteManager.deleteNote(this, guid);
			showNoteInPane(position);
		}
		
		private void undeleteNote(Note anote) {
			NoteManager.undeleteNote(this, anote);
			updateNotesList(Query,lastIndex);
		}
			
		//@SuppressWarnings("deprecation")
		private void revertNote( string guid) {
			dialogstring = guid;
			ShowDialog(DIALOG_REVERT_NOTE);
		}

		public class SyncMessageHandler : Handler {
		
			private Activity activity;
			
			public SyncMessageHandler(Activity activity) {
				this.activity = activity;
			}
		
			public override void HandleMessage (Message msg)
			{
				SyncService currentService = SyncManager.getInstance().getCurrentService();
				string serviceDescription = currentService.getDescription();
				string message = "";
				bool dismiss = false;

				switch (msg.What) {
					case SyncService.AUTH_COMPLETE:
						message = GetString(Resource.String.messageAuthComplete);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
					case SyncService.AUTH_FAILED:
						dismiss = true;
						message = GetString(Resource.String.messageAuthFailed);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
					case SyncService.PARSING_COMPLETE:
						 ErrorList errors = (ErrorList)msg.Obj;
						if(errors == null || errors.isEmpty()) {
							message = GetString(Resource.String.messageSyncComplete);
							message = string.Format(message,serviceDescription);
							Toast.MakeText(activity, message, ToastLength.Short).Show();
							finishSync();
						} else {
							TLog.v(TAG, "syncErrors: {0}", TextUtils.Join("\n",errors.ToArray()));
							dialogstring = GetString(Resource.String.messageSyncError);
							dialogBoolean = errors.save();
							ShowDialog(DIALOG_SYNC_ERRORS);
						}
						break;
					case SyncService.CONNECTING_FAILED:
						dismiss = true;
						message = GetString(Resource.String.messageSyncConnectingFailed);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
					case SyncService.PARSING_FAILED:
						dismiss = true;
						message = GetString(Resource.String.messageSyncParseFailed);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
					case SyncService.PARSING_NO_NOTES:
						dismiss = true;
						message = GetString(Resource.String.messageSyncNoNote);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
						
					case SyncService.NO_INTERNET:
						dismiss = true;
						Toast.MakeText(activity, GetString(Resource.String.messageSyncNoConnection),ToastLength.Short).Show();
						break;
						
					case SyncService.NO_SD_CARD:
						dismiss = true;
						Toast.MakeText(activity, activity.GetString(Resource.String.messageNoSDCard),
								ToastLength.Short).Show();
						break;
					case SyncService.SYNC_CONNECTED:
						dialogstring = GetString(Resource.String.gettings_notes);
						ShowDialog(DIALOG_SYNC);
						break;
					case SyncService.BEGIN_PROGRESS:
						syncTotalNotes = msg.Arg1;
						syncProcessedNotes = 0;
						dialogstring = GetString(Resource.String.syncing_local);
						ShowDialog(DIALOG_SYNC);
						break;
					case SyncService.SYNC_PROGRESS:
						if(msg.Arg1 == 90) {
							dialogstring = GetString(Resource.String.syncing_remote);						
							ShowDialog(DIALOG_SYNC);
						}
						break;
					case SyncService.NOTE_DELETED:
						message = GetString(Resource.String.messageSyncNoteDeleted);
						message = string.Format(message,serviceDescription);
						//Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
		
					case SyncService.NOTE_PUSHED:
						message = GetString(Resource.String.messageSyncNotePushed);
						message = string.Format(message,serviceDescription);
						//Toast.MakeText(activity, message, ToastLength.Short).Show();

						break;
					case SyncService.NOTE_PULLED:
						message = GetString(Resource.String.messageSyncNotePulled);
						message = string.Format(message,serviceDescription);
						//Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
															
					case SyncService.NOTE_DELETE_ERROR:
						dismiss = true;
						Toast.MakeText(activity, activity.GetString(Resource.String.messageSyncNoteDeleteError), ToastLength.Short).Show();
						break;
		
					case SyncService.NOTE_PUSH_ERROR:
						dismiss = true;
						Toast.MakeText(activity, activity.GetString(Resource.String.messageSyncNotePushError), ToastLength.Short).Show();
						break;
					case SyncService.NOTE_PULL_ERROR:
						dismiss = true;
						message = GetString(Resource.String.messageSyncNotePullError);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
					case SyncService.IN_PROGRESS:
						Toast.MakeText(activity, activity.GetString(Resource.String.messageSyncAlreadyInProgress), ToastLength.Short).Show();
						dismiss = true;
						break;
					case SyncService.NOTES_BACKED_UP:
						Toast.MakeText(activity, activity.GetString(Resource.String.messageNotesBackedUp), ToastLength.Short).Show();
						break;
					case SyncService.SYNC_CANCELLED:
						dismiss = true;
						message = GetString(Resource.String.messageSyncCancelled);
						message = string.Format(message,serviceDescription);
						Toast.MakeText(activity, message, ToastLength.Short).Show();
						break;
					default:
						break;
		
				}
				if(dismiss)
					RemoveDialog(DIALOG_SYNC);
			}
		}

		protected void  onActivityResult (int requestCode, int resultCode, Intent  data) {
			TLog.d(TAG, "onActivityResult called with result {0}", resultCode);
			
			// returning from file picker
			if(data != null && data.HasExtra(FilePickerActivity.EXTRA_FILE_PATH)) {
				// Get the file path
				File f = new File(data.GetStringExtra(FilePickerActivity.EXTRA_FILE_PATH));
				Uri noteUri = Uri.FromFile(f);
				Intent intent = new Intent(this, typeof(Receive));
				intent.Data = noteUri;
				StartActivity(intent);
			}
			else { // returning from sync conflict
				SyncService currentService = SyncManager.getInstance().getCurrentService();
				currentService.resolvedConflict(requestCode);			
			}
		}
		
		public void finishSync() {
			TLog.v(TAG, "Finishing Sync");
			
			RemoveDialog(DIALOG_SYNC);
			
			if(rightPane != null)
				showNoteInPane(lastIndex);
		}
	}
}