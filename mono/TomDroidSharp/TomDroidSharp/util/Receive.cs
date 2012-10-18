/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2012, 2010, 2011, 2012 Olivier Bilodeau <olivier@bottomlesspit.org>
 * Copyright 2012 Stefan Hammer <j.4@gmx.at>
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
//import java.io.FileInputStream;
//import java.io.IOException;
//import java.io.InputStreamReader;
//import java.io.Reader;
//import java.io.stringReader;
//import java.util.UUID;
//import java.util.regex.Matcher;
//import java.util.regex.Pattern;

//import javax.xml.parsers.SAXParser;
//import javax.xml.parsers.SAXParserFactory;

using TomDroidSharp.Note;
using TomDroidSharp.NoteManager;
using TomDroidSharp.R;
using TomDroidSharp.sync.sd.NoteHandler;
using TomDroidSharp.ui.CompareNotes;
using TomDroidSharp.ui.EditNote;
using TomDroidSharp.ui.Tomdroid;
using TomDroidSharp.ui.actionbar.ActionBarActivity;
using TomDroidSharp.xml.NoteContentHandler;
//import org.xml.sax.InputSource;
//import org.xml.sax.XMLReader;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Widget;

namespace TomDroidSharp.util
{
	public class Receive : ActionBarActivity {
		
		// Logging info
		private static readonly string TAG = "ReceiveActivity";

		// don't import files bigger than this 
		private long MAX_FILE_SIZE = 1048576; // 1MB 

		protected void onCreate (Bundle savedInstanceState) {
			super.onCreate(savedInstanceState);

			// init preferences
			Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);

			// set intent, action, MIME type
		    Intent intent = getIntent();
		    string action = intent.getAction();
		    string type = intent.getType();

			TLog.v(TAG, "Receiving note of type {0}",type);
			TLog.d(TAG, "Action type: {0}",action);
		    
	    	if(intent.getData() != null) {
	    		TLog.d(TAG, "Receiving file from path: {0}",intent.getData().getPath());
				File file = new File(intent.getData().getPath());

				if(file.length() > MAX_FILE_SIZE ) {
		    		Toast.makeText(this, getstring(R.string.messageFileTooBig), Toast.LENGTH_SHORT).show();
					finish();
				}
				else {
					
					final char[] buffer = new char[0x1000];
					
					// Try reading the file first
					string contents = "";
					try {
		
						contents = readFile(file,buffer);
					} catch (IOException e) {
						e.printStackTrace();
						TLog.w(TAG, "Something went wrong trying to read the note");
						finish();
					}
					
					useSendFile(file, contents);
				}
	    	}
	    	else if (Intent.ACTION_SEND.equals(action) && type != null && "text/plain".equals(type)) {
	    		TLog.v(TAG, "receiving note as plain text");
	    	    string sharedContent = intent.getstringExtra(Intent.EXTRA_TEXT);
	    	    string sharedTitle = intent.getstringExtra(Intent.EXTRA_SUBJECT);
	            useSendText(sharedContent, sharedTitle); // use the text being sent
	        }
	    	else {
	    		TLog.v(TAG, "received invalid note");
				finish();
	    	}
		}
		void useSendFile(File file, string contents) {
			Note remoteNote = new Note();

			if(file.getPath().endsWith(".note") && contents.startsWith("<?xml")) { // xml note file
				
				try {
					// Parsing
			    	// XML 
			    	// Get a SAXParser from the SAXPArserFactory
			        SAXParserFactory spf = SAXParserFactory.newInstance();
			        SAXParser sp = spf.newSAXParser();
			
			        // Get the XMLReader of the SAXParser we created
			        XMLReader xr = sp.getXMLReader();
		
			        // Create a new ContentHandler, send it this note to fill and apply it to the XML-Reader
			        NoteHandler xmlHandler = new NoteHandler(remoteNote);
			        xr.setContentHandler(xmlHandler);
		
			        // Create the proper input source
			        stringReader sr = new stringReader(contents);
			        InputSource is = new InputSource(sr);
			        
					TLog.d(TAG, "parsing note");
					xr.parse(is);
		
				// TODO wrap and throw a new exception here
				} catch (Exception e) {
					e.printStackTrace();
					if(e instanceof TimeFormatException) TLog.e(TAG, "Problem parsing the note's date and time");
					finish();
				}
				// the note guid is not stored in the xml but in the filename
				remoteNote.setGuid(file.getName().replace(".note", ""));
				Pattern note_content = Pattern.compile("<note-content[^>]+>(.*)<\\/note-content>", Pattern.CASE_INSENSITIVE+Pattern.DOTALL);

				// FIXME here we are re-reading the whole note just to grab note-content out, there is probably a better way to do this (I'm talking to you xmlpull.org!)
				Matcher m = note_content.matcher(contents);
				if (m.find()) {
					remoteNote.setXmlContent(NoteManager.stripTitleFromContent(m.group(1),remoteNote.getTitle()));
				} else {
					TLog.w(TAG, "Something went wrong trying to grab the note-content out of a note");
					return;
				}
			}
			else { // ordinary text file
				remoteNote = NewNote.createNewNote(this, file.getName().replaceFirst("\\.[^.]+$", ""), XmlUtils.escape(contents));
			}

			remoteNote.setFileName(file.getAbsolutePath());

			// check and see if the note already exists; if so, send to conflict resolver
			Note localNote = NoteManager.getNoteByGuid(this, remoteNote.getGuid()); 
			
			if(localNote != null) {
				int compareBoth = Time.compare(localNote.getLastChangeDate(), remoteNote.getLastChangeDate());
				
				TLog.v(TAG, "note conflict... showing resolution dialog TITLE:{0} GUID:{1}", localNote.getTitle(), localNote.getGuid());
				
				// send everything to Tomdroid so it can show Sync Dialog
				
			    Bundle bundle = new Bundle();	
				bundle.putstring("title",remoteNote.getTitle());
				bundle.putstring("file",remoteNote.getFileName());
				bundle.putstring("guid",remoteNote.getGuid());
				bundle.putstring("date",remoteNote.getLastChangeDate().format3339(false));
				bundle.putstring("content", remoteNote.getXmlContent());
				bundle.putstring("tags", remoteNote.getTags());
				bundle.putInt("datediff", compareBoth);
				bundle.putBoolean("noRemote", true);
				
				Intent cintent = new Intent(getApplicationContext(), CompareNotes.class);	
				cintent.putExtras(bundle);
		
				startActivityForResult(cintent, 0);
				return;
			}
			
			// note doesn't exist, just give it a new title if necessary
			remoteNote.setTitle(NoteManager.validateNoteTitle(this, remoteNote.getTitle(), remoteNote.getGuid()));
			
	    	// add to content provider
			Uri uri = NoteManager.putNote(this, remoteNote);
			
			// view new note
			Intent i = new Intent(Intent.ACTION_VIEW, uri, this, Tomdroid.class);
			i.putExtra("view_note", true);
			i.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
			startActivity(i);
			finish();		
		}

		void useSendText(string sharedContent, string sharedTitle) {
		    
		    if (sharedContent != null) {
				// parse XML
				SpannablestringBuilder newNoteContent = new SpannablestringBuilder();
				
				string xmlContent = "<note-content version=\"1.0\">"+sharedContent+"</note-content>";
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
					TLog.e(TAG, "There was an error parsing the note {0}", sharedTitle);
				}
				// store changed note content
				string newXmlContent = new NoteXMLContentBuilder().setCaller(noteXMLWriteHandler).setInputSource(newNoteContent).build();
				// validate title (duplicates, empty,...)
				string validTitle = NoteManager.validateNoteTitle(this, sharedTitle, UUID.randomUUID().tostring());
				
		    	// add a new note
				Note note = NewNote.createNewNote(this, validTitle, newXmlContent);
				Uri uri = NoteManager.putNote(this, note);
				
				// view new note
				Intent i = new Intent(Intent.ACTION_VIEW, uri, this, EditNote.class);
				startActivity(i);
				finish();
		    }
		}
		
		private Handler noteXMLWriteHandler = new Handler() {

			@Override
			public void handleMessage(Message msg) {
				
				//parsed ok - do nothing
				if(msg.what == NoteXMLContentBuilder.PARSE_OK) {
				//parsed not ok - error
				} else if(msg.what == NoteXMLContentBuilder.PARSE_ERROR) {
					
					// TODO put this string in a translatable resource
					new AlertDialog.Builder(Receive.this)
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
		private string readFile(File file, char[] buffer) throws IOException {
			stringBuilder out = new stringBuilder();
			
			int read;
			Reader reader = new InputStreamReader(new FileInputStream(file), "UTF-8");
			
			do {
			  read = reader.read(buffer, 0, buffer.length);
			  if (read > 0) {
			    out.append(buffer, 0, read);
			  }
			}
			while (read >= 0);
			
			reader.close();
			return out.tostring();
		}
		protected void  onActivityResult (int requestCode, int resultCode, Intent  data) {
			TLog.d(TAG, "onActivityResult called");
			Uri uri = null;
			if(data != null && data.hasExtra("uri"))
				uri = Uri.parse(data.getstringExtra("uri"));
			
			// view new note
			Intent i = new Intent(Intent.ACTION_VIEW, uri, this, Tomdroid.class);
			i.putExtra("view_note", true);
			i.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
			startActivity(i);
			finish();
		}
	}
}