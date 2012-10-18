/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
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

//import java.util.List;

using TomDroidSharp.sync.sd.SdCardSyncService;
using TomDroidSharp.sync.web.SnowySyncService;
using TomDroidSharp.util.Preferences;
using Android.App;
using Android.OS;

namespace TomDroidSharp.sync
{

public class SyncManager {
		
		private static List<SyncService> services = new List<SyncService>();
		private SyncService service;
		
		public SyncManager() {
			createServices();
		}

		public List<SyncService> getServices() {
			return services;
		}
		
		public static SyncService getService(string name) {
			
			for (int i = 0; i < services.size(); i++) {
				SyncService service = services.get(i);			
				if (name.equals(service.getName()))
					return service;
			}
			
			return null;
		}
		
		public void startSynchronization(bool push) {
			
			service = getCurrentService();
			service.setCancelled(false);
			service.startSynchronization(push);
		}
		
		public SyncService getCurrentService() {
			string serviceName = Preferences.getstring(Preferences.Key.SYNC_SERVICE);
			return getService(serviceName);
		}
		
		private static SyncManager instance = null;
		private static Activity activity;
		private static Handler handler;
		
		public static SyncManager getInstance() {
			
			if (instance == null)
				instance = new SyncManager();
			
			return instance;
		}
		
		public static void setActivity(Activity a) {
			activity = a;
			getInstance().createServices();
		}
		
		public static void setHandler(Handler h) {
			handler = h;
			getInstance().createServices();
		}

		private void createServices() {
			services.clear();
			
			services.add(new SnowySyncService(activity, handler));
			services.add(new SdCardSyncService(activity, handler));
		}

		// new methods to TEdit
		
		public void pullNote(string guid) {
			SyncService service = getCurrentService();
			service.pullNote(guid);		
		}

		public void cancel() {
			service.setCancelled(true);
		}
	}
}