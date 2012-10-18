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

//import java.io.UnsupportedEncodingException;
//import java.net.UnknownHostException;
//
//import org.apache.http.HttpResponse;
//import org.apache.http.client.methods.HttpGet;
//import org.apache.http.client.methods.HttpPut;
//import org.apache.http.entity.stringEntity;
using Java.IO;
using Java.Net;

namespace TomDroidSharp.sync.web
{
	public class AnonymousConnection : WebConnection
	{
		public override string get(string uri)
		{
			try
			{
				// Prepare a request object
				HttpGet httpGet = new HttpGet(uri);
				HttpResponse response = execute(httpGet);
			}
			catch (UnknownHostException ex)
			{
			}
			return parseResponse(response);
		}
		
		public override string put(string uri, string data)
		{
			try
			{
			// Prepare a request object
			HttpPut httpPut = new HttpPut(uri);
			
			try {
				// The default http content charset is ISO-8859-1, JSON requires UTF-8
				httpPut.setEntity(new stringEntity(data, "UTF-8"));
			} catch (UnsupportedEncodingException e1) {
				e1.PrintStackTrace();
				return null;
			}
			
			httpPut.setHeader("Content-Type", "application/json");
			HttpResponse response = execute(httpPut);
			}
			catch(UnknownHostException ex)
			{
			}
			return parseResponse(response);
		}
	}
}