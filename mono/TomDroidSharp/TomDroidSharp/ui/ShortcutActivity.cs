/**
 Tomdroid
 Tomboy on Android
 http://www.launchpad.net/tomdroid

 Copyright 2011 Piotr Adamski <mcveat@gmail.com>

 This file is part of Tomdroid.

 Tomdroid is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 Tomdroid is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Tomdroid.  If not, see <http://www.gnu.org/licenses/>.
 */

using Android.App;
using Android.Database;
using Android.OS;
using Android.Views;
using Android.Widget;

using TomDroidSharp.ui.actionbar;
using TomDroidSharp.util;

namespace TomDroidSharp.ui
{
	/**
	 * @author Piotr Adamski <mcveat@gmail.com>
	 */
	[Activity (Label = "PreferencesActivity")]
	public class ShortcutActivity : ActionBarListActivity
	{
		private readonly string TAG = "com.TomDroidSharp.ShortcutActivity";
	    private ListAdapter adapter;

	    protected override void onCreate(Bundle savedInstanceState) {
	        base.onCreate(savedInstanceState);
	        Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);
	        TLog.d(TAG, "creating shortcut...");
	        SetContentView(Resource.Layout.shortcuts_list);
			Title = Resource.String.shortcuts_view_caption;
	        adapter = NoteManager.getListAdapter(this);
			ListAdapter = adapter;

			ListView.EmptyView = FindViewById<View>(Resource.Id.list_empty);
		}

	    protected override void onListItemClick(ListView l, View v, int position, long id) {
			ICursor item = (ICursor) adapter.Item[position];
	        NoteViewShortcutsHelper helper = new NoteViewShortcutsHelper(this);
			SetResult(Result.Ok, helper.getCreateShortcutIntent(item));
	        Finish();
	    }
	}
}