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


using Android.Content;
using Android.Graphics;
using Android.Views;

//import android.annotation.TargetApi;
using Android.Graphics.Drawables;
using Android.Runtime;

namespace TomDroidSharp.ui.actionbar
{

	/**
	 * A <em>really</em> dumb implementation of the {@link android.view.IMenuItem} interface, that's only
	 * useful for our actionbar-compat purposes. See
	 * <code>com.android.internal.view.menu.MenuItemImpl</code> in AOSP for a more complete
	 * implementation.
	 */
	public class SimpleMenuItem : IMenuItem {

	    private SimpleMenu mMenu;

	    private readonly int mId;
	    private readonly int mOrder;
	    private CharSequence mTitle;
	    private CharSequence mTitleCondensed;
	    private Drawable mIconDrawable;
	    private int mIconResId = 0;
	    private bool mEnabled = true;

	    public SimpleMenuItem(SimpleMenu menu, int id, int order, CharSequence title) {
	        mMenu = menu;
	        mId = id;
	        mOrder = order;
	        mTitle = title;
	    }

	    public int getItemId() {
	        return mId;
	    }

	    public int getOrder() {
	        return mOrder;
	    }

	    public IMenuItem setTitle(CharSequence title) {
	        mTitle = title;
	        return this;
	    }

	    public IMenuItem setTitle(int titleRes) {
	        return setTitle(mMenu.getContext().GetString(titleRes));
	    }

	    public CharSequence getTitle() {
	        return mTitle;
	    }

	    public IMenuItem setTitleCondensed(CharSequence title) {
	        mTitleCondensed = title;
	        return this;
	    }

	    public CharSequence getTitleCondensed() {
	        return mTitleCondensed != null ? mTitleCondensed : mTitle;
	    }

	    public IMenuItem setIcon(Drawable icon) {
	        mIconResId = 0;
	        mIconDrawable = icon;
	        return this;
	    }

	    public IMenuItem setIcon(int iconResId) {
	        mIconDrawable = null;
	        mIconResId = iconResId;
	        return this;
	    }

	    public Drawable getIcon() {
	        if (mIconDrawable != null) {
	            return mIconDrawable;
	        }

	        if (mIconResId != 0) {
	            return mMenu.getResources().GetDrawable(mIconResId);
	        }

	        return null;
	    }

	    public IMenuItem setEnabled(bool enabled) {
	        mEnabled = enabled;
	        return this;
	    }

	    public bool isEnabled() {
	        return mEnabled;
	    }

	    // No-op operations. We use no-ops to allow inflation from menu XML.

	    public int getGroupId() {
	        // Noop
	        return 0;
	    }

	    public View getActionView() {
	        // Noop
	        return null;
	    }

	    //@TargetApi(14)
		public IMenuItem setActionProvider(ActionProvider actionProvider) {
	        // Noop
	        return this;
	    }

	    //@TargetApi(14)
		public ActionProvider getActionProvider() {
	        // Noop
	        return null;
	    }

	    public bool expandActionView() {
	        // Noop
	        return false;
	    }

	    public bool collapseActionView() {
	        // Noop
	        return false;
	    }

	    public bool isActionViewExpanded() {
	        // Noop
	        return false;
	    }

	    //@TargetApi(14)
		public IMenuItem setOnActionExpandListener(OnActionExpandListener onActionExpandListener) {
	        // Noop
	        return this;
	    }

	    public IMenuItem setIntent(Intent intent) {
	        // Noop
	        return this;
	    }

	    public Intent intent (){
	        // Noop
	        return null;
	    }

	    public IMenuItem setShortcut(char c, char c1) {
	        // Noop
	        return this;
	    }

	    public IMenuItem setNumericShortcut(char c) {
	        // Noop
	        return this;
	    }

	    public char getNumericShortcut() {
	        // Noop
	        return 0;
	    }

	    public IMenuItem setAlphabeticShortcut(char c) {
	        // Noop
	        return this;
	    }

	    public char getAlphabeticShortcut() {
	        // Noop
	        return 0;
	    }

	    public IMenuItem setCheckable(bool b) {
	        // Noop
	        return this;
	    }

	    public bool isCheckable() {
	        // Noop
	        return false;
	    }

	    public IMenuItem setChecked(bool b) {
	        // Noop
	        return this;
	    }

	    public bool isChecked() {
	        // Noop
	        return false;
	    }

	    public IMenuItem setVisible(bool b) {
	        // Noop
	        return this;
	    }

	    public bool isVisible() {
	        // Noop
	        return true;
	    }

	    public bool hasSubMenu() {
	        // Noop
	        return false;
	    }

	    public ISubMenu getSubMenu() {
	        // Noop
	        return null;
	    }

	    public IMenuItem setOnMenuItemClickListener(OnMenuItemClickListener onMenuItemClickListener) {
	        // Noop
	        return this;
	    }

	    public ContextMenu.ContextMenuInfo getMenuInfo() {
	        // Noop
	        return null;
	    }

	    public void setShowAsAction(int i) {
	        // Noop
	    }

	    public IMenuItem setShowAsActionFlags(int i) {
	        // Noop
	        return null;
	    }

	    public IMenuItem setActionView(View view) {
	        // Noop
	        return this;
	    }

	    public IMenuItem setActionView(int i) {
	        // Noop
	        return this;
	    }
	}
}