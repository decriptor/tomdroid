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
using System.Xml;

using TomDroidSharp;
using Android.Util;

namespace TomDroidSharp.Sync.sd
{
	public class NoteHandler
	{
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
		private StringBuilder lastChangeDate = new StringBuilder();
		private StringBuilder title = new StringBuilder();
		private StringBuilder noteContent = new StringBuilder();
		private StringBuilder createDate = new StringBuilder();
		private StringBuilder cursorPos = new StringBuilder();
		private StringBuilder width = new StringBuilder();
		private StringBuilder height = new StringBuilder();
		private StringBuilder X = new StringBuilder();
		private StringBuilder Y = new StringBuilder();
		private StringBuilder tag = new StringBuilder();
		
		// link to model 
		private Note note;

		
		public NoteHandler(Note note) {
			this.note = note;
		}

		public void startElement(string uri, string localName, string name, Attribute attributes)
		{
			try {
				// TODO validate top-level tag for tomboy notes and throw exception if its the wrong version number (maybe offer to parse also?)		

				if (localName.Equals(TITLE)) {
					inTitleTag = true;
				} 
				else if (localName.Equals(LAST_CHANGE_DATE)) {
					inLastChangeDateTag = true;
				}
				else if (localName.Equals(NOTE_CONTENT)) {
					inNoteContentTag = true;
				}
				else if (localName.Equals(CREATE_DATE)) {
					inCreateDateTag = true;
				}
				else if (localName.Equals(NOTE_C)) {
					inCursorTag = true;
				}
				else if (localName.Equals(NOTE_W)) {
					inWidthTag = true;
				}
				else if (localName.Equals(NOTE_H)) {
					inHeightTag = true;
				}
				else if (localName.Equals(NOTE_X)) {
					inXTag = true;
				}
				else if (localName.Equals(NOTE_Y)) {
					inYTag = true;
				}
				else if (localName.Equals(NOTE_TAG)) {
					inTagTag = true;
				}
			}
			catch (XmlException ex)
			{
			}
		}

		public void endElement(string uri, string localName, string name)
		{
			try {
			if (localName.Equals(TITLE)) {
				inTitleTag = false;
				note.setTitle(title.ToString());
			} 
			else if (localName.Equals(LAST_CHANGE_DATE)) {
				inLastChangeDateTag = false;
				note.setLastChangeDate(lastChangeDate.ToString());
			}
			else if (localName.Equals(NOTE_CONTENT)) {
				inNoteContentTag = false;
				note.setXmlContent(noteContent.ToString());
			}
			else if (localName.Equals(CREATE_DATE)) {
				inCreateDateTag = false;
				if(createDate.Length > 0)
					note.setCreateDate(createDate.ToString());
			}
			else if (localName.Equals(NOTE_C)) {
				inCursorTag = false;
				if(cursorPos.Length > 0)
					note.cursorPos = Int32.Parse(cursorPos.ToString());
			}
			else if (localName.Equals(NOTE_W)) {
				inWidthTag = false;
				if(width.Length > 0)
					note.width = Int32.Parse(width.ToString());
			}
			else if (localName.Equals(NOTE_H)) {
				inHeightTag = false;
				if(height.Length > 0)
					note.height = Int32.Parse(height.ToString());
			}
			else if (localName.Equals(NOTE_X)) {
				inXTag = false;
				if(X.Length > 0)
					note.X = Int32.Parse(X.ToString());
			}
			else if (localName.Equals(NOTE_Y)) {
				inYTag = false;
				if(Y.Length > 0)
					note.Y = Int32.Parse(Y.ToString());
			}
			else if (localName.Equals(NOTE_TAG)) {
				inTagTag = false;
				if(tag.Length > 0)
					note.addTag(tag.ToString());
			}
			}
			catch(XmlException ex)
			{
			}
			catch(TimeFormatException tfe)
			{

			}
		}

		public void characters(char[] ch, int start, int length)
		{
			try {
			if (inTitleTag) {
				title.Append(ch, start, length);
			} 
			else if (inLastChangeDateTag) {
				lastChangeDate.Append(ch, start, length);
			} 
			else if (inNoteContentTag) {
				noteContent.Append(ch, start, length);
			}
			else if (inCreateDateTag) {
				createDate.Append(ch, start, length);
			}
			else if (inCursorTag) {
				cursorPos.Append(ch, start, length);
			}
			else if (inWidthTag) {
				width.Append(ch, start, length);
			}
			else if (inHeightTag) {
				height.Append(ch, start, length);
			}
			else if (inXTag) {
				X.Append(ch, start, length);
			}
			else if (inYTag) {
				Y.Append(ch, start, length);
			}
			else if (inTagTag) {
				tag.Append(ch, start, length);
			}
			}
			catch (XmlException ex)
			{
			}
		}
	}
}