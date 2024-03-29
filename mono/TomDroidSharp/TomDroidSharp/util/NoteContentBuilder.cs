/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2008, 2009, 2010 Olivier Bilodeau <olivier@bottomlesspit.org>
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


using Android.OS;
using Android.Text;

using System.Threading;
using System.Text;
using Java.Lang;


namespace TomDroidSharp.util
{

	public class NoteContentBuilder {
		
		public static readonly int PARSE_OK = 0;
		public static readonly int PARSE_ERROR = 1;
		
		private InputSource noteContentIs;
		
		// this is what we are building here
		private System.Text.StringBuilder noteContent = new System.Text.StringBuilder();
		
		private readonly string TAG = "NoteContentBuilder";
		
		// thread related
		private Runnable runner;
		private Handler parentHandler;
		private string subjectName;
		private string noteContentstring;
		
		public NoteContentBuilder () {}
		
		public NoteContentBuilder setCaller(Handler parent) {
			
			parentHandler = parent;
			return this;
		}
		
		/**
		 * Allows you to give a string that will be appended to errors in order to make them more useful.
		 * You'll probably want to set it to the Note's title.
		 * @param title
		 * @return this (builder pattern) 
		 */
		public NoteContentBuilder setTitle(string title) {
			
			subjectName = title;
			return this;
		}
		
		public NoteContentBuilder setInputSource(string nc) {
			
			noteContentstring = "<note-content>"+nc+"</note-content>";
			noteContentIs = new InputSource(new stringReader(noteContentstring));
			return this;
		}
		
		public System.Text.StringBuilder build() {
			
			runner = new Runnable() {
				
//				public void run() {
//					
//					
//					bool successful = true;
//					
//					try {
//						// Parsing
//				    	// XML 
//				    	// Get a SAXParser from the SAXPArserFactory
//				        SAXParserFactory spf = SAXParserFactory.newInstance();
//
//				        // trashing the namespaces but keep prefixes (since we don't have the xml header)
//				        spf.setFeature("http://xml.org/sax/features/namespaces", false);
//				        spf.setFeature("http://xml.org/sax/features/namespace-prefixes", true);
//				        SAXParser sp = spf.newSAXParser();
//
//						TLog.v(TAG, "parsing note {0}", subjectName);
//						
//				        sp.parse(noteContentIs, new NoteContentHandler(noteContent));
//					} catch (Exception e) {
//						e.PrintStackTrace();
//						// TODO handle error in a more granular way
//						TLog.e(TAG, "There was an error parsing the note {0}", noteContentstring);
//						successful = false;
//					}
//					
//					warnHandler(successful);
//				}
			};
			System.Threading.Thread thread = new System.Threading.Thread(runner);
			thread.Start();
			return noteContent;
		}

	    private void warnHandler(bool successful) {
			
			// notify the main UI that we are done here (sending an ok along with the note's title)
			Message msg = Message.Obtain();
			if (successful) {
				msg.What = PARSE_OK;
			} else {
				
				msg.What = PARSE_ERROR;
			}
			
			parentHandler.SendMessage(msg);
	    }
	}
}