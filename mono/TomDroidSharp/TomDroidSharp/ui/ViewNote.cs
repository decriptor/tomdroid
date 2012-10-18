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

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;

using TomDroidSharp.Note;
using TomDroidSharp.NoteManager;
using TomDroidSharp.R;
using TomDroidSharp.ui.actionbar.ActionBarActivity;
using TomDroidSharp.util.LinkifyPhone;
using TomDroidSharp.util.NoteContentBuilder;
using TomDroidSharp.util.NoteViewShortcutsHelper;
using TomDroidSharp.util.Preferences;
using TomDroidSharp.util.Send;
using TomDroidSharp.util.TLog;
using TomDroidSharp.xml.LinkInternalSpan;

//import java.util.regex.Matcher;
//import java.util.regex.Pattern;

namespace TomDroidSharp.ui
{
	// TODO this class is starting to smell
	public class ViewNote : ActionBarActivity {
		public static readonly string CALLED_FROM_SHORTCUT_EXTRA = "org.tomdroid.CALLED_FROM_SHORTCUT";
	    public static readonly string SHORTCUT_NAME = "org.tomdroid.SHORTCUT_NAME";

	    // UI elements
		private TextView content;
		private TextView title;

		// Model objects
		private Note note;

		private SpannablestringBuilder noteContent;

		// Logging info
		private static readonly string TAG = "ViewNote";
	    // UI feedback handler
		
		private Uri uri;

		// TODO extract methods in here
		@Override
		protected void onCreate(Bundle savedInstanceState) {
			super.onCreate(savedInstanceState);
			Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);
			SetContentView(R.layout.note_view);
			
			content = (TextView) findViewById(R.id.content);
			title = (TextView) findViewById(R.id.title);

			// this we will call on resume as well.
			updateTextAttributes();
	        uri = getIntent().getData();
	    }

		private void handleNoteUri(Uri uri) {// We were triggered by an Intent URI
	        TLog.d(TAG, "ViewNote started: Intent-filter triggered.");

	        // TODO validate the good action?
	        // intent.getAction()

	        // TODO verify that getNote is doing the proper validation
	        note = NoteManager.getNote(this, uri);

	        if(note != null) {
				title.setText((CharSequence) note.getTitle());
	            noteContent = note.getNoteContent(noteContentHandler);
	        } else {
	            TLog.d(TAG, "The note {0} doesn't exist", uri);
	            showNoteNotFoundDialog(uri);
	        }
	    }

	    private void showNoteNotFoundDialog(Uri uri) {
	        AlertDialog.Builder builder = new AlertDialog.Builder(this);
	        addCommonNoteNotFoundDialogElements(builder);
	        addShortcutNoteNotFoundElements(uri, builder);
	        builder.show();
	    }

	    private void addShortcutNoteNotFoundElements(Uri uri, AlertDialog.Builder builder) {
	        bool proposeShortcutRemoval;
	        bool calledFromShortcut = getIntent().getBooleanExtra(CALLED_FROM_SHORTCUT_EXTRA, false);
	        string shortcutName = getIntent().getstringExtra(SHORTCUT_NAME);
	        proposeShortcutRemoval = calledFromShortcut && uri != null && shortcutName != null;

	        if (proposeShortcutRemoval) {
	            Intent removeIntent = new NoteViewShortcutsHelper(this).getRemoveShortcutIntent(shortcutName, uri);
	            builder.setPositiveButton(getstring(R.string.btnRemoveShortcut), new OnClickListener() {
	                public void onClick(DialogInterface dialogInterface, readonly int i) {
	                    sendBroadcast(removeIntent);
	                    finish();
	                }
	            });
	        }
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

		@Override
		public void onResume(){
			TLog.v(TAG, "resume view note");
			super.onResume();

	        if (uri == null) {
				TLog.d(TAG, "The Intent's data was null.");
	            showNoteNotFoundDialog(uri);
	        } else handleNoteUri(uri);
			updateTextAttributes();
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
		
		@Override
		public bool onCreateOptionsMenu(Menu menu) {

			// Create the menu based on what is defined in res/menu/noteview.xml
			MenuInflater inflater = getMenuInflater();
			inflater.inflate(R.menu.view_note, menu);
			
	        // Calling super after populating the menu is necessary here to ensure that the
	        // action bar helpers have a chance to handle this event.
			return super.onCreateOptionsMenu(menu);
		}

		@Override
		public bool onOptionsItemSelected(MenuItem item) {
			switch (item.getItemId()) {
		        case android.R.id.home:
		        	// app icon in action bar clicked; go home
	                Intent intent = new Intent(this, Tomdroid.class);
	                intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
	                startActivity(intent);
	            	return true;
				case R.id.menuPrefs:
					startActivity(new Intent(this, PreferencesActivity.class));
					return true;
				case R.id.view_note_send:
					(new Send(this, uri, false)).send();
					return true;
				case R.id.view_note_edit:
					startEditNote();
					return true;
				case R.id.view_note_delete:
					deleteNote();
					return true;
			}
			return super.onOptionsItemSelected(item);
		}

		private void deleteNote() {
			Activity activity = this;
			new AlertDialog.Builder(this)
	        .setIcon(android.R.drawable.ic_dialog_alert)
	        .setTitle(R.string.delete_note)
	        .setMessage(R.string.delete_message)
	        .setPositiveButton(R.string.yes, new DialogInterface.OnClickListener() {

	            public void onClick(DialogInterface dialog, int which) {
	        		NoteManager.deleteNote(activity, note);
	        		Toast.makeText(activity, getstring(R.string.messageNoteDeleted), Toast.LENGTH_SHORT).show();
	        		activity.finish();
	            }

	        })
	        .setNegativeButton(R.string.no, null)
	        .show();
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

		private Handler noteContentHandler = new Handler() {

			@Override
			public void handleMessage(Message msg) {

				//parsed ok - show
				if(msg.what == NoteContentBuilder.PARSE_OK) {
					showNote(false);

				//parsed not ok - error
				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {

					new AlertDialog.Builder(ViewNote.this)
						.setMessage(getstring(R.string.messageErrorNoteParsing))
						.setTitle(getstring(R.string.error))
						.setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
							public void onClick(DialogInterface dialog, int which) {
								dialog.dismiss();
								showNote(false);
							}})
						.show();
	        	}
			}
		};

		// custom transform filter that takes the note's title part of the URI and translate it into the note id
		// this was done to avoid problems with invalid characters in URI (ex: ? is the Query separator but could be in a note title)
		private TransformFilter noteTitleTransformFilter = new TransformFilter() {

			public string transformUrl(Matcher m, string str) {

				int id = NoteManager.getNoteId(ViewNote.this, str);

				// return something like content://org.tomdroid.notes/notes/3
				return Tomdroid.CONTENT_URI.tostring()+"/"+id;
			}
		};

	    protected void startEditNote() {
			Intent i = new Intent(Intent.ACTION_VIEW, uri, this, EditNote.class);
			startActivity(i);
		}
		
	}
}