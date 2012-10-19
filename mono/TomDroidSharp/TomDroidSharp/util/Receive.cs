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

//import org.xml.sax.InputSource;
//import org.xml.sax.XMLReader;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Widget;
using TomDroidSharp.ui.actionbar;
using Java.IO;
using System;
using TomDroidSharp.ui;
using System.Text;
using Android.Text.Format;
using Java.Util.Regex;
using Java.Util;

namespace TomDroidSharp.util
{
	public class Receive : ActionBarActivity {
		
		// Logging info
		private static readonly string TAG = "ReceiveActivity";

		// don't import files bigger than this 
		private long MAX_FILE_SIZE = 1048576; // 1MB 

		protected void onCreate (Bundle savedInstanceState) {
			base.onCreate(savedInstanceState);

			// init preferences
			Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);

			// set intent, action, MIME type
		    Intent intent = Intent;
			string action = intent.Action;
			string type = intent.Type;

			TLog.v(TAG, "Receiving note of type {0}",type);
			TLog.d(TAG, "Action type: {0}",action);
		    
	    	if(intent.Data != null) {
	    		TLog.d(TAG, "Receiving file from path: {0}",intent.Data.Path);
				File file = new File(intent.Data.Path);

				if(file.Length > MAX_FILE_SIZE ) {
		    		Toast.MakeText(this, GetString(Resource.String.messageFileTooBig), ToastLength.Short).Show();
					Finish();
				}
				else {
					
					char[] buffer = new char[0x1000];
					
					// Try reading the file first
					string contents = "";
					try {
		
						contents = readFile(file,buffer);
					} catch (IOException e) {
						e.PrintStackTrace();
						TLog.w(TAG, "Something went wrong trying to read the note");
						Finish();
					}
					
					useSendFile(file, contents);
				}
	    	}
	    	else if (Intent.ActionSend.Equals(action) && type != null && "text/plain".Equals(type)) {
	    		TLog.v(TAG, "receiving note as plain text");
	    	    string sharedContent = intent.GetStringExtra(Intent.ExtraText);
	    	    string sharedTitle = intent.GetStringExtra(Intent.ExtraSubject);
	            useSendText(sharedContent, sharedTitle); // use the text being sent
	        }
	    	else {
	    		TLog.v(TAG, "received invalid note");
				Finish();
	    	}
		}
		void useSendFile(File file, string contents) {
			Note remoteNote = new Note();

			if(file.Path.EndsWith(".note") && contents.StartsWith("<?xml")) { // xml note file
				
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
			        InputSource inputSource = new InputSource(sr);
			        
					TLog.d(TAG, "parsing note");
					xr.parse(inputSource);
		
				// TODO wrap and throw a new exception here
				} catch (Exception e) {
					e.PrintStackTrace();
					if(e as TimeFormatException) TLog.e(TAG, "Problem parsing the note's date and time");
					Finish();
				}
				// the note guid is not stored in the xml but in the filename
				remoteNote.setGuid(file.Name.Replace(".note", ""));
				Java.Util.Regex.Pattern note_content = Java.Util.Regex.Pattern.Compile("<note-content[^>]+>(.*)<\\/note-content>", Pattern.CASE_INSENSITIVE+Pattern.DOTALL);

				// FIXME here we are re-reading the whole note just to grab note-content out, there is probably a better way to do this (I'm talking to you xmlpull.org!)
				Matcher m = note_content.Matcher(contents);
				if (m.Find()) {
					remoteNote.setXmlContent(NoteManager.stripTitleFromContent(m.Group(1),remoteNote.getTitle()));
				} else {
					TLog.w(TAG, "Something went wrong trying to grab the note-content out of a note");
					return;
				}
			}
			else { // ordinary text file
				remoteNote = NewNote.createNewNote(this, file.getName().replaceFirst("\\.[^.]+$", ""), XmlUtils.escape(contents));
			}

			remoteNote.setFileName(file.AbsolutePath);

			// check and see if the note already Exists; if so, send to conflict resolver
			Note localNote = NoteManager.getNoteByGuid(this, remoteNote.getGuid()); 
			
			if(localNote != null) {
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
				bundle.PutBoolean("noRemote", true);
				
				Intent cintent = new Intent(ApplicationContext, typeof(CompareNotes));	
				cintent.PutExtras(bundle);
		
				StartActivityForResult(cintent, 0);
				return;
			}
			
			// note doesn't exist, just give it a new title if necessary
			remoteNote.setTitle(NoteManager.validateNoteTitle(this, remoteNote.getTitle(), remoteNote.getGuid()));
			
	    	// add to content provider
			Android.Net.Uri uri = NoteManager.putNote(this, remoteNote);
			
			// view new note
			Intent i = new Intent(Intent.ActionView, uri, this, typeof(Tomdroid));
			i.PutExtra("view_note", true);
			i.AddFlags(ActivityFlags.ClearTop);
			StartActivity(i);
			Finish();		
		}

		void useSendText(string sharedContent, string sharedTitle) {
		    
		    if (sharedContent != null) {
				// parse XML
				StringBuilder newNoteContent = new StringBuilder();
				
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
					e.PrintStackTrace();
					// TODO handle error in a more granular way
					TLog.e(TAG, "There was an error parsing the note {0}", sharedTitle);
				}
				// store changed note content
				string newXmlContent = new NoteXMLContentBuilder().setCaller(noteXMLWriteHandler).setInputSource(newNoteContent).build();
				// validate title (duplicates, empty,...)
				string validTitle = NoteManager.validateNoteTitle(this, sharedTitle, UUID.RandomUUID().ToString());
				
		    	// add a new note
				Note note = NewNote.createNewNote(this, validTitle, newXmlContent);
				Android.Net.Uri uri = NoteManager.putNote(this, note);
				
				// view new note
				Intent i = new Intent(Intent.ActionView, uri, this, typeof(EditNote));
				StartActivity(i);
				Finish();
		    }
		}
		
//		private Handler noteXMLWriteHandler = new Handler() {
//
//			public override void handleMessage(Message msg) {
//				
//				//parsed ok - do nothing
//				if(msg.what == NoteXMLContentBuilder.PARSE_OK) {
//				//parsed not ok - error
//				} else if(msg.what == NoteXMLContentBuilder.PARSE_ERROR) {
//					
//					// TODO put this string in a translatable resource
//					new AlertDialog.Builder(Receive.this)
//						.setMessage("The requested note could not be parsed. If you see this error " +
//									" and you are able to replicate it, please file a bug!")
//						.setTitle("Error")
//						.setNeutralButton("Ok", new OnClickListener() {
//							public void onClick(DialogInterface dialog, int which) {
//								dialog.dismiss();
//								Finish();
//							}})
//						.Show();
//	        	}
//			}
//		};

		private string readFile(File file, char[] buffer)
		{
			StringBuilder outStuff = new StringBuilder();
			try
			{
				
				int read;
				Reader reader = new InputStreamReader(new FileInputStream(file), "UTF-8");
				
				do {
				  read = reader.Read(buffer, 0, buffer.Length);
				  if (read > 0) {
				    outStuff.Append(buffer, 0, read);
				  }
				}
				while (read >= 0);
				
				reader.Close();
			}
			catch( IOException ex)
			{

			}
			return outStuff.ToString();
		}

		protected void  onActivityResult (int requestCode, int resultCode, Intent  data) {
			TLog.d(TAG, "onActivityResult called");
			Android.Net.Uri uri = null;
			if(data != null && data.HasExtra("uri"))
				uri = Android.Net.Uri.Parse(data.GetStringExtra("uri"));
			
			// view new note
			Intent i = new Intent(Intent.ActionView, uri, this, typeof(Tomdroid));
			i.PutExtra("view_note", true);
			i.AddFlags(ActivityFlags.ClearTop);
			StartActivity(i);
			Finish();
		}
	}
}