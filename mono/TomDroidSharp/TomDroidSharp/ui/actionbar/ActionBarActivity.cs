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

using Android.App;
using Android.OS;
using Android.Views;
using Android.Runtime;

namespace TomDroidSharp.ui.actionbar
{

	/**
	 * A base activity that defers common functionality across app activities to an {@link
	 * ActionBarHelper}.
	 *
	 * NOTE: dynamically marking menu items as invisible/visible is not currently supported.
	 *
	 * NOTE: this may used with the Android Compatibility Package by extending
	 * android.support.v4.app.FragmentActivity instead of {@link Activity}.
	 */
	[Activity (Label = "TomDroidSharp")]
	public abstract class ActionBarActivity : Activity
	{
	    readonly ActionBarHelper mActionBarHelper = ActionBarHelper.createInstance(this);

	    /**
	     * Returns the {@link ActionBarHelper} for this activity.
	     */
	    protected ActionBarHelper getActionBarHelper() {
	        return mActionBarHelper;
	    }

	    /**{@inheritDoc}*/
		public override MenuInflater MenuInflater {
			get {
				return mActionBarHelper.getMenuInflater(base.MenuInflater);
			}
		}

	    /**{@inheritDoc}*/
	    protected override void OnCreate(Bundle savedInstanceState) {
	        base.OnCreate(savedInstanceState);
	        mActionBarHelper.onCreate(savedInstanceState);
	    }

	    /**{@inheritDoc}*/
	    protected override void OnPostCreate(Bundle savedInstanceState) {
	        base.OnPostCreate(savedInstanceState);
	        mActionBarHelper.onPostCreate(savedInstanceState);
	    }

	    /**
	     * Base action bar-aware implementation for
	     * {@link Activity#onCreateOptionsMenu(android.view.Menu)}.
	     *
	     * Note: marking menu items as invisible/visible is not currently supported.
	     */
	    public override bool OnCreateOptionsMenu (IMenu menu)
		{
	        bool retValue = false;
	        retValue |= mActionBarHelper.onCreateOptionsMenu(menu);
	        retValue |= base.OnCreateOptionsMenu(menu);
	        return retValue;
	    }

	    /**{@inheritDoc}*/
		protected override void OnTitleChanged (Java.Lang.ICharSequence title, Android.Graphics.Color color)
		{
	        //mActionBarHelper.onTitleChanged(title, color);
	        base.OnTitleChanged(title, color);
	    }
	}
}