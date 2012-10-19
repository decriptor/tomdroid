/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2010, Matthew Stevenson <saturnreturn@gmail.com>
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

//import java.text.DateFormat;
//import java.util.Date;

using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Text.Format;
using Android.Views;
using Android.Widget;

/* Provides a custom ListView layout for Note List */

namespace TomDroidSharp.util
{

	public class NoteListCursorAdapter : SimpleCursorAdapter {

		// static properties
		private static readonly string TAG = "NoteListCursorAdapter";

		
	    private int layout;
	    private Context context;

	    private DateFormat localeDateFormat;
	    private DateFormat localeTimeFormat;
	    
	    private int selectedIndex;


	    public NoteListCursorAdapter (Context context, int layout, ICursor c, string[] from, int[] to, int selectedIndex) : base(context, layout, c, from, to) {
	        this.layout = layout;
	        this.context = context;
	        this.selectedIndex = selectedIndex;
	        
	        localeDateFormat = DateFormat.getDateFormat(context);
	        localeTimeFormat = DateFormat.getTimeFormat(context);
	    }
	    

	    public override View newView(Context context, ICursor cursor, ViewGroup parent) {

	        Cursor c = getCursor();

	        LayoutInflater inflater = LayoutInflater.From(context);
	        View v = inflater.inflate(layout, parent, false);

	        populateFields(v, c);

	        return v;
	    }

	    public override void bindView(View v, Context context, ICursor c) {

	        populateFields(v, c);
	    }
	    
		public override View getView(int position, View convertView, ViewGroup parent) {
	    	View v = base.getView(position, convertView, parent);
	    	if(this.selectedIndex == position) {
	            TextView note_title = (TextView) v.FindViewById(Resource.Id.note_title);
	            if (note_title != null) {
	            	note_title.setTextColor(0xFFFFFFFF);
	            }
	            TextView note_modified = (TextView) v.FindViewById(Resource.Id.note_date);
	            if (note_modified != null) {
	            	note_modified.setTextColor(0xFFFFFFFF);
	            }
	    		v.setBackgroundResource(Resource.Drawable.drop_shadow_selected);
	    		v.FindViewById(Resource.Id.triangle).setBackgroundResource(Resource.Drawable.white_triangle);
	    	}
	    	else {
	            TextView note_title = (TextView) v.FindViewById(Resource.Id.note_title);
	            if (note_title != null) {
	            	note_title.setTextColor(0xFF000000);
	            }
	            TextView note_modified = (TextView) v.FindViewById(Resource.Id.note_date);
	            if (note_modified != null) {
	            	note_modified.setTextColor(0xFF000000);
	            }
	    		v.setBackgroundResource(0);
	    		v.FindViewById(Resource.Id.triangle).setBackgroundResource(0);
	    	}
	    	return v;
		}
	    
	    private void populateFields(View v, Cursor c){

	        int nameCol = c.getColumnIndex(Note.TITLE);
	        int modifiedCol = c.getColumnIndex(Note.MODIFIED_DATE);
	        int tagCol = c.getColumnIndex(Note.TAGS);
	        
	        string title = c.GetString(nameCol);
	        string tags = c.GetString(tagCol);
	        
	        //Format last modified dates to be similar to desktop Tomboy
	        //TODO this is messy - must be a better way than having 3 separate date types
	        Time lastModified = new Time();
	        lastModified.parse3339(c.GetString(modifiedCol));
	        Long lastModifiedMillis = lastModified.toMillis(false);
	        Date lastModifiedDate = new Date(lastModifiedMillis);
	        
	        string strModified = this.context.GetString(Resource.String.textModified)+" ";
	        //TODO this is very inefficient
	        if (DateUtils.isToday(lastModifiedMillis)){
	        	strModified += this.context.GetString(Resource.String.textToday) +", " + localeTimeFormat.format(lastModifiedDate);
	        } else {
	        	// Add a day to the last modified date - if the date is now today, it means the note was edited yesterday
	        	Time yesterdayTest = lastModified;
	        	yesterdayTest.monthDay += 1;
	        	if (DateUtils.isToday(yesterdayTest.toMillis(false))){
	        		strModified += this.context.GetString(Resource.String.textYexterday) +", " + localeTimeFormat.format(lastModifiedDate);
	        	} else {
	        		strModified += localeDateFormat.format(lastModifiedDate) + ", " + localeTimeFormat.format(lastModifiedDate);
	        	}
	        }

	        /**
	         * Next set the name of the entry.
	         */
	        TextView note_title = (TextView) v.FindViewById(Resource.Id.note_title);
	        if (note_title != null) {
	        	note_title.SetText(title);
	            if(tags.contains("system:deleted"))
	            	note_title.setPaintFlags(note_title.getPaintFlags() | Paint.STRIKE_THRU_TEXT_FLAG);
	            else
	            	note_title.setPaintFlags(note_title.getPaintFlags() & ~Paint.STRIKE_THRU_TEXT_FLAG);
	        }
	        TextView note_modified = (TextView) v.FindViewById(Resource.Id.note_date);
	        if (note_modified != null) {
	        	note_modified.SetText(strModified);
	        }
	    }

	}
}