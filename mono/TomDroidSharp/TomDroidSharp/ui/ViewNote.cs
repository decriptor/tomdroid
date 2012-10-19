/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2008, 2009, 2010 Olivier Bilodeau <olivier@bottomlesspit.org>
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

using System;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;

using TomDroidSharp.ui.actionbar;

//import java.util.regex.Matcher;
//import java.util.regex.Pattern;
using TomDroidSharp.util;
using Android.Runtime;
using TomDroidSharp.xml;
using Android.Text.Util;

namespace TomDroidSharp.ui
{
	// TODO this class is starting to smell
	[Activity (Label = "ViewNote")]
	public class ViewNote : ActionBarActivity {
		public static readonly string CALLED_FROM_SHORTCUT_EXTRA = "org.tomdroid.CALLED_FROM_SHORTCUT";
	    public static readonly string SHORTCUT_NAME = "org.tomdroid.SHORTCUT_NAME";

	    // UI elements
		private TextView content;
		private TextView title;

		// Model objects
		private Note note;

		private StringBuilder noteContent;

		// Logging info
		private static readonly string TAG = "ViewNote";
	    // UI feedback handler
		
		private System.Uri uri;

		// TODO extract methods in here
		protected override void onCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);
			SetContentView(Resource.Layout.note_view);

			content = FindViewById<TextView>(Resource.Id.content);
			title = FindViewById<TextView>(Resource.Id.title);

			// this we will call on resume as well.
			updateTextAttributes();
	        uri = Intent.Data;
	    }

		private void handleNoteUri(Android.Net.Uri uri) {// We were triggered by an Intent URI
	        TLog.d(TAG, "ViewNote started: Intent-filter triggered.");

	        // TODO validate the good action?
	        // intent.getAction()

	        // TODO verify that getNote is doing the proper validation
	        note = NoteManager.getNote(this, uri);

	        if(note != null) {
				title.SetText((CharSequence) note.getTitle());
	            noteContent = note.getNoteContent(noteContentHandler);
	        } else {
	            TLog.d(TAG, "The note {0} doesn't exist", uri);
	            showNoteNotFoundDialog(uri);
	        }
	    }

	    private void showNoteNotFoundDialog(Android.Net.Uri uri) {
	        AlertDialog.Builder builder = new AlertDialog.Builder(this);
	        addCommonNoteNotFoundDialogElements(builder);
	        addShortcutNoteNotFoundElements(uri, builder);
	        builder.Show();
	    }

	    private void addShortcutNoteNotFoundElements(Android.Net.Uri uri, AlertDialog.Builder builder) {
	        bool proposeShortcutRemoval;
	        bool calledFromShortcut = Intent.GetBooleanExtra(CALLED_FROM_SHORTCUT_EXTRA, false);
	        string shortcutName = Intent.GetStringExtra(SHORTCUT_NAME);
	        proposeShortcutRemoval = calledFromShortcut && uri != null && shortcutName != null;

	        if (proposeShortcutRemoval) {
	            Intent removeIntent = new NoteViewShortcutsHelper(this).getRemoveShortcutIntent(shortcutName, uri);
//	            builder.setPositiveButton(GetString(Resource.String.btnRemoveShortcut), new OnClickListener() {
//	                public void onClick(DialogInterface dialogInterface, readonly int i) {
//	                    sendBroadcast(removeIntent);
//	                    Finish();
//	                }
//	            });
	        }
	    }

	    private void addCommonNoteNotFoundDialogElements(AlertDialog.Builder builder) {
	        builder.SetMessage(Resources.GetString(Resource.String.messageNoteNotFound))
	                .SetTitle(GetString(Resource.String.titleNoteNotFound))
//	                .setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//	                    public void onClick(DialogInterface dialog, int which) {
//	                        dialog.dismiss();
//	                        Finish();
//	                    }
//	                })
					;
	    }

		public override void onResume(){
			TLog.v(TAG, "resume view note");
			base.OnResume();

	        if (uri == null) {
				TLog.d(TAG, "The Intent's data was null.");
	            showNoteNotFoundDialog(uri);
	        } else handleNoteUri(uri);
			updateTextAttributes();
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

		public override bool onCreateOptionsMenu(Menu menu) {

			// Create the menu based on what is defined in res/menu/noteview.xml
			MenuInflater inflater = getMenuInflater();
			inflater.Inflate(Resource.menu.view_note, menu);
			
	        // Calling base after populating the menu is necessary here to ensure that the
	        // action bar helpers have a chance to handle this event.
			return base.onCreateOptionsMenu(menu);
		}

		public override bool onOptionsItemSelected(IMenuItem item) {
			switch (item.ItemId) {
		        case Resource.Id.home:
		        	// app icon in action bar clicked; go home
	                Intent intent = new Intent(this, typeof(Tomdroid));
	                intent.AddFlags(ActivityFlags.ClearTop);
	                StartActivity(intent);
	            	return true;
				case Resource.Id.menuPrefs:
					StartActivity(new Intent(this, typeof(PreferencesActivity)));
					return true;
				case Resource.Id.view_note_send:
					(new Send(this, uri, false)).send();
					return true;
				case Resource.Id.view_note_edit:
					startEditNote();
					return true;
				case Resource.Id.view_note_delete:
					deleteNote();
					return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		private void deleteNote() {
			Activity activity = this;
			new AlertDialog.Builder(this)
	        .SetIcon(Resource.Drawable.ic_dialog_alert)
	        .SetTitle(Resource.String.delete_note)
	        .SetMessage(Resource.String.delete_message)
//	        .setPositiveButton(Resource.String.yes, new DialogInterface.OnClickListener() {
//
//	            public void onClick(DialogInterface dialog, int which) {
//	        		NoteManager.deleteNote(activity, note);
//	        		Toast.MakeText(activity, GetString(Resource.String.messageNoteDeleted), ToastLength.Short).Show();
//	        		activity.Finish();
//	            }
//
//	        })
	        .SetNegativeButton(Resource.String.no, null)
	        .Show();
		}

		private void showNote(bool xml) {
			if(xml) {
				content.SetText(note.getXmlContent());
				title.SetText((CharSequence) note.getTitle());
				this.SetTitle(this.Title + " - XML");
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

//		private Handler noteContentHandler = new Handler() {
//
//			public override void handleMessage(Message msg) {
//
//				//parsed ok - show
//				if(msg.what == NoteContentBuilder.PARSE_OK) {
//					showNote(false);
//
//				//parsed not ok - error
//				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {
//
//					new AlertDialog.Builder(ViewNote.this)
//						.setMessage(GetString(Resource.String.messageErrorNoteParsing))
//						.setTitle(GetString(Resource.String.error))
//						.setNeutralButton(GetString(Resource.String.btnOk), new OnClickListener() {
//							public void onClick(DialogInterface dialog, int which) {
//								dialog.dismiss();
//								showNote(false);
//							}})
//						.Show();
//	        	}
//			}
//		};

		// custom transform filter that takes the note's title part of the URI and translate it into the note id
		// this was done to avoid problems with invalid characters in URI (ex: ? is the Query separator but could be in a note title)
//		private TransformFilter noteTitleTransformFilter = new TransformFilter() {
//
//			public string transformUrl(Matcher m, string str) {
//
//				int id = NoteManager.getNoteId(ViewNote.this, str);
//
//				// return something like content://org.tomdroid.notes/notes/3
//				return Tomdroid.CONTENT_URI.ToString()+"/"+id;
//			}
//		};

	    protected void startEditNote() {
			Intent i = new Intent(Intent.ActionView, uri, this, typeof(EditNote));
			StartActivity(i);
		}
		
	}
}