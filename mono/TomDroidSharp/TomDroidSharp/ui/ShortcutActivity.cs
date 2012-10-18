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

using Android.Database;
using Android.OS;
using Android.Views;
using Android.Widget;

using TomDroidSharp.NoteManager;
using TomDroidSharp.R;
using TomDroidSharp.ui.actionbar.ActionBarListActivity;
using TomDroidSharp.util.NoteViewShortcutsHelper;
using TomDroidSharp.util.Preferences;
using TomDroidSharp.util.TLog;


namespace TomDroidSharp.ui
{
	/**
	 * @author Piotr Adamski <mcveat@gmail.com>
	 */
	public class ShortcutActivity : ActionBarListActivity {
	    private readonly string TAG = ShortcutActivity.class.getName();
	    private ListAdapter adapter;

	    @Override
	    protected void onCreate(final Bundle savedInstanceState) {
	        super.onCreate(savedInstanceState);
	        Preferences.init(this, Tomdroid.CLEAR_PREFERENCES);
	        TLog.d(TAG, "creating shortcut...");
	        setContentView(R.layout.shortcuts_list);
	        setTitle(R.string.shortcuts_view_caption);
	        adapter = NoteManager.getListAdapter(this);
	        setListAdapter(adapter);
	        getListView().setEmptyView(findViewById(R.id.list_empty));

	    }

	    @Override
	    protected void onListItemClick(final ListView l, View v, int position, long id) {
	        Cursor item = (Cursor) adapter.getItem(position);
	        NoteViewShortcutsHelper helper = new NoteViewShortcutsHelper(this);
	        setResult(RESULT_OK, helper.getCreateShortcutIntent(item));
	        finish();
	    }
	}
}