/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2010 Olivier Bilodeau <olivier@bottomlesspit.org>
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

using Android.Content;
using Java.Util;

namespace TomDroidSharp.util
{
	/**
	 * Creates a new note object 
	 *
	 */
	public class NewNote {

		// Logging info
		private static readonly string	TAG = "NewNote";
		// indicates, if note was never saved before (for dismiss dialogue)
		public static bool neverSaved;
		
		public static Note createNewNote(Context context, string title, string xmlContent) {
			TLog.v(TAG, "Creating new note");
			
			Note note = new Note();
			neverSaved = true;
			
			note.setTitle(title);

			UUID newid = UUID.RandomUUID();
			note.setGuid(newid.ToString());
			note.setLastChangeDate();
			note.setXmlContent(xmlContent);
			
			return note;
		}

	}
}