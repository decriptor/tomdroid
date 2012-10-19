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
using Android.Views;

//import java.util.List;
using Android.Runtime;
using Android.Content.Res;
using System.Collections.Generic;
using System;

namespace TomDroidSharp.ui.actionbar
{
	/**
	 * A <em>really</em> dumb implementation of the {@link android.view.Menu} interface, that's only
	 * useful for our actionbar-compat purposes. See
	 * <code>com.android.internal.view.menu.MenuBuilder</code> in AOSP for a more complete
	 * implementation.
	 */
	public class SimpleMenu : Menu {

	    private Context mContext;
	    private Resources mResources;

	    private List<SimpleMenuItem> mItems;

	    public SimpleMenu(Context context) {
	        mContext = context;
			mResources = context.Resources;
	        mItems = new List<SimpleMenuItem>();
	    }

	    public Context getContext() {
	        return mContext;
	    }

	    public Resources getResources() {
	        return mResources;
	    }

	    public IMenuItem add(CharSequence title) {
	        return addInternal(0, 0, title);
	    }

	    public IMenuItem add(int titleRes) {
	        return addInternal(0, 0, mResources.GetString(titleRes));
	    }

	    public IMenuItem add(int groupId, int itemId, int order, CharSequence title) {
	        return addInternal(itemId, order, title);
	    }

	    public IMenuItem add(int groupId, int itemId, int order, int titleRes) {
	        return addInternal(itemId, order, mResources.GetString(titleRes));
	    }

	    /**
	     * Adds an item to the menu.  The other add methods funnel to this.
	     */
	    private IMenuItem addInternal(int itemId, int order, CharSequence title) {
	        SimpleMenuItem item = new SimpleMenuItem(this, itemId, order, title);
	        mItems.Add(findInsertIndex(mItems, order), item);
	        return item;
	    }

	    private static int findInsertIndex(Dictionary<string, IMenuItem> items, int order) {
	        for (int i = items.Count - 1; i >= 0; i--) {
	            IMenuItem item = items[i];
	            if (item.getOrder() <= order) {
	                return i + 1;
	            }
	        }

	        return 0;
	    }

	    public int findItemIndex(int id) {
	        int size = Count;

	        for (int i = 0; i < size; i++) {
	            SimpleMenuItem item = mItems[i];
	            if (item.getItemId() == id) {
	                return i;
	            }
	        }

	        return -1;
	    }

	    public void removeItem(int itemId) {
	        removeItemAtInt(findItemIndex(itemId));
	    }

	    private void removeItemAtInt(int index) {
	        if ((index < 0) || (index >= mItems.Count)) {
	            return;
	        }
	        mItems.Remove(index);
	    }

	    public void clear() {
	        mItems.Clear();
	    }

	    public IMenuItem findItem(int id) {
	        int size = Count;
	        for (int i = 0; i < size; i++) {
	            SimpleMenuItem item = mItems[i];
	            if (item.getItemId() == id) {
	                return item;
	            }
	        }

	        return null;
	    }

	    public int Count() {
			return mItems.Count;
	    }

	    public IMenuItem getItem(int index) {
	        return mItems[index];
	    }

	    // Unsupported operations.

	    public ISubMenu addSubMenu(CharSequence charSequence) {
	        throw new NotImplementedException ("This operation is not supported for SimpleMenu");
	    }

	    public ISubMenu addSubMenu(int titleRes) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public ISubMenu addSubMenu(int groupId, int itemId, int order, CharSequence title) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public ISubMenu addSubMenu(int groupId, int itemId, int order, int titleRes) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public int addIntentOptions(int i, int i1, int i2, ComponentName componentName,
	            Intent[] intents, Intent intent, int i3, IMenuItem[] menuItems) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public void removeGroup(int i) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public void setGroupCheckable(int i, bool b, bool b1) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public void setGroupVisible(int i, bool b) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public void setGroupEnabled(int i, bool b) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public bool hasVisibleItems() {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public void close() {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public bool performShortcut(int i, KeyEvent keyEvent, int i1) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public bool isShortcutKey(int i, KeyEvent keyEvent) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public bool performIdentifierAction(int i, int i1) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }

	    public void setQwertyMode(bool b) {
			throw new NotImplementedException("This operation is not supported for SimpleMenu");
	    }
	}
}