/*
 * Copyright 2011 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using TomDroidSharp.R;

using Android.App;
using Android.Content;
using Android.Views;

namespace TomDroidSharp.ui.actionbar
{

	/**
	 * An extension of {@link ActionBarHelper} that provides Android 3.0-specific functionality for
	 * Honeycomb tablets. It thus requires API level 11.
	 */
	public class ActionBarHelperHoneycomb : ActionBarHelper {
	    private Menu mOptionsMenu;
	    private View mRefreshIndeterminateProgressView = null;

	    protected ActionBarHelperHoneycomb(Activity activity) : base (activity) {
	    }

	    public override bool onCreateOptionsMenu(Menu menu) {
	        mOptionsMenu = menu;
	        return base.onCreateOptionsMenu(menu);
	    }

	    public override void setRefreshActionItemState(bool refreshing) {
	        // On Honeycomb, we can set the state of the refresh button by giving it a custom
	        // action view.
	        if (mOptionsMenu == null) {
	            return;
	        }

	        IMenuItem refreshItem = mOptionsMenu.findItem(Resource.Id.menu_refresh);
	        if (refreshItem != null) {
	            if (refreshing) {
	                if (mRefreshIndeterminateProgressView == null) {
	                    LayoutInflater inflater = (LayoutInflater)
	                            getActionBarThemedContext().GetSystemService(
	                                    Context.LAYOUT_INFLATER_SERVICE);
	                    mRefreshIndeterminateProgressView = inflater.inflate(
	                            Resource.Layout.actionbar_indeterminate_progress, null);
	                }

	                refreshItem.SetActionView(mRefreshIndeterminateProgressView);
	            } else {
	                refreshItem.SetActionView(null);
	            }
	        }
	    }

	    /**
	     * Returns a {@link Context} suitable for inflating layouts for the action bar. The
	     * implementation for this method in {@link ActionBarHelperICS} asks the action bar for a
	     * themed context.
	     */
	    protected Context getActionBarThemedContext() {
	        return mActivity;
	    }
	}
}