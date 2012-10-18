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
using TomDroidSharp;
//import org.xml.sax.Attributes;
//import org.xml.sax.SAXException;
//import org.xml.sax.helpers.DefaultHandler;

using Android.Util;

namespace TomDroidSharp.sync.sd
{
	public class NoteHandler : DefaultHandler {

		private string TAG = "NoteHandler";

		// position keepers
		private bool inTitleTag = false;
		private bool inLastChangeDateTag = false;
		private bool inNoteContentTag = false;
		private bool inCreateDateTag = false;
		private bool inCursorTag = false;
		private bool inWidthTag = false;
		private bool inHeightTag = false;
		private bool inXTag = false;
		private bool inYTag = false;
		private bool inTagTag = false;

		// -- Tomboy's notes XML tags names --
		private readonly static string TITLE = "title";
		private readonly static string LAST_CHANGE_DATE = "last-change-date";
		private readonly static string NOTE_CONTENT = "note-content";
		private readonly static string CREATE_DATE = "create-date";
		private readonly static string NOTE_C = "cursor-position";
		private readonly static string NOTE_W = "width";
		private readonly static string NOTE_H = "height";
		private readonly static string NOTE_X = "x";
		private readonly static string NOTE_Y = "y";
		private readonly static string NOTE_TAG = "tag";
		
		// Buffers for parsed elements
		private stringBuilder title = new stringBuilder();
		private stringBuilder lastChangeDate = new stringBuilder();
		private stringBuilder noteContent = new stringBuilder();
		private stringBuilder createDate = new stringBuilder();
		private stringBuilder cursorPos = new stringBuilder();
		private stringBuilder width = new stringBuilder();
		private stringBuilder height = new stringBuilder();
		private stringBuilder X = new stringBuilder();
		private stringBuilder Y = new stringBuilder();
		private stringBuilder tag = new stringBuilder();
		
		// link to model 
		private Note note;

		
		public NoteHandler(Note note) {
			this.note = note;
		}
		
		@Override
		public void startElement(string uri, string localName, string name, Attributes attributes) throws SAXException {
			
			// TODO validate top-level tag for tomboy notes and throw exception if its the wrong version number (maybe offer to parse also?)		

			if (localName.equals(TITLE)) {
				inTitleTag = true;
			} 
			else if (localName.equals(LAST_CHANGE_DATE)) {
				inLastChangeDateTag = true;
			}
			else if (localName.equals(NOTE_CONTENT)) {
				inNoteContentTag = true;
			}
			else if (localName.equals(CREATE_DATE)) {
				inCreateDateTag = true;
			}
			else if (localName.equals(NOTE_C)) {
				inCursorTag = true;
			}
			else if (localName.equals(NOTE_W)) {
				inWidthTag = true;
			}
			else if (localName.equals(NOTE_H)) {
				inHeightTag = true;
			}
			else if (localName.equals(NOTE_X)) {
				inXTag = true;
			}
			else if (localName.equals(NOTE_Y)) {
				inYTag = true;
			}
			else if (localName.equals(NOTE_TAG)) {
				inTagTag = true;
			}
		}

		@Override
		public void endElement(string uri, string localName, string name)
				throws SAXException, TimeFormatException {

			if (localName.equals(TITLE)) {
				inTitleTag = false;
				note.setTitle(title.tostring());
			} 
			else if (localName.equals(LAST_CHANGE_DATE)) {
				inLastChangeDateTag = false;
				note.setLastChangeDate(lastChangeDate.tostring());
			}
			else if (localName.equals(NOTE_CONTENT)) {
				inNoteContentTag = false;
				note.setXmlContent(noteContent.tostring());
			}
			else if (localName.equals(CREATE_DATE)) {
				inCreateDateTag = false;
				if(createDate.length() > 0)
					note.setCreateDate(createDate.tostring());
			}
			else if (localName.equals(NOTE_C)) {
				inCursorTag = false;
				if(cursorPos.length() > 0)
					note.cursorPos = Integer.parseInt(cursorPos.tostring());
			}
			else if (localName.equals(NOTE_W)) {
				inWidthTag = false;
				if(width.length() > 0)
					note.width = Integer.parseInt(width.tostring());
			}
			else if (localName.equals(NOTE_H)) {
				inHeightTag = false;
				if(height.length() > 0)
					note.height = Integer.parseInt(height.tostring());
			}
			else if (localName.equals(NOTE_X)) {
				inXTag = false;
				if(X.length() > 0)
					note.X = Integer.parseInt(X.tostring());
			}
			else if (localName.equals(NOTE_Y)) {
				inYTag = false;
				if(Y.length() > 0)
					note.Y = Integer.parseInt(Y.tostring());
			}
			else if (localName.equals(NOTE_TAG)) {
				inTagTag = false;
				if(tag.length() > 0)
					note.addTag(tag.tostring());
			}
		}

		@Override
		public void characters(char[] ch, int start, int length)
				throws SAXException {
			
			if (inTitleTag) {
				title.append(ch, start, length);
			} 
			else if (inLastChangeDateTag) {
				lastChangeDate.append(ch, start, length);
			} 
			else if (inNoteContentTag) {
				noteContent.append(ch, start, length);
			}
			else if (inCreateDateTag) {
				createDate.append(ch, start, length);
			}
			else if (inCursorTag) {
				cursorPos.append(ch, start, length);
			}
			else if (inWidthTag) {
				width.append(ch, start, length);
			}
			else if (inHeightTag) {
				height.append(ch, start, length);
			}
			else if (inXTag) {
				X.append(ch, start, length);
			}
			else if (inYTag) {
				Y.append(ch, start, length);
			}
			else if (inTagTag) {
				tag.append(ch, start, length);
			}
		}
	}
}