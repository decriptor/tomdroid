/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2012 Koichi Akabe <vbkaisetsu@gmail.com>
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

using Android.App;
using System;
using Android.Content;
using TomDroidSharp.util;
using Android.Views;
using Android.Text.Style;


namespace TomDroidSharp.xml
{

	/*
	 * This class is responsible for parsing the xml note content
	 * and formatting the contents in a StringBuilder
	 */
	public class LinkInternalSpan : ClickableSpan {

		// Logging info
		private static readonly string TAG = "LinkInternalSpan";
		
		private string title;
		public LinkInternalSpan(string title) : base(){
			this.title = title;
		}

		public override void onClick(View v) {
			Activity act = (Activity)v.Context;
			int id = NoteManager.getNoteId(act, title);
			Uri intentUri;
			if(id != 0) {
				intentUri = Uri.Parse(Tomdroid.CONTENT_URI.ToString()+"/"+id);
			} else {
				/* TODO: open new note */
				TLog.d(TAG, "link: {0} was clicked", title);
				return;
			}
			Intent i = new Intent(Intent.ActionView, intentUri);
			act.StartActivity(i);
		}

		#region implemented abstract members of ClickableSpan
		public override void OnClick (View widget)
		{
			throw new NotImplementedException ();
		}
		#endregion
		
		public static MatchFilter getNoteLinkMatchFilter(StringBuilder noteContent, LinkInternalSpan[] links) {
			
			return new MatchFilter() {
				
	//			public bool acceptMatch(CharSequence s, int start, int end) {
	//				int spanstart, spanend;
	//				foreach(LinkInternalSpan link in links) {
	//					spanstart = noteContent.getSpanStart(link);
	//					spanend = noteContent.getSpanEnd(link);
	//					if(!(end <= spanstart || spanend <= start)) {
	//						return false;
	//					}
	//				}
	//				return true;
	//			}
	//		};
			};
		}
	}
}