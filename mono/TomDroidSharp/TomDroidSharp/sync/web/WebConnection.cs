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

using Java.IO;
using TomDroidSharp.util;
using Java.Net;
using Java.Lang;

using System.Net;

namespace TomDroidSharp.Sync.web
{
	public abstract class WebConnection {
		
		private static readonly string TAG = "WebConnection";
		
		public abstract string get(string uri);// throws UnknownHostException;
		public abstract string put(string uri, string data);// throws UnknownHostException;
		
		private static string convertStreamTostring(InputStream inputStream) {
			/*
			 * To convert the InputStream to string we use the BufferedReader.readLine()
			 * method. We iterate until the BufferedReader return null which means
			 * there's no more data to read. Each line will appended to a stringBuilder
			 * and returned as string.
			 */
			BufferedReader reader = new BufferedReader(new InputStreamReader(inputStream));
			StringBuilder sb = new StringBuilder();

			string line = null;
			try {
				while ((line = reader.ReadLine()) != null) {
					sb.Append(line + "\n");
				}
			} catch (IOException e) {
				e.PrintStackTrace();
			} finally {
				try {
					inputStream.Close();
				} catch (IOException e) {
					e.PrintStackTrace();
				}
			}

			return sb.ToString();
		}

		protected string parseResponse(HttpWebResponse response) {
			
			if (response == null)
				return "";
			
			string result = null;
			
			// Examine the response status
			TLog.i(TAG, "Response status : {0}", response.StatusDescription);

			// Get hold of the response entity
			HttpEntity entity = response.getEntity();
			// If the response does not enclose an entity, there is no need
			// to worry about connection release

			if (entity != null) {
				
				try {
					InputStream instream;

					instream = entity.getContent();
					
					result = convertStreamTostring(instream);
					
					TLog.i(TAG, "Received : {0}", result);
					
					// Closing the input stream will trigger connection release
					instream.Close();
					
				} catch (IllegalStateException e) {
					// TODO Auto-generated catch block
					e.PrintStackTrace();
				} catch (IOException e) {
					// TODO Auto-generated catch block
					e.PrintStackTrace();
				}
			}
			
			return result;
		}
		
		protected HttpResponse execute(HttpUriRequest request)
		{
			DefaultHttpClient httpclient = new DefaultHttpClient();
			
			try {
				// Execute the request
				HttpResponse response = httpclient.execute(request);
				return response;
				
			}catch (UnknownHostException e){
				throw e;
			} catch (ClientProtocolException e) {
				e.PrintStackTrace();
			} catch (IOException e) {
				e.PrintStackTrace();
			} catch (IllegalArgumentException e) {
				e.PrintStackTrace();
			} catch (IllegalStateException e) {
				e.PrintStackTrace();
			}
			
			return null;
		}
	}
}