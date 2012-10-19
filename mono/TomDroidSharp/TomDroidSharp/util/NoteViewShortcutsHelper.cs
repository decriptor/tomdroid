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

using Android.Content;
using Android.Database;
using Android.Net;

using TomDroidSharp.ui;

namespace TomDroidSharp.util
{
/**
 * @author Piotr Adamski <mcveat@gmail.com>
 */
	public class NoteViewShortcutsHelper 
	{
	    private readonly Context context;

	    public NoteViewShortcutsHelper(Context context) {
	        this.context = context;
	    }

	    public Intent getCreateShortcutIntent(ICursor item) {
	        string name = getNoteTitle(item);
	        Uri uri = Tomdroid.getNoteIntentUri(getNoteId(item));
	        return getCreateShortcutIntent(name, uri);
	    }

	    private Intent getCreateShortcutIntent(string name, Uri uri) {
	        Intent i = new Intent();
	        i.PutExtra(Intent.ExtraShortcutIntent, getNoteViewShortcutIntent(name, uri));
	        i.PutExtra(Intent.ExtraShortcutIntent, name);
	        Intent.ShortcutIconResource icon = fromContext(context, Resource.Drawable.ic_shortcut);
	        i.PutExtra(Intent.ExtraShortcutIconResource, icon);
	        return i;
	    }

	    public Intent getBroadcastableCreateShortcutIntent(Uri uri, string name) {
	        Intent i = getCreateShortcutIntent(name, uri);
	        i.SetAction("com.android.launcher.action.INSTALL_SHORTCUT");
	        return i;
	    }

	    public Intent getRemoveShortcutIntent(string name, Uri uri) {
	        Intent i = new Intent();
	        i.PutExtra(Intent.ExtraShortcutIntent, getNoteViewShortcutIntent(name, uri));
	        i.PutExtra(Intent.ExtraShortcutName, name);
	        i.SetAction("com.android.launcher.action.UNINSTALL_SHORTCUT");
	        return i;
	    }

	    private Intent getNoteViewShortcutIntent(string name, Uri intentUri) {
	        Intent i = new Intent(Intent.ActionView, intentUri, context, typeof(ViewNote));
	        i.PutExtra(ViewNote.CALLED_FROM_SHORTCUT_EXTRA, true);
	        i.PutExtra(ViewNote.SHORTCUT_NAME, name);
	        return i;
	    }

	    private string getNoteTitle(ICursor item) {
			return item.GetString(item.GetColumnIndexOrThrow(Note.TITLE));
	    }

	    private int getNoteId(ICursor item) {
	        return item.GetInt(item.GetColumnIndexOrThrow(Note.ID));
	    }
	}
}