/*
 * Copyright 2011 Anders Kalør
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

// modified by noahy <noahy57@gmail.com>
// modifications released under GPL 3.0+

using System.Collections.Generic;

using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using TomDroidSharp.ui.actionbar;
using Android.App;
using Java.IO;

namespace TomDroidSharp.ui
{
	[Activity (Label = "FilePickerActivity")]
	public class FilePickerActivity : ActionBarListActivity
	{	
		/**
		 * The file path
		 */
		public readonly static string EXTRA_FILE_PATH = "file_path";
		
		/**
		 * Sets whether hidden files should be visible in the list or not
		 */
		public readonly static string EXTRA_SHOW_HIDDEN_FILES = "show_hidden_files";

		/**
		 * The allowed file extensions in an List of strings
		 */
		public readonly static string EXTRA_ACCEPTED_FILE_EXTENSIONS = "accepted_file_extensions";
		
		protected File mDirectory;
		protected List<File> mFiles;
		protected FilePickerListAdapter mAdapter;
		protected bool mShowHiddenFiles = false;
		protected string[] acceptedFileExtensions;
		
		private LinearLayout navButtons = null;

		protected override void onCreate(Bundle savedInstanceState)
		{
			base.onCreate(savedInstanceState);

			View contentView =  View.Inflate(this, Resource.Layout.file_picker_content_view, null);
			SetContentView(contentView);
	        
	        navButtons = contentView.FindViewById<LinearLayout>(Resource.Id.navButtons);
	        
			// Set the view to be shown if the list is empty
			LayoutInflater inflator = (LayoutInflater) GetSystemService(Context.LayoutInflaterService);
			View emptyView = inflator.Inflate(Resource.Layout.file_picker_empty_view, null);
			((ViewGroup)ListView.Parent).AddView(emptyView);
			ListView.EmptyView = emptyView;

			TextView listHeader = new TextView(this);
			listHeader.SetText(Resource.String.chooseFile);
			ListView.AddHeaderView(listHeader);
			
			// Set initial directory
			mDirectory = new File(Preferences.GetString(Preferences.Key.LAST_FILE_PATH));
			refreshNavButtons();
			
			// Initialize the List
			mFiles = new List<File>();
			
			// Set the ListAdapter
			mAdapter = new FilePickerListAdapter(this, mFiles);
			ListAdapter = mAdapter;
			
			// Initialize the extensions array to allow any file extensions
			acceptedFileExtensions = new string[] {};
			
			// Get intent extras
			if(Intent.HasExtra(EXTRA_FILE_PATH)) {
				mDirectory = new File(Intent.GetStringExtra(EXTRA_FILE_PATH));
			}
			if(Intent.HasExtra(EXTRA_SHOW_HIDDEN_FILES)) {
				mShowHiddenFiles = Intent.GetBooleanExtra(EXTRA_SHOW_HIDDEN_FILES, false);
			}
			if(Intent.HasExtra(EXTRA_ACCEPTED_FILE_EXTENSIONS)) {
				List<string> collection = Intent.GetStringArrayListExtra(EXTRA_ACCEPTED_FILE_EXTENSIONS);
				acceptedFileExtensions = (string[]) collection.ToArray(new string[collection.Count]);
			}
		}
		
		protected override void onResume() {
			refreshFilesList();
			base.OnResume();
		}
		
		/**
		 * Updates the list view to the current directory
		 */
		protected void refreshFilesList() {
			// Clear the files List
			mFiles.Clear();
			
			// Set the extension file filter
			ExtensionFilenameFilter filter = new ExtensionFilenameFilter(acceptedFileExtensions);
			
			// Get the files in the directory
			File[] files = mDirectory.ListFiles(filter);
			if(files != null && files.Length > 0) {
				foreach(File f in files) {
					if(f.IsHidden() && !mShowHiddenFiles) {
						// Don't add the file
						continue;
					}
					
					// Add the file the ArrayAdapter
					mFiles.Add(f);
				}
				
				Collections.sort(mFiles, new FileComparator());
			}
			mAdapter.NotifyDataSetChanged();
		}

		protected override void onListItemClick(ListView l, View v, int position, long id) {
			File newFile = (File)l.GetItemAtPosition(position);
			
			if(newFile.IsFile()) {
				// Set result
				Intent extra = new Intent();
				extra.PutExtra(EXTRA_FILE_PATH, newFile.AbsolutePath);
				SetResult(Android.App.Result.Ok, extra);
				// Finish the activity
				Finish();
			} else {
				mDirectory = newFile;
				
				// set last directory to this one
				Preferences.putstring(Preferences.Key.LAST_FILE_PATH, newFile.getAbsolutePath());
				
				// refresh nav buttons
				refreshNavButtons();

				// Update the files list
				refreshFilesList();
			}
			
			base.OnListItemClick(l, v, position, id);
		}

		private void refreshNavButtons() {
			
			navButtons.RemoveAllViews();

			string directory = mDirectory.AbsolutePath;
			if(directory.Equals("/"))
				directory = "";

			string[] directories = directory.Split("/");
			int position = 0;
			foreach(string dir in directories) {
				int count = 0;
				Button navButton = new Button(this);
				navButton.SetText(position==0?"/":dir);
				string newDir = "";
				foreach(string dir2 in directories) {
					if(count++ > position)
						break;
					newDir += "/"+dir2;
				}
				string newDir2 = position==0?"/":newDir;
//				navButton.setOnClickListener(new OnClickListener(){
//
//					public override void onClick(View v) {
//						mDirectory = new File(newDir2);
//						refreshNavButtons();
//						refreshFilesList();
//					}
//				});
				position++;
				navButtons.AddView(navButton);
			}
		}

		private class FilePickerListAdapter : ArrayAdapter<Java.IO.File> {
			
			private List<File> mObjects;
			
			public FilePickerListAdapter(Context context, List<File> objects) : base (context, Resource.Layout.file_picker_list_item, Resource.Id.text1, objects) {
				mObjects = objects;
			}
			
			public override View getView(int position, View convertView, ViewGroup parent) {
				
				View row = null;
				
				if(convertView == null) { 
					LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
					row = inflater.Inflate(Resource.Layout.file_picker_list_item, parent, false);
				} else {
					row = convertView;
				}

				File fileObject = mObjects[position];

				ImageView imageView = row.FindViewById<ImageView>(Resource.Id.file_picker_image);
				TextView textView = row.FindViewById<TextView>(Resource.Id.file_picker_text);
				// Set single line
				textView.SetSingleLine(true);
				
				textView.SetText(fileObject.Name);
				if(fileObject.IsFile()) {
					// Show the file icon
					imageView.SetImageResource(Resource.Drawable.file);
				} else {
					// Show the folder icon
					imageView.SetImageResource(Resource.Drawable.folder);
				}
				
				return row;
			}

		}
		
		private class FileComparator : IComparer<File> {
			int IComparer<File>.Compare (File f1, File f2)
			{
		    	if(f1 == f2) {
		    		return 0;
		    	}
		    	if(f1.IsDirectory() && f2.IsFile()) {
		        	// Show directories above files
		        	return -1;
		        }
		    	if(f1.IsFile() && f2.IsDirectory()) {
		        	// Show files below directories
		        	return 1;
		        }
		    	// Sort the directories alphabetically
		        return f1.getName().compareToIgnoreCase(f2.getName());
		    }
		}
		
//		private class ExtensionFilenameFilter : FilenameFilter {
//			private string[] mExtensions;
//			
//			public ExtensionFilenameFilter(string[] extensions) : base(){
//				mExtensions = extensions;
//			}
			
//			public override bool accept(File dir, string filename) {
//				if(new File(dir, filename).IsDirectory()) {
//					// Accept all directory names
//					return true;
//				}
//				if(mExtensions != null && mExtensions.Length > 0) {
//					for(int i = 0; i < mExtensions.Length; i++) {
//						if(filename.EndsWith(mExtensions[i])) {
//							// The filename ends with the extension
//							return true;
//						}
//					}
//					// The filename did not match any of the extensions
//					return false;
//				}
//				// No extensions has been set. Accept all file extensions.
//				return true;
//			}
//		}
	}
}