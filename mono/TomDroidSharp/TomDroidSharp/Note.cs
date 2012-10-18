/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2008, 2009, 2010, 2011 Olivier Bilodeau <olivier@bottomlesspit.org>
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
using System.Json;
using System.Runtime.Serialization;

using Android;
using Android.OS;
using Android.Text;
using Android.Text.Format;
//import org.json.JSONArray;
//import org.json.JSONObject;

using Android.Util;
using TomDroidSharp.util;
using Java.Util.Regex;

namespace TomDroidSharp
{
	public class Note : ISerializable
	{

		// Static references to fields (used in Bundles, ContentResolvers, etc.)
		public static readonly string ID = "_id";
		public static readonly string GUID = "guid";
		public static readonly string TITLE = "title";
		public static readonly string MODIFIED_DATE = "modified_date";
		public static readonly string URL = "url";
		public static readonly string FILE = "file";
		public static readonly string TAGS = "tags";
		public static readonly string NOTE_CONTENT = "content";
		public static readonly string NOTE_CONTENT_PLAIN = "content_plain";
		
		// Notes constants
		public static readonly int NOTE_HIGHLIGHT_COLOR = 0x99FFFF00; // lowered alpha to show cursor
		public static readonly string NOTE_MONOSPACE_TYPEFACE = "monospace";
		public static readonly float NOTE_SIZE_SMALL_FACTOR = 0.8f;
		public static readonly float NOTE_SIZE_LARGE_FACTOR = 1.5f;
		public static readonly float NOTE_SIZE_HUGE_FACTOR = 1.8f;
		
		// Members
		private SpannableStringBuilder noteContent;
		private string xmlContent;
		private string url;
		private string fileName;
		private string title;
		private string tags = "";
		private string lastChangeDate;
		private int dbId;

		// Unused members (for SD Card)
		
		public string createDate = new Time().Format3339(false);
		public int cursorPos = 0;
		public int height = 0;
		public int width = 0;
		public int X = -1;
		public int Y = -1;

		
		// TODO before guid were of the UUID object type, now they are simple strings 
		// but at some point we probably need to validate their uniqueness (per note collection or universe-wide?) 
		private string guid;
		
		// this is to tell the sync service to update the last date after pushing this note
		public bool lastSync = false;
		
		// Date converter pattern (remove extra sub milliseconds from datetime string)
		// ex: will strip 3020 in 2010-01-23T12:07:38.7743020-05:00
		private static readonly Java.Util.Regex.Pattern dateCleaner = Java.Util.Regex.Pattern.Compile(
				"(\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3})" +	// matches: 2010-01-23T12:07:38.774
				".+" + 														// matches what we are getting rid of
				"([-\\+]\\d{2}:\\d{2})");									// matches timezone (-xx:xx or +xx:xx)
		
		public Note() {
			tags = new String();
		}

		public Note(JsonObject json)
		{	
			// These methods return an empty string if the key is not found
			setTitle(XmlUtils.unescape(json.Keys["title"]));
			setGuid(json.Keys["guid"]);
			setLastChangeDate(json.Keys["last-change-date"]);
			String newXMLContent = json.Keys["note-content"];
			setXmlContent(newXMLContent);
			JsonArray jtags = json.Keys["tags"];
			String tag;
			tags = new String();
			if (jtags != null) {
				for (int i = 0; i < jtags.Count; i++ ) {
					tag = jtags[i];
					tags += tag + ",";
				}
			}
		}

		public string getTags() {
			return tags;
		}
		
		public void setTags(String tags) {
			this.tags = tags;
		}
		
		public void addTag(String tag) {
			if(tags.Length > 0)
				this.tags = this.tags+","+tag;
			else
				this.tags = tag;
		}
		
		public void removeTag(String tag)
		{	
			string[] taga = TextUtils.Split(this.tags, ",");
			String newTags = "";
			foreach(string atag in taga)
			{
				if(atag != tag)
					newTags += atag;
			}
			this.tags = newTags;
		}

		public string getUrl() {
			return url;
		}

		public void setUrl(String url) {
			this.url = url;
		}

		public string getFileName() {
			return fileName;
		}

		public void setFileName(String fileName) {
			this.fileName = fileName;
		}

		public string getTitle() {
			return title;
		}

		public void setTitle(String title) {
			this.title = title;
		}

		public Time getLastChangeDate() {
			Time time = new Time();
			time.Parse3339(lastChangeDate);
			return time;
		}
		
		// sets change date to now
		public void setLastChangeDate() {
			Time now = new Time();
			now.SetToNow();
			String time = now.Format3339(false);
			setLastChangeDate(time);
		}
		
		public void setLastChangeDate(Time lastChangeDateTime) {
			this.lastChangeDate = lastChangeDateTime.Format3339(false);
		}
		
		public void setLastChangeDate(String lastChangeDateStr)
		{
			try
			{
				// regexp out the sub-milliseconds from tomboy's datetime format
				// Normal RFC 3339 format: 2008-10-13T16:00:00.000-07:00
				// Tomboy's (C# library) format: 2010-01-23T12:07:38.7743020-05:00
				Matcher m = dateCleaner.Matcher(lastChangeDateStr);
				if (m.Find()) {
					//TLog.d(TAG, "I had to clean out extra sub-milliseconds from the date");
					lastChangeDateStr = m.Group(1)+m.Group(2);
					//TLog.v(TAG, "new date: {0}", lastChangeDateStr);
				}
				
				this.lastChangeDate = lastChangeDateStr;
			}
			catch(TimeFormatException tfe)
			{
			}	
		}	

		public void setCreateDate(String createDateStr)
		{
			try
			{

				// regexp out the sub-milliseconds from tomboy's datetime format
				// Normal RFC 3339 format: 2008-10-13T16:00:00.000-07:00
				// Tomboy's (C# library) format: 2010-01-23T12:07:38.7743020-05:00
				Matcher m = dateCleaner.Matcher(createDateStr);
				if (m.Find()) {
					//TLog.d(TAG, "I had to clean out extra sub-milliseconds from the date");
					createDateStr = m.Group(1)+m.Group(2);
					//TLog.v(TAG, "new date: {0}", lastChangeDateStr);
				}
				
				this.createDate = createDateStr;
				}
			catch(TimeFormatException tfe)
			{
			}
		}
		
		public int getDbId() {
			return dbId;
		}

		public void setDbId(int id) {
			this.dbId = id;
		}
		
		public string getGuid() {
			return guid;
		}
		
		public void setGuid(String guid) {
			this.guid = guid;
		}

		// TODO: should this handler passed around evolve into an observer pattern?
		public SpannableStringBuilder getNoteContent(Handler handler) {
			
			// TODO not sure this is the right place to do this
			noteContent = new NoteContentBuilder().setCaller(handler).setInputSource(xmlContent).setTitle(this.getTitle()).build();
			return noteContent;
		}
		
		public string getXmlContent() {
			return xmlContent;
		}
		
		public void setXmlContent(String xmlContent) {
			this.xmlContent = xmlContent;
		}

		public override string ToString()
		{
			return "Note: "+ getTitle() + " (" + getLastChangeDate() + ")";
		}
		
		// gets full xml to be exported as .note file
		public string getXmlFileString()
		{	
			string tagString = "";

			if(tags.Length >0) {
				string[] tagsA = tags.Split(",");
				tagString = "\n\t<tags>";
				foreach(string atag in tagsA)
				{
					tagString += "\n\t\t<tag>"+atag+"</tag>"; 
				}
				tagString += "\n\t</tags>"; 
			}

			// TODO: create-date
			string fileString = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<note version=\"0.3\" xmlns:link=\"http://beatniksoftware.com/tomboy/link\" xmlns:size=\"http://beatniksoftware.com/tomboy/size\" xmlns=\"http://beatniksoftware.com/tomboy\">\n\t<title>"
					+getTitle().Replace("&", "&amp;")+"</title>\n\t<text xml:space=\"preserve\"><note-content version=\"0.1\">"
					+getTitle().Replace("&", "&amp;")+"\n\n" // added for compatibility
					+getXmlContent()+"</note-content></text>\n\t<last-change-date>"
					+getLastChangeDate().Format3339(false)+"</last-change-date>\n\t<last-metadata-change-date>"
					+getLastChangeDate().Format3339(false)+"</last-metadata-change-date>\n\t<create-date>"
					+createDate+"</create-date>\n\t<cursor-position>"
					+cursorPos+"</cursor-position>\n\t<width>"
					+width+"</width>\n\t<height>"
					+height+"</height>\n\t<x>"
					+X+"</x>\n\t<y>"
					+Y+"</y>"
					+tagString+"\n\t<open-on-startup>False</open-on-startup>\n</note>\n";
			return fileString;
		}

	}
}