/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2010, Benoit Garret <benoit.garret_launchpad@gadz.org>
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
using System.Collections.Generic;
using System;
using Java.IO;
using TomDroidSharp.ui;

namespace TomDroidSharp.Util
{
	public class ErrorList : LinkedList<Dictionary<string, Object>> {
		
		// Eclipse wants this, let's grant his wish
		private static readonly long serialVersionUID = 2442181279736146737L;
		
		private class Error : Dictionary<string, Object> {
			
			// Eclipse wants this, let's grant his wish
			private static readonly long serialVersionUID = -8279130686438869537L;

			public Error addError(Exception e ) {
				Writer result = new stringWriter();
				PrintWriter printWriter = new PrintWriter(result);
				e.PrintStackTrace(printWriter);
				this.Add("error", result.ToString());
				return this;
			}

			public Error addError(string message) {
				this.Add("error", message);
				return this;
			}
			
			public Error addNote(Note note) {
				this.Add("label", note.getTitle());
				this.Add("filename", new File(note.getFileName()).Name);
				return this;
			}
			
			public Error addObject(string key, Object o) {
				this.Add(key, o);
				return this;
			}
		}
		
		public static Dictionary<string, Object> createError(Note note, Exception e) {
			return new Error()
				.addNote(note)
				.addError(e);
		}
		
		public static Dictionary<string, Object> createError(string label, string filename, Exception e) {
			return new Error()
				.addError(e)
				.addObject("label", label)
				.addObject("filename", filename);
		}
		
		public static Dictionary<string, Object> createErrorWithContents(Note note, Exception e, string noteContents) {
			return new Error()
				.addNote(note)
				.addError(e)
				.addObject("note-content", noteContents);
		}
		
		public static Dictionary<string, Object> createErrorWithContents(Note note, string message, string noteContents) {
			return new Error()
				.addNote(note)
				.addError(message)
				.addObject("note-content", noteContents);
		}
		
		public static Dictionary<string, Object> createErrorWithContents(string label, string filename, Exception e, string noteContents) {
			return new Error()
				.addObject("label", label)
				.addObject("filename", filename)
				.addError(e)
				.addObject("note-content", noteContents);
		}
		
		/**
		 * Saves the error list in an "errors" directory located in the notes directory.
		 * Both the exception and the note content are saved.
		 * @return true if the save was successful, false if it wasn't
		 */
		public bool save() {
			string path = Tomdroid.NOTES_PATH+"errors/";
			
			File fPath = new File(path);
			if (!fPath.Exists()) {
				fPath.Mkdirs();
				// Check a second time, if not the most likely cause is the volume doesn't exist
				if(!fPath.Exists()) return false;
			}
			
			if(this == null || this.isEmpty() || this.Count == 0)
				return false;
			
			for(int i = 0; i < this.Count; i++) {
				Dictionary<string, Object> error = this.get(i);
				if(error == null)
					continue;
				string filename = findFilename(path, (string)error.get("filename"), 0);
				
				try {
					FileWriter fileWriter;
					string content = (string)error.get("note-content");
					
					if(content != null) {
						fileWriter = new FileWriter(path+filename);
						fileWriter.Write(content);
						fileWriter.Flush();
						fileWriter.Close();
					}
					
					fileWriter = new FileWriter(path+filename+".exception");
					fileWriter.Write((string)error.get("error"));
					fileWriter.Flush();
					fileWriter.Close();
				} catch (FileNotFoundException e) {
				 // TODO Auto-generated catch block
					e.PrintStackTrace();
				} catch (IOException e) {
				 // TODO Auto-generated catch block
					e.PrintStackTrace();
				}
			}
			
			return true;
		}
		
		/**
		 * Finds a not existing filename to write the error.
		 * @param path The directory in which to save the error
		 * @param baseName The base filename of the error
		 * @param level The number that get appended to the filename
		 * @return A filename that doesn't Exists in the path directory
		 */
		private string findFilename(string path, string baseName, int level) {
			
			if(level < 0) level = 0;
			
			string suffix = ""+(level == 0 ? "" : level);
			string filePath = path+baseName+suffix;
			File file = new File(filePath);
			
			return file.Exists() ? findFilename(path, baseName, level + 1) : baseName+suffix;		
		}
	}
}