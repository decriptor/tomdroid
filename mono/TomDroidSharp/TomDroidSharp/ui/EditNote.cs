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

//import java.io.stringReader;
//import java.util.regex.Matcher;
//import java.util.regex.Pattern;
//
//import javax.xml.parsers.SAXParser;
//import javax.xml.parsers.SAXParserFactory;

using TomDroidSharp.Note;
using TomDroidSharp.NoteManager;
using TomDroidSharp.R;
using TomDroidSharp.sync.SyncManager;
using TomDroidSharp.sync.sd.SdCardSyncService;
using TomDroidSharp.ui.actionbar.ActionBarActivity;
using TomDroidSharp.util.LinkifyPhone;
using TomDroidSharp.util.NewNote;
using TomDroidSharp.util.NoteContentBuilder;
using TomDroidSharp.util.Preferences;
using TomDroidSharp.util.NoteXMLContentBuilder;
using TomDroidSharp.util.TLog;
using TomDroidSharp.xml.LinkInternalSpan;
using TomDroidSharp.xml.NoteContentHandler;
//import org.xml.sax.InputSource;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Text;
using Android.Text.Format;
using Android.Text.Style;
using Android.Text.Util;
using Android.Views;
using Android.Widget;

namespace TomDroidSharp.ui
{

	// TODO this class is starting to smell
	public class EditNote : ActionBarActivity {
		
		// UI elements
		private EditText title;
		private EditText content;
		private SlidingDrawer formatBar;
		
		// Model objects
		private Note note;
		private SpannablestringBuilder noteContent;
		
		// Logging info
		private static readonly string TAG = "EditNote";
		
		private Uri uri;
		public static readonly string CALLED_FROM_SHORTCUT_EXTRA = "org.tomdroid.CALLED_FROM_SHORTCUT";
	    public static readonly string SHORTCUT_NAME = "org.tomdroid.SHORTCUT_NAME";
	    
		// rich text variables
		
		int styleStart = -1, cursorLoc = 0;
	    private int sselectionStart;
		private int sselectionEnd;
	    private float size = 1.0f;
		private bool xmlOn = false;
		
		// check whether text has been changed yet
		private bool textChanged = false;
		// discard changes -> not will not be saved
		private bool discardChanges = false;
		// force close without onDestroy() function when note not existing!
		private bool forceClose = false;
		
		// TODO extract methods in here
		@Override
		protected void onCreate(Bundle savedInstanceState) {
			super.onCreate(savedInstanceState);

			Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);
			
			SetContentView(R.layout.note_edit);
			
			content = (EditText) findViewById(R.id.content);
			title = (EditText) findViewById(R.id.title);
			
			formatBar = (SlidingDrawer) findViewById(R.id.formatBar);

			content.setOnFocusChangeListener(new OnFocusChangeListener() {

			    public void onFocusChange(View v, bool hasFocus) {
			    	if(hasFocus && !xmlOn) {
			    		formatBar.setVisibility(View.VISIBLE);
			    	}
			    	else {
			    		formatBar.setVisibility(View.GONE);
			    	}
			    }
			});
			
	        uri = getIntent().getData();
		}

		private void handleNoteUri( Uri uri) {// We were triggered by an Intent URI
	        TLog.d(TAG, "EditNote started: Intent-filter triggered.");

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

		private Handler noteContentHandler = new Handler() {

			@Override
			public void handleMessage(Message msg) {

				//parsed ok - show
				if(msg.what == NoteContentBuilder.PARSE_OK) {
					showNote(false);
					
					// add format bar listeners here
					
					addFormatListeners();

				//parsed not ok - error
				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {

					new AlertDialog.Builder(EditNote.this)
						.setMessage(getstring(R.string.messageErrorNoteParsing))
						.setTitle(getstring(R.string.error))
						.setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
							public void onClick(DialogInterface dialog, int which) {
								dialog.dismiss();
								showNote(true);
							}})
						.show();
	        	}
			}
		};
		
	    private void showNoteNotFoundDialog( Uri uri) {
	    	 AlertDialog.Builder builder = new AlertDialog.Builder(this);
	        builder.setMessage(getstring(R.string.messageNoteNotFound))
	                .setTitle(getstring(R.string.titleNoteNotFound))
	                .setNeutralButton(getstring(R.string.btnOk), new OnClickListener() {
	                    public void onClick(DialogInterface dialog, int which) {
	                        dialog.dismiss();
	                        forceClose = true;
	                        finish();
	                    }
	                });
	        builder.show();
	    }
	    
	    @Override
	    protected void onPause() {
	    	if (uri != null) {
	        	if(!discardChanges && textChanged) // changed and not discarding changes
	       			saveNote();
	        	else if (discardChanges && NewNote.neverSaved)
	        		NoteManager.deleteNote(this, note);
	        		NewNote.neverSaved = false;
	        }
	    	super.onPause();
	    }

	    @Override
	    protected void onDestroy() {
	    	if(!forceClose) {
	    		if(note.getTitle().length() == 0 && note.getXmlContent().length() == 0 && !textChanged) // if the note is empty, e.g. new
					NoteManager.deleteNote(this, note);
	    	}
	    	super.onDestroy();
	    }
	    
		@Override
		public void onResume(){
			TLog.v(TAG, "resume edit note");
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
			MenuInflater inflater = getMenuInflater();
			inflater.inflate(R.menu.edit_note, menu);

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
				case R.id.edit_note_save:
					saveNote();
					return true;
				case R.id.edit_note_discard:
					discardNoteContent();
					return true;
	/*			case R.id.edit_note_xml:
	            	if(!xmlOn) {
	            		item.setTitle(getstring(R.string.text));
	            		item.setIcon(R.drawable.text);
	            		xmlOn = true;
	        			SpannablestringBuilder newNoteContent = (SpannablestringBuilder) content.getText();

	        			// store changed note content
	        			string newXmlContent = new NoteXMLContentBuilder().setCaller(noteXMLWriteHandler).setInputSource(newNoteContent).build();
	        			// Since 0.5 EditNote expects the redundant title being removed from the note content, but we still may need this for debugging:
	        			//note.setXmlContent("<note-content version=\"0.1\">"+note.getTitle()+"\n\n"+newXmlContent+"</note-content>");
	        			TLog.d(TAG, "new xml content: {0}", newXmlContent);
	        			note.setXmlContent(newXmlContent);
	            		formatBarShell.setVisibility(View.GONE);
	            		content.setText(note.getXmlContent());
	            	}
	            	else {
	            		item.setTitle(getstring(R.string.xml));
	            		item.setIcon(R.drawable.xml);
	            		xmlOn = false;
	            		updateNoteContent(true);  // update based on xml that we are switching FROM
	            		if(content.isFocused())
	            			formatBarShell.setVisibility(View.VISIBLE);
	            	}
					return true;*/
			}
			return super.onOptionsItemSelected(item);
		}
		
		
		private void showNote(bool xml) {
			if(xml) {

				formatBar.setVisibility(View.GONE);
				content.setText(note.getXmlContent());
				title.setText((CharSequence) note.getTitle());
				this.setTitle(this.getTitle() + " - XML");
				xmlOn = true;
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
		
		private Handler noteXMLParseHandler = new Handler() {

			@Override
			public void handleMessage(Message msg) {
				
				//parsed ok - show
				if(msg.what == NoteContentBuilder.PARSE_OK) {
					showNote(false);

				//parsed not ok - error
				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {
					
					// TODO put this string in a translatable resource
					new AlertDialog.Builder(EditNote.this)
						.setMessage("The requested note could not be parsed. If you see this error " +
									" and you are able to replicate it, please file a bug!")
						.setTitle("Error")
						.setNeutralButton("Ok", new OnClickListener() {
							public void onClick(DialogInterface dialog, int which) {
								dialog.dismiss();
								showNote(true);
							}})
						.show();
	        	}
			}
		};

		private Handler noteXMLWriteHandler = new Handler() {

			@Override
			public void handleMessage(Message msg) {
				
				//parsed ok - do nothing
				if(msg.what == NoteXMLContentBuilder.PARSE_OK) {
				//parsed not ok - error
				} else if(msg.what == NoteXMLContentBuilder.PARSE_ERROR) {
					
					// TODO put this string in a translatable resource
					new AlertDialog.Builder(EditNote.this)
						.setMessage("The requested note could not be parsed. If you see this error " +
									" and you are able to replicate it, please file a bug!")
						.setTitle("Error")
						.setNeutralButton("Ok", new OnClickListener() {
							public void onClick(DialogInterface dialog, int which) {
								dialog.dismiss();
								finish();
							}})
						.show();
	        	}
			}
		};

		// custom transform filter that takes the note's title part of the URI and translate it into the note id
		// this was done to avoid problems with invalid characters in URI (ex: ? is the Query separator but could be in a note title)
		private TransformFilter noteTitleTransformFilter = new TransformFilter() {

			public string transformUrl(Matcher m, string str) {

				int id = NoteManager.getNoteId(EditNote.this, str);
				
				// return something like content://org.tomdroid.notes/notes/3
				return Tomdroid.CONTENT_URI.tostring()+"/"+id;
			}  
		};

		private bool updateNoteContent(bool xml) {

			SpannablestringBuilder newNoteContent = new SpannablestringBuilder();
			if(xml) {
				// parse XML
				string xmlContent = "<note-content version=\"1.0\">"+this.content.getText().tostring()+"</note-content>";
				string subjectName = this.title.getText().tostring();
		        InputSource noteContentIs = new InputSource(new stringReader(xmlContent));
				try {
					// Parsing
			    	// XML 
			    	// Get a SAXParser from the SAXPArserFactory
			        SAXParserFactory spf = SAXParserFactory.newInstance();

			        // trashing the namespaces but keep prefixes (since we don't have the xml header)
			        spf.setFeature("http://xml.org/sax/features/namespaces", false);
			        spf.setFeature("http://xml.org/sax/features/namespace-prefixes", true);
			        SAXParser sp = spf.newSAXParser();

			        sp.parse(noteContentIs, new NoteContentHandler(newNoteContent));
				} catch (Exception e) {
					e.printStackTrace();
					// TODO handle error in a more granular way
					TLog.e(TAG, "There was an error parsing the note {0}", subjectName);
					return false;
				}
				
			}
			else
				newNoteContent = (SpannablestringBuilder) this.content.getText();

			// store changed note content
			string newXmlContent = new NoteXMLContentBuilder().setCaller(noteXMLWriteHandler).setInputSource(newNoteContent).build();
			
			// Since 0.5 EditNote expects the redundant title being removed from the note content, but we still may need this for debugging:
			//note.setXmlContent("<note-content version=\"0.1\">"+note.getTitle()+"\n\n"+newXmlContent+"</note-content>");
			note.setXmlContent(newXmlContent);
			noteContent = note.getNoteContent(noteXMLWriteHandler);
			return true;
		}
		
		private void saveNote() {
			TLog.v(TAG, "saving note");
			
			bool updated = updateNoteContent(xmlOn);
			if(!updated) {
				Toast.makeText(this, getstring(R.string.messageErrorParsingXML), Toast.LENGTH_SHORT).show();
				return;
			}
			
			string validTitle = NoteManager.validateNoteTitle(this, title.getText().tostring(), note.getGuid()); 
			title.setText(validTitle);
			note.setTitle(validTitle);

			Time now = new Time();
			now.setToNow();
			string time = now.Format3339(false);
			note.setLastChangeDate(time);
			NoteManager.putNote( this, note);
			if(!SyncManager.getInstance().getCurrentService().needsLocation() && Preferences.getBoolean(Preferences.Key.AUTO_BACKUP_NOTES)) {
				TLog.v(TAG, "backing note up");
				SdCardSyncService.backupNote(note);
			}
			textChanged = false;
			NewNote.neverSaved = false;

			Toast.makeText(this, getstring(R.string.messageNoteSaved), Toast.LENGTH_SHORT).show();
			TLog.v(TAG, "note saved");
		}

		private void discardNoteContent() {
			new AlertDialog.Builder(EditNote.this)
				.setMessage(getstring(R.string.messageDiscardChanges))
				.setTitle(getstring(R.string.titleDiscardChanges))
				.setPositiveButton(R.string.yes, new DialogInterface.OnClickListener() {
				            public void onClick(DialogInterface dialog, int which) {
				            	discardChanges = true;
				            	dialog.dismiss();
								finish();
				            }
				        })
				        .setNegativeButton(R.string.no, new DialogInterface.OnClickListener() {
				            public void onClick(DialogInterface dialog, int which) {
								dialog.dismiss();
				            }
					})
				.show();
		}

		private void addFormatListeners()
		{
			
			 ToggleButton boldButton = (ToggleButton)findViewById(R.id.bold);
			
			boldButton.setOnClickListener(new Button.OnClickListener() {

				public void onClick(View v) {
			    	
	            	int selectionStart = content.getSelectionStart();
	            	
	            	styleStart = selectionStart;
	            	
	            	int selectionEnd = content.getSelectionEnd();
	            	
	            	if (selectionStart > selectionEnd){
	            		int temp = selectionEnd;
	            		selectionEnd = selectionStart;
	            		selectionStart = temp;
	            	}
	            	
	            	if (selectionEnd > selectionStart)
	            	{
	            		Spannable str = content.getText();
	            		StyleSpan[] ss = str.getSpans(selectionStart, selectionEnd, StyleSpan.class);
	            		
	            		bool Exists = false;
	            		for (int i = 0; i < ss.length; i++) {
	            			if (ss[i].getStyle() == android.graphics.Typeface.BOLD){
	            				str.removeSpan(ss[i]);
	            				Exists = true;
	            			}
	                    }
	            		
	            		if (!Exists){
	            			str.setSpan(new StyleSpan(android.graphics.Typeface.BOLD), selectionStart, selectionEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	            		}
	        			textChanged = true;
	        			updateNoteContent(xmlOn);
	            		boldButton.setChecked(false);
	            	}
	            	else
	            		cursorLoc = selectionStart;
	            }
			});
			
			 ToggleButton italicButton = (ToggleButton)findViewById(R.id.italic);
			
			italicButton.setOnClickListener(new Button.OnClickListener() {
	            public void onClick(View v) {
	            	            	
	            	int selectionStart = content.getSelectionStart();
	            	
	            	styleStart = selectionStart;
	            	
	            	int selectionEnd = content.getSelectionEnd();
	            	
	            	if (selectionStart > selectionEnd){
	            		int temp = selectionEnd;
	            		selectionEnd = selectionStart;
	            		selectionStart = temp;
	            	}
	            	
	            	if (selectionEnd > selectionStart)
	            	{
	            		Spannable str = content.getText();
	            		StyleSpan[] ss = str.getSpans(selectionStart, selectionEnd, StyleSpan.class);
	            		
	            		bool Exists = false;
	            		for (int i = 0; i < ss.length; i++) {
	            			if (ss[i].getStyle() == android.graphics.Typeface.ITALIC){
	            				str.removeSpan(ss[i]);
	            				Exists = true;
	            			}
	                    }
	            		
	            		if (!Exists){
	            			str.setSpan(new StyleSpan(android.graphics.Typeface.ITALIC), selectionStart, selectionEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	            		}
	            		
	        			textChanged = true;
	            		updateNoteContent(xmlOn);
		          		italicButton.setChecked(false);
	            	}
	            	else
	            		cursorLoc = selectionStart;
	            }
			});
			
			 ToggleButton strikeoutButton = (ToggleButton) findViewById(R.id.strike);   
	        
			strikeoutButton.setOnClickListener(new Button.OnClickListener() {
	            public void onClick(View v) {
	            	 
	            	int selectionStart = content.getSelectionStart();
	            	
	            	styleStart = selectionStart;
	            	
	            	int selectionEnd = content.getSelectionEnd();
	            	
	            	if (selectionStart > selectionEnd){
	            		int temp = selectionEnd;
	            		selectionEnd = selectionStart;
	            		selectionStart = temp;
	            	}
	            	
	            	if (selectionEnd > selectionStart)
	            	{
	            		Spannable str = content.getText();
	            		StrikethroughSpan[] ss = str.getSpans(selectionStart, selectionEnd, StrikethroughSpan.class);
	            		
	            		bool Exists = false;
	            		for (int i = 0; i < ss.length; i++) {
	            				str.removeSpan(ss[i]);
	            				Exists = true;
	                    }
	            		
	            		if (!Exists){
	            			str.setSpan(new StrikethroughSpan(), selectionStart, selectionEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	            		}
	        			textChanged = true;
	            		updateNoteContent(xmlOn);
	            		strikeoutButton.setChecked(false);
	            	}
	            	else
	            		cursorLoc = selectionStart;
	            }
	        });
			
			 ToggleButton highButton = (ToggleButton)findViewById(R.id.highlight);
			
			highButton.setOnClickListener(new Button.OnClickListener() {
	            public void onClick(View v) {
	            	            	
	            	int selectionStart = content.getSelectionStart();
	            	
	            	styleStart = selectionStart;
	            	
	            	int selectionEnd = content.getSelectionEnd();
	            	
	            	if (selectionStart > selectionEnd){
	            		int temp = selectionEnd;
	            		selectionEnd = selectionStart;
	            		selectionStart = temp;
	            	}
	            	
	            	if (selectionEnd > selectionStart)
	            	{
	            		Spannable str = content.getText();
	            		BackgroundColorSpan[] ss = str.getSpans(selectionStart, selectionEnd, BackgroundColorSpan.class);
	            		
	            		bool Exists = false;
	            		for (int i = 0; i < ss.length; i++) {
	        				str.removeSpan(ss[i]);
	        				Exists = true;
	                    }
	            		
	            		if (!Exists){
	            			str.setSpan(new BackgroundColorSpan(Note.NOTE_HIGHLIGHT_COLOR), selectionStart, selectionEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	            		}
	            		
	        			textChanged = true;
	        			updateNoteContent(xmlOn);
	            		highButton.setChecked(false);
	            	}
	            	else
	            		cursorLoc = selectionStart;
	            }
			});
			
			 ToggleButton monoButton = (ToggleButton)findViewById(R.id.mono);
			
			monoButton.setOnClickListener(new Button.OnClickListener() {
	            public void onClick(View v) {
	            	            	
	            	int selectionStart = content.getSelectionStart();
	            	
	            	styleStart = selectionStart;
	            	
	            	int selectionEnd = content.getSelectionEnd();
	            	
	            	if (selectionStart > selectionEnd){
	            		int temp = selectionEnd;
	            		selectionEnd = selectionStart;
	            		selectionStart = temp;
	            	}
	            	
	            	if (selectionEnd > selectionStart)
	            	{
	            		Spannable str = content.getText();
	            		TypefaceSpan[] ss = str.getSpans(selectionStart, selectionEnd, TypefaceSpan.class);
	            		
	            		bool Exists = false;
	            		for (int i = 0; i < ss.length; i++) {
	            			if (ss[i].getFamily()==Note.NOTE_MONOSPACE_TYPEFACE){
	            				str.removeSpan(ss[i]);
	            				Exists = true;
	            			}
	                    }
	            		
	            		if (!Exists){
	            			str.setSpan(new TypefaceSpan(Note.NOTE_MONOSPACE_TYPEFACE), selectionStart, selectionEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	            		}
	            		
	        			textChanged = true;
	        			updateNoteContent(xmlOn);
	            		monoButton.setChecked(false);
	            	}
	            	else
	            		cursorLoc = selectionStart;
	            }
			});
	        
	        content.addTextChangedListener(new TextWatcher() { 
	            public void afterTextChanged(Editable s) {
	            	
	                // set text as changed to force auto save if preferred
	            	textChanged = true;
	 
	            	//add style as the user types if a toggle button is enabled
	            	
	            	int position = Selection.getSelectionStart(content.getText());
	            	
	        		if (position < 0){
	        			position = 0;
	        		}
	            	
	        		if (position > 0){
	        			
	        			if (styleStart > position || position > (cursorLoc + 1)){
							//user changed cursor location, reset
							if (position - cursorLoc > 1){
								//user pasted text
								styleStart = cursorLoc;
							}
							else{
								styleStart = position - 1;
							}
						}
	        			
	                	if (boldButton.isChecked()){  
	                		StyleSpan[] ss = s.getSpans(styleStart, position, StyleSpan.class);

	                		for (int i = 0; i < ss.length; i++) {
	            				s.removeSpan(ss[i]);
	                        }
	                		s.setSpan(new StyleSpan(android.graphics.Typeface.BOLD), styleStart, position, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	                	}
	                	if (italicButton.isChecked()){
	                		StyleSpan[] ss = s.getSpans(styleStart, position, StyleSpan.class);
	                		
	                		for (int i = 0; i < ss.length; i++) {
	                			if (ss[i].getStyle() == android.graphics.Typeface.ITALIC){
	                    			if (ss[i].getStyle() == android.graphics.Typeface.ITALIC){
	                    				s.removeSpan(ss[i]);
	                    			}
	                			}
	                        }
	                		s.setSpan(new StyleSpan(android.graphics.Typeface.ITALIC), styleStart, position, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	                	}
	                	if (strikeoutButton.isChecked()){
	                		StrikethroughSpan[] ss = s.getSpans(styleStart, position, StrikethroughSpan.class);
	                		
	                		for (int i = 0; i < ss.length; i++) {
	            				s.removeSpan(ss[i]);
	                        }
	            			s.setSpan(new StrikethroughSpan(), styleStart, position, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	                	}
	                	if (highButton.isChecked()){
	                		BackgroundColorSpan[] ss = s.getSpans(styleStart, position, BackgroundColorSpan.class);
	                		
	                		for (int i = 0; i < ss.length; i++) {
	            				s.removeSpan(ss[i]);
	                        }
	            			s.setSpan(new BackgroundColorSpan(Note.NOTE_HIGHLIGHT_COLOR), styleStart, position, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	                	}
	                	if (monoButton.isChecked()){
	                		TypefaceSpan[] ss = s.getSpans(styleStart, position, TypefaceSpan.class);
	                		
	                		for (int i = 0; i < ss.length; i++) {
	                			if (ss[i].getFamily()==Note.NOTE_MONOSPACE_TYPEFACE){
	                				s.removeSpan(ss[i]);
	                			}
	                        }
	            			s.setSpan(new TypefaceSpan(Note.NOTE_MONOSPACE_TYPEFACE), styleStart, position, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	                	}
	                	if (size != 1.0f){
	                		RelativeSizeSpan[] ss = s.getSpans(styleStart, position, RelativeSizeSpan.class);
	                		
	                		for (int i = 0; i < ss.length; i++) {
	                			s.removeSpan(ss[i]);
	                		}
	                		s.setSpan(new RelativeSizeSpan(size), styleStart, position, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	                	}
	        		}
	        		
	        		cursorLoc = Selection.getSelectionStart(content.getText());
	            } 
	            public void beforeTextChanged(CharSequence s, int start, int count, int after) { 
	                    //unused
	            } 
	            public void onTextChanged(CharSequence s, int start, int before, int count) { 
	                    //unused
	            } 
	        });

	        // set text as changed to force auto save if preferred
	        
	        title.addTextChangedListener(new TextWatcher() { 
	            public void afterTextChanged(Editable s) {
	            	textChanged = true;
	            } 
	            public void beforeTextChanged(CharSequence s, int start, int count, int after) { 
	                    //unused
	            } 
	            public void onTextChanged(CharSequence s, int start, int before, int count) { 
	                    //unused
	            } 
	        });
	        
			 ToggleButton sizeButton = (ToggleButton)findViewById(R.id.size);
			
			sizeButton.setOnClickListener(new Button.OnClickListener() {

				public void onClick(View v) {
					sselectionStart = content.getSelectionStart();
			    	sselectionEnd = content.getSelectionEnd();
	            	showSizeDialog();
	            }
			});
		}
		
		private void showSizeDialog() {
			 CharSequence[] items = {getstring(R.string.small), getstring(R.string.normal), getstring(R.string.large), getstring(R.string.huge)};

			AlertDialog.Builder builder = new AlertDialog.Builder(this);
			builder.setTitle(R.string.messageSelectSize);
			builder.setSingleChoiceItems(items, -1, new DialogInterface.OnClickListener() {
			    public void onClick(DialogInterface dialog, int item) {
			        Toast.makeText(getApplicationContext(), items[item], Toast.LENGTH_SHORT).show();	
			        switch (item) {
		        		case 0: size = 0.8f; break;
		        		case 1: size = 1.0f; break;
		        		case 2: size = 1.5f; break;
		        		case 3: size = 1.8f; break;
					}
			        
			    	if (sselectionStart == sselectionEnd) {
			    		// there is no text selected -> start span here and elongate while typing
		            	styleStart = sselectionStart;
			    		cursorLoc = sselectionStart;
			    	} else {
			    		// there is some text selected, just change the size of this text
			    		changeSize();
			    	}
	                dialog.dismiss();
			    }
			});
			builder.show();
		}

		public void changeSize() 
		{
	        if (sselectionStart > sselectionEnd){
	        	int temp = sselectionEnd;
	        	sselectionEnd = sselectionStart;
	        	sselectionStart = temp;
	        }
	        
	    	if(sselectionStart < sselectionEnd)
	    	{
	        	Spannable str = content.getText();
	        	
	        	// get all the spans in the selected range
	        	RelativeSizeSpan[] ss = str.getSpans(sselectionStart, sselectionEnd, RelativeSizeSpan.class);
	        	
	        	// check the position of the old span and the text size and decide how to rebuild the spans
	    		for (int i = 0; i < ss.length; i++) {
	    			int oldStart = str.getSpanStart(ss[i]);
	    			int oldEnd = str.getSpanEnd(ss[i]);
	    			float oldSize = ss[i].getSizeChange();
	    			str.removeSpan(ss[i]);
					
	    			if (oldStart < sselectionStart && sselectionEnd < oldEnd) {
	    				// old span starts end ends outside selection
	    				str.setSpan(new RelativeSizeSpan(oldSize), oldStart, sselectionStart, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	            		str.setSpan(new RelativeSizeSpan(oldSize), sselectionEnd, oldEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	    			} else if (oldStart < sselectionStart && oldEnd <= sselectionEnd){
	    				// old span starts outside, ends inside the selection
	            		str.setSpan(new RelativeSizeSpan(oldSize), oldStart, sselectionStart, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	    			} else if (sselectionStart <= oldStart && sselectionEnd < oldEnd){
	    				// old span starts inside, ends outside the selection
	    				str.setSpan(new RelativeSizeSpan(oldSize), sselectionEnd, oldEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	    			} else if (sselectionStart <= oldStart && oldEnd <= sselectionEnd) {
	    				// old span was equal or within the selection -> just delete it and make the new one.
	    			}
	    	
	            }
	    		// generate the new span in the selected range
	        	if(size != 1.0f) {
	        		str.setSpan(new RelativeSizeSpan(size), sselectionStart, sselectionEnd, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE);
	        	}
				textChanged = true;
				updateNoteContent(xmlOn);
				size = 1.0f;
	    	}
	    }	
	}
}