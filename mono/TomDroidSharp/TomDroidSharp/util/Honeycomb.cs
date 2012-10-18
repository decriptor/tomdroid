
using Android.App;
//import android.annotation.TargetApi;

namespace TomDroidSharp.Util
{
	public class Honeycomb {
		@TargetApi(11)
		public static void invalidateOptionsMenuWrapper(Activity activity) {
			activity.invalidateOptionsMenu();
		}
	}
}