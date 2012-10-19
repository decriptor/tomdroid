/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2011, Olivier Bilodeau <olivier@bottomlesspit.org>
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

using Android.Content;
using Android.Preferences;
using Android.Text;

namespace TomDroidSharp.util
{
	public class Preferences
	{		
//		public enum Key {
//			SYNC_SERVICE ("sync_service", "tomboy-web"),
//			SYNC_SERVER_ROOT_API ("sync_server_root_api", ""),
//			SYNC_SERVER_USER_API ("sync_server_user_api", ""),
//			SYNC_SERVER ("sync_server", "https://one.ubuntu.com/notes"),
//			SD_LOCATION ("sd_location", "tomdroid"),
//			LAST_FILE_PATH ("last_file_path", "/"),
//			INCLUDE_NOTE_TEMPLATES("include_note_templates", false),
//			INCLUDE_DELETED_NOTES("include_deleted_notes", false),
//			LINK_TITLES("link_titles", true),
//			LINK_URLS("link_urls", true),
//			LINK_EMAILS("link_emails", true),
//			LINK_PHONES("link_phones", true),
//			LINK_ADDRESSES("link_addresses", true),
//			DEL_ALL_NOTES("del_all_notes", ""),
//			DEL_REMOTE_NOTES("del_remote_notes", ""),
//			BACKUP_NOTES("backup_notes", ""),
//			AUTO_BACKUP_NOTES("auto_backup_notes", false),
//			RESTORE_NOTES("restore_notes", ""),
//			CLEAR_SEARCH_HISTORY ("clearSearchHistory", ""),
//			ACCESS_TOKEN ("access_token", ""),
//			ACCESS_TOKEN_SECRET ("access_token_secret", ""),
//			REQUEST_TOKEN ("request_token", ""),
//			REQUEST_TOKEN_SECRET ("request_token_secret", ""),
//			OAUTH_10A ("oauth_10a", false),
//			AUTHORIZE_URL ("authorize_url", ""),
//			ACCESS_TOKEN_URL ("access_token_url", ""),
//			REQUEST_TOKEN_URL ("request_token_url", ""),
//			LATEST_SYNC_REVISION ("latest_sync_revision", 0L),
//			LATEST_SYNC_DATE ("latest_sync_date", (new Time()).Format3339(false)), // will be used to tell whether we have newer notes
//			SORT_ORDER ("sort_order", "sort_date"),
//			FIRST_RUN ("first_run", true),
//			BASE_TEXT_SIZE("base_text_size","18")
//		}

//			private string name = "";
//			private Object defaultValue = "";
//			
//			Key(string name, Object defaultValue) {
//				this.name = name;
//				this.defaultValue = defaultValue;
//			}
//			
//			public string getName() {
//				return name;
//			}
//			
//			public Object getDefault() {
//				return defaultValue;
//			}
//		}
//		
//		private static SharedPreferences client = null;
//		private static SharedPreferences.Editor editor = null;
//		
//		public static void init(Context context, bool clean) {
//			
//			client = PreferenceManager.getDefaultSharedPreferences(context);
//			editor = client.edit();
//			
//			if (clean)
//				editor.clear().commit();
//		}
//		
//		public static string GetString(Key key) {
//			return client.GetString(key.getName(), (string) key.getDefault());
//		}
//		
//		public static void putstring(Key key, string value) {
//			
//			if (value == null)
//				editor.putstring(key.getName(), (string)key.getDefault());
//			else
//				editor.putstring(key.getName(), value);
//			editor.commit();
//		}
//		
//		public static long getLong(Key key) {
//			
//			return client.getLong(key.getName(), (Long)key.getDefault());
//		}
//		
//		public static void putLong(Key key, long value) {
//			
//			editor.putLong(key.getName(), value);
//			editor.commit();
//		}
//		
//		public static bool GetBoolean(Key key) {
//			
//			return client.GetBoolean(key.getName(), (bool)key.getDefault());
//		}
//		
//		public static void putBoolean(Key key, bool value) {
//			
//			editor.putBoolean(key.getName(), value);
//			editor.commit();
//		}
	}
}