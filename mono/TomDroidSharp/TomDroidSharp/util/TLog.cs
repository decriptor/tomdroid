/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 *
 * Copyright 2011 Piotr Adamski <mcveat@gmail.com>
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


using Android.Util;
//import static java.text.MessageFormat.format;


namespace TomDroidSharp.util
{


	/**
	 * @author Piotr Adamski <mcveat@gmail.com>
	 */
	public class TLog
	{
	    // Logging should be disabled for release builds
	    private static readonly boolean LOGGING_ENABLED = true;

	    public static void v(string tag, Throwable t, string msg, Object... args) {
	        if (LOGGING_ENABLED) Log.v(tag, format(msg, args), t);
	    }

	    public static void v(string tag, string msg, Object... args) {
	    	if (LOGGING_ENABLED) Log.v(tag, format(msg, args));
	    }

	    public static void d(string tag, Throwable t, string msg, Object... args) {
	        if (LOGGING_ENABLED) Log.d(tag, format(msg, args), t);
	    }

	    public static void d(string tag, string msg, Object... args) {
	    	if (LOGGING_ENABLED) Log.d(tag, format(msg, args));
	    }

	    public static void i(string tag, Throwable t, string msg, Object... args) {
	        Log.i(tag, format(msg, args), t);
	    }

	    public static void i(string tag, string msg, Object... args) {
	        Log.i(tag, format(msg, args));
	    }

	    public static void w(string tag, Throwable t, string msg, Object... args) {
	        Log.w(tag, format(msg, args), t);
	    }

	    public static void w(string tag, string msg, Object... args) {
	        Log.w(tag, format(msg, args));
	    }

	    public static void e(string tag, Throwable t, string msg, Object... args) {
	        Log.e(tag, format(msg, args), t);
	    }

	    public static void e(string tag, string msg, Object... args) {
	        Log.e(tag, format(msg, args));
	    }
	/**
	 * FIXME disabled since they were introduced in API level 8 and we target lower
	    public static void wtf(string tag, Throwable t, string msg, Object... args) {
	        Log.wtf(tag, format(msg, args), t);
	    }

	    public static void wtf(string tag, string msg, Object... args) {
	        Log.wtf(tag, format(msg, args));
	    }
	 */
	}
}