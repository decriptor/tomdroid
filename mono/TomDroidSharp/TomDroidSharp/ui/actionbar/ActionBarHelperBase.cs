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

//import org.xmlpull.v1.XmlPullParser;
//import org.xmlpull.v1.XmlPullParserException;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;

//import java.io.IOException;
//import java.util.HashSet;
//import java.util.Set;
using System.IO;
using Org.XmlPull.V1;
using System.Collections.Generic;
using Java.Lang;

namespace TomDroidSharp.ui.actionbar
{

	/**
	 * A class that : the action bar pattern for pre-Honeycomb devices.
	 */
	public class ActionBarHelperBase : ActionBarHelper {
	    private static readonly string MENU_RES_NAMESPACE = "http://schemas.android.com/apk/res/android";
	    private static readonly string MENU_ATTR_ID = "id";
	    private static readonly string MENU_ATTR_SHOW_AS_ACTION = "showAsAction";

	    protected List<int> mActionItemIds = new List<int>();

	    protected ActionBarHelperBase(Activity activity) : base(activity) {
	    }
	    /**{@inheritDoc}*/
	    public override void onCreate(Bundle savedInstanceState) {
	        if (!(mActivity as PreferenceActivity)) {
	            mActivity.RequestWindowFeature(Window.FEATURE_CUSTOM_TITLE);
	        }
	    }

	    /**{@inheritDoc}*/
	    public override void onPostCreate(Bundle savedInstanceState) {
	        mActivity.Window.SetFeatureInt(Window.FEATURE_CUSTOM_TITLE,
	                Resource.Layout.actionbar_compat);
	        setupActionBar();

	        SimpleMenu menu = new SimpleMenu(mActivity);
	        mActivity.OnCreatePanelMenu(Window.FEATURE_OPTIONS_PANEL, menu);
	        mActivity.OnPrepareOptionsMenu(menu);
	        for (int i = 0; i < menu.Count; i++) {
				IMenuItem item = menu.getItem(i);
	            if (mActionItemIds.Contains(item.ItemId)) {
	                addActionItemCompatFromMenuItem(item);
	            }
	        }
	    }

	    /**
	     * Sets up the compatibility action bar with the given title.
	     */
	    private void setupActionBar() {
	        ViewGroup actionBarCompat = getActionBarCompat();
	        if (actionBarCompat == null) {
	            return;
	        }

	        LinearLayout.LayoutParams springLayoutParams = new LinearLayout.LayoutParams(
	                0, ViewGroup.LayoutParams.FillParent);
	        springLayoutParams.Weight = 1;

	        // Add Home button
	        SimpleMenu tempMenu = new SimpleMenu(mActivity);
	        SimpleMenuItem homeItem = new SimpleMenuItem(
				tempMenu, Resource.Id.home, 0, mActivity.GetString(Resource.String.app_name));
	        homeItem.setIcon(Resource.Drawable.Icon);
	        addActionItemCompatFromMenuItem(homeItem);

	        // Add title text
	        TextView titleText = new TextView(mActivity, null, Resource.attr.actionbarCompatTitleStyle);
	        titleText.LayoutParameters =  springLayoutParams;
	        titleText.SetText(mActivity.Title);
	        actionBarCompat.AddView(titleText);
	    }

	    /**{@inheritDoc}*/
	    public override void setRefreshActionItemState(bool refreshing) {
	        View refreshButton = mActivity.FindViewById(Resource.Id.actionbar_compat_item_refresh);
	        View refreshIndicator = mActivity.FindViewById(
	                Resource.Id.actionbar_compat_item_refresh_progress);

	        if (refreshButton != null) {
	            refreshButton.setVisibility(refreshing ? View.GONE : View.VISIBLE);
	        }
	        if (refreshIndicator != null) {
	            refreshIndicator.setVisibility(refreshing ? View.VISIBLE : View.GONE);
	        }
	    }

	    /**
	     * Action bar helper code to be run in {@link Activity#onCreateOptionsMenu(android.view.Menu)}.
	     *
	     * NOTE: This code will mark on-screen menu items as invisible.
	     */
	    public override bool OnCreateOptionsMenu(IMenu menu) {
	        // Hides on-screen action items from the options menu.
	        foreach (int id in mActionItemIds) {
	            menu.FindItem(id).setVisible(false);
	        }
	        return true;
	    }

	    /**{@inheritDoc}*/
	    protected override void onTitleChanged(ICharSequence title, int color) {
	        TextView titleView = (TextView) mActivity.FindViewById(Resource.Id.actionbar_compat_title);
	        if (titleView != null) {
	            titleView.SetText(title);
	        }
	    }

	    /**
	     * Returns a {@link android.view.MenuInflater} that can read action bar metadata on
	     * pre-Honeycomb devices.
	     */
	    public MenuInflater getMenuInflater(MenuInflater superMenuInflater) {
	        return new WrappedMenuInflater(mActivity, superMenuInflater);
	    }

	    /**
	     * Returns the {@link android.view.ViewGroup} for the action bar on phones (compatibility action
	     * bar). Can return null, and will return null on Honeycomb.
	     */
	    private ViewGroup getActionBarCompat() {
	        return (ViewGroup) mActivity.FindViewById(Resource.Id.actionbar_compat);
	    }

	    /**
	     * Adds an action button to the compatibility action bar, using menu information from a {@link
	     * android.view.IMenuItem}. If the menu item ID is <code>menu_refresh</code>, the menu item's
	     * state can be changed to show a loading spinner using
	     * {@link com.example.android.actionbarcompat.ActionBarHelperBase#setRefreshActionItemState(bool)}.
	     */
	    private View addActionItemCompatFromMenuItem(IMenuItem item) {
			int itemId = item.ItemId;

	        ViewGroup actionBar = getActionBarCompat();
	        if (actionBar == null) {
	            return null;
	        }

	        // Create the button
	        ImageButton actionButton = new ImageButton(mActivity, null,
	                itemId == Resource.Id.home
	                        ? Resource.attr.actionbarCompatItemHomeStyle
	                        : Resource.attr.actionbarCompatItemStyle);
	        actionButton.LayoutParameters = (new ViewGroup.LayoutParams(
	                (int) mActivity.getResources().getDimension(
	                        itemId == android.Resource.Id.home
	                                ? Resource.dimen.actionbar_compat_button_home_width
	                                : Resource.dimen.actionbar_compat_button_width),
	                ViewGroup.LayoutParams.FILL_PARENT));
	        if (itemId == Resource.Id.menuSync) {
	            actionButton.Id = Resource.Id.actionbar_compat_item_refresh;
	        }
	        actionButton.SetImageDrawable(item.Icon);
	        actionButton.SetScaleType(ImageView.ScaleType.Center);
			actionButton.ContentDescription = item.TitleFormatted;
//	        actionButton.setOnClickListener(new View.OnClickListener() {
//	            public void onClick(View view) {
//	                mActivity.onMenuItemSelected(Window.FEATURE_OPTIONS_PANEL, item);
//	            }
//	        });

	        actionBar.AddView(actionButton);

	        if (item.ItemId == Resource.Id.menuSync) {
	            // Refresh buttons should be stateful, and allow for indeterminate progress indicators,
	            // so add those.
	            ProgressBar indicator = new ProgressBar(mActivity, null,
	                    Resource.attr.actionbarCompatProgressIndicatorStyle);

	            int buttonWidth = mActivity.Resources.GetDimensionPixelSize(
	                    Resource.dimen.actionbar_compat_button_width);
	            int buttonHeight = mActivity.Resources.GetDimensionPixelSize(
	                    Resource.dimen.actionbar_compat_height);
	            int progressIndicatorWidth = buttonWidth / 2;

	            LinearLayout.LayoutParams indicatorLayoutParams = new LinearLayout.LayoutParams(
	                    progressIndicatorWidth, progressIndicatorWidth);
	            indicatorLayoutParams.SetMargins(
	                    (buttonWidth - progressIndicatorWidth) / 2,
	                    (buttonHeight - progressIndicatorWidth) / 2,
	                    (buttonWidth - progressIndicatorWidth) / 2,
	                    0);
	            indicator.LayoutParameters = indicatorLayoutParams;
				indicator.Visibility = ViewStates.Gone;
	            indicator.Id = Resource.Id.actionbar_compat_item_refresh_progress;
	            actionBar.AddView(indicator);
	        }

	        return actionButton;
	    }

	    /**
	     * A {@link android.view.MenuInflater} that reads action bar metadata.
	     */
	    private class WrappedMenuInflater : MenuInflater {
	        MenuInflater mInflater;

	        public WrappedMenuInflater(Context context, MenuInflater inflater) : base(context) {
	            mInflater = inflater;
	        }

	        public override void inflate(int menuRes, Menu menu) {
	            loadActionBarMetadata(menuRes);
	            mInflater.Inflate(menuRes, menu);
	        }

	        /**
	         * Loads action bar metadata from a menu resource, storing a list of menu item IDs that
	         * should be shown on-screen (i.e. those with showAsAction set to always or ifRoom).
	         * @param menuResId
	         */
	        private void loadActionBarMetadata(int menuResId) {
	            XmlResourceParser parser = null;
	            try {
	                parser = mActivity.Resources.GetXml(menuResId);

	                int eventType = parser.getEventType();
	                int itemId;
	                int showAsAction;

	                bool eof = false;
	                while (!eof) {
	                    switch (eventType) {
	                        case XmlPullParser.START_TAG:
	                            if (!parser.getName().equals("item")) {
	                                break;
	                            }

	                            itemId = parser.getAttributeResourceValue(MENU_RES_NAMESPACE,
	                                    MENU_ATTR_ID, 0);
	                            if (itemId == 0) {
	                                break;
	                            }

	                            showAsAction = parser.getAttributeIntValue(MENU_RES_NAMESPACE,
	                                    MENU_ATTR_SHOW_AS_ACTION, -1);
	                            if (showAsAction == IMenuItem.SHOW_AS_ACTION_ALWAYS) {
	                                mActionItemIds.add(itemId);
	                            }
	                            break;

	                        case XmlPullParser.END_DOCUMENT:
	                            eof = true;
	                            break;
	                    }

	                    eventType = parser.next();
	                }
	            } catch (XmlPullParserException e) {
	                throw new InflateException("Error inflating menu XML", e);
	            } catch (IOException e) {
	                throw new InflateException("Error inflating menu XML", e);
	            } finally {
	                if (parser != null) {
	                    parser.Close();
	                }
	            }
	        }

	    }
	}
}