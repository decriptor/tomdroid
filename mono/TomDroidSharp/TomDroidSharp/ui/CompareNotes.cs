/*
 * Tomdroid
 * Tomboy on Android
 * http://www.launchpad.net/tomdroid
 * 
 * Copyright 2009, 2010, 2011 Olivier Bilodeau <olivier@bottomlesspit.org>
 * Copyright 2009, 2010 Benoit Garret <benoit.garret_launchpad@gadz.org>
 * Copyright 2011 Stefan Hammer <j.4@gmx.at>
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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Tomdroid.	If not, see <http://www.gnu.org/licenses/>.
 */

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using TomDroidSharp.util;
using Android.Text.Format;
using TomDroidSharp.ui.actionbar;

namespace TomDroidSharp.ui
{

	public class CompareNotes : ActionBarActivity {	
		private static readonly string TAG = "SyncDialog";
		
		private Note localNote;
		private bool differentNotes;
		private Note remoteNote;
		private int dateDiff;
		private bool noRemote;

		public override void onCreate(Bundle savedInstanceState) {	
			base.onCreate(savedInstanceState);	
			
			if(!this.Intent.HasExtra("datediff")) {
				TLog.v(TAG, "no date diff");
				Finish();
				return;
			}
			TLog.v(TAG, "starting CompareNotes");
			
			SetContentView(Resource.Layout.note_compare);
			
			Bundle extras = this.Intent.Extras;

			remoteNote = new Note();
			remoteNote.SetTitle(extras.GetString("title"));
			remoteNote.SetGuid(extras.GetString("guid"));
			remoteNote.SetLastChangeDate(extras.GetString("date"));
			remoteNote.SetXmlContent(extras.GetString("content"));	
			remoteNote.SetTags(extras.GetString("tags"));
			
			ContentValues values = new ContentValues();
			values.Put(Note.TITLE, extras.GetString("title"));
			values.Put(Note.FILE, extras.GetString("file"));
			values.Put(Note.GUID, extras.GetString("guid"));
			values.Put(Note.MODIFIED_DATE, extras.GetString("date"));
			values.Put(Note.NOTE_CONTENT, extras.GetString("content"));
			values.Put(Note.TAGS, extras.GetString("tags"));
			 
			dateDiff = extras.GetInt("datediff");
			noRemote = extras.GetBoolean("noRemote");
			
			// check if we're comparing two different notes with same title
			
			differentNotes = Intent.HasExtra("localGUID"); 
			if(differentNotes) {
				localNote = NoteManager.getNoteByGuid(this, extras.GetString("localGUID"));
				TLog.v(TAG, "comparing two different notes with same title");
			}
			else {
				localNote = NoteManager.getNoteByGuid(this, extras.GetString("guid"));
				TLog.v(TAG, "comparing two versions of the same note");
			}
			
			bool deleted = localNote.getTags().Contains("system:deleted"); 
			
			string message;

			Button localBtn = (Button)FindViewById(Resource.Id.localButton);
			Button remoteBtn = (Button)FindViewById(Resource.Id.remoteButton);
			Button copyBtn = (Button)FindViewById(Resource.Id.copyButton);
			
			TextView messageView = (TextView)FindViewById(Resource.Id.message);
			
			ToggleButton diffLabel = (ToggleButton)FindViewById(Resource.Id.diff_label);
			ToggleButton localLabel = (ToggleButton)FindViewById(Resource.Id.local_label);
			ToggleButton remoteLabel = (ToggleButton)FindViewById(Resource.Id.remote_label);

			EditText localTitle = (EditText)FindViewById(Resource.Id.local_title);
			EditText remoteTitle = (EditText)FindViewById(Resource.Id.remote_title);
			
			TextView diffView = (TextView)FindViewById(Resource.Id.diff);
			EditText localEdit = (EditText)FindViewById(Resource.Id.local);
			EditText remoteEdit = (EditText)FindViewById(Resource.Id.remote);


			updateTextAttributes(localTitle, localEdit);
			updateTextAttributes(remoteTitle, remoteEdit);
			
			if(deleted) {
				TLog.v(TAG, "comparing deleted with remote");
				message = GetString(Resource.String.sync_conflict_deleted);
				
				diffLabel.Visibility = ViewStates.Gone;
				localLabel.Visibility = ViewStates.Gone;
				diffView.Visibility = ViewStates.Gone;
				localEdit.Visibility = ViewStates.Gone;
				localTitle.Visibility = ViewStates.Gone;

				copyBtn.Visibility = ViewStates.Gone;
				
				// if importing note, offer cancel import option to open main screen
				if(noRemote) {
					localBtn.SetText(GetString(Resource.String.btnCancelImport));
//					localBtn.SetOnClickListener( new View.OnClickListener() {
//						public void onClick(View v) {
//							finishForResult(new Intent());
//						}
//			        });
				}
				else {
					localBtn.SetText(GetString(Resource.String.delete_remote));
//					localBtn.SetOnClickListener( new View.OnClickListener() {
//						public void onClick(View v) {
//							onChooseDelete();
//						}
//			        });
				}
			}
			else {
				string diff = "";
				bool titleMatch = localNote.getTitle().Equals(extras.GetString("title"));

				if(differentNotes)
					message = GetString(Resource.String.sync_conflict_titles_message);
				else
					message = GetString(Resource.String.sync_conflict_message);
				
				if(!titleMatch) {
					diff = "<b>"+GetString(Resource.String.diff_titles)+"</b><br/><i>"+GetString(Resource.String.local_label)+"</i><br/> "+localNote.getTitle()+"<br/><br/><i>"+GetString(Resource.String.remote_label)+"</i><br/>"+extras.GetString("title");		
				}

				if(localNote.getXmlContent().Equals(extras.GetString("content").Replace("</*link:[^>]+>", ""))) {
					TLog.v(TAG, "compared notes have same content");
					if(titleMatch) { // same note, fix the dates
						if(extras.GetInt("datediff") < 0) { // local older
							TLog.v(TAG, "compared notes have same content and titles, pulling newer remote");
							pullNote(remoteNote);
						}
						else if(extras.GetInt("datediff") == 0 || noRemote) {
							TLog.v(TAG, "compared notes have same content and titles, same date, doing nothing");
						}
						else {
							TLog.v(TAG, "compared notes have same content and titles, pushing newer local");
							pushNote(localNote);
							return;
						}
						
						if(noRemote) {
							TLog.v(TAG, "compared notes have same content and titles, showing note");
							finishForResult(new Intent());
						}
						else // do nothing
							Finish();
						
						return;
					}
					else {
						TLog.v(TAG, "compared notes have different titles");
			            diffView.SetText(diff);
						localEdit.Visibility = ViewStates.Gone;
						remoteEdit.Visibility = ViewStates.Gone;					
					}
				}
				else {
					TLog.v(TAG, "compared notes have different content");
					if(titleMatch && !differentNotes) {
						localTitle.Visibility = ViewStates.Gone;
						remoteTitle.Visibility = ViewStates.Gone;
					}
					else
		    			diff += "<br/><br/>";

					Patch patch = DiffUtils.diff(Arrays.asList(TextUtils.split(localNote.getXmlContent(), "\\r?\\n|\\r")), Arrays.asList(TextUtils.split(extras.GetString("content"), "\\r?\\n|\\r")));
		            string diffResult = "";
					foreach (Delta delta in patch.getDeltas()) {
		            	diffResult += delta.ToString()+"<br/>";
		            }

		            Pattern firstPattern = Pattern.compile("\\[ChangeDelta, position: ([0-9]+), lines: \\[([^]]+)\\] to \\[([^]]+)\\]\\]");
		            Pattern secondPattern = Pattern.compile("\\[InsertDelta, position: ([0-9]+), lines: \\[([^]]+)\\]\\]");
		            Pattern thirdPattern = Pattern.compile("\\[DeleteDelta, position: ([0-9]+), lines: \\[([^]]+)\\]\\]");
		        	
		            Matcher matcher = firstPattern.matcher(diffResult);
		            stringBuffer result = new stringBuffer();
		            while (matcher.find())
		            {
		                matcher.appendReplacement(
		                	result, 
	                		"<b>"+string.format(GetString(Resource.String.line_x),string.valueOf(Integer.parseInt(matcher.group(1)) + 1))+"</b><br/><i>"
	                		+GetString(Resource.String.local_label)+":</i><br/>"+matcher.group(2)+"<br/><br/><i>"
	                		+GetString(Resource.String.remote_label)+":</i><br/>"+matcher.group(3)+"<br/>"
		                );
		            }
		            matcher.appendTail(result);

		            matcher = secondPattern.matcher(result);
		            result = new stringBuffer();
		            while (matcher.find())
		            {
		                matcher.appendReplacement(
		                	result, 
							"<b>"+string.format(GetString(Resource.String.line_x),string.valueOf(Integer.parseInt(matcher.group(1)) + 1))+"</b><br/><i>"
		                	+GetString(Resource.String.remote_label)+":</i><br/>"+matcher.group(2)+"<br/><br/>"

		                );
		            }
		            matcher.appendTail(result);

		            matcher = thirdPattern.matcher(result);
		            result = new stringBuffer();
		            while (matcher.find())
		            {
		                matcher.appendReplacement(
		                	result, 
							"<b>"+string.format(GetString(Resource.String.line_x),string.valueOf(Integer.parseInt(matcher.group(1)) + 1))+"</b><br/><i>"
		                	+GetString(Resource.String.local_label)+":</i><br/>"+matcher.group(2)+"<br/><br/>"

		                );
		            }
		            matcher.appendTail(result);
		            
					diff += "<b>"+GetString(Resource.String.diff_content)+"</b><br/>";		
		            
		            diff += result;
					
		            diff = diff.replace("\n","<br/>");
		            
		            diffView.SetText(Html.fromHtml(diff));
					
				}
				
				if(noRemote) {
					localBtn.SetText(GetString(Resource.String.btnCancelImport));
					message = GetString(Resource.String.sync_conflict_import_message);
				}

//				localBtn.SetOnClickListener( new View.OnClickListener() {
//					public void onClick(View v) {
//
//						// check if there is no remote (e.g. we are receiving a note file that conflicts with a local note - see Receive.java), just finish
//						if(noRemote)
//							Finish();
//						else {
//							// take local
//							TLog.v(TAG, "user chose local version for note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());
//							onChooseNote(localTitle.getText().ToString(),localEdit.getText().ToString(), true);
//						}
//					}
//		        });
				localTitle.SetText(localNote.getTitle());
				localEdit.SetText(localNote.getXmlContent());
			}
			
			messageView.Text = string.format(message,localNote.getTitle());
			remoteTitle.Text = (extras.GetString("title"));
			remoteEdit.Text = (extras.GetString("content"));

//			remoteBtn.SetOnClickListener( new View.OnClickListener() {
//				public void onClick(View v) {
//	            	// take local
//					TLog.v(TAG, "user chose remote version for note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());
//					onChooseNote(remoteTitle.getText().ToString(),remoteEdit.getText().ToString(), false);
//				}
//	        });
			
//			copyBtn.SetOnClickListener( new View.OnClickListener() {
//				public void onClick(View v) {
//	            	// take local
//					TLog.v(TAG, "user chose to create copy for note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());
//					copyNote();
//				}
//	        });
			
			// collapse notes
			collapseNote(localTitle, localEdit, true);
			collapseNote(remoteTitle, remoteEdit, true);
			diffView.Visibility = ViewStates.Gone;

//			diffLabel.SetOnClickListener( new View.OnClickListener() {
//				public void onClick(View v) {
//					diffView.SetVisibility(diffLabel.isChecked()?View.VISIBLE:View.GONE);
//				}
//	        });	
			
//			localLabel.SetOnClickListener( new View.OnClickListener() {
//				public void onClick(View v) {
//					collapseNote(localTitle, localEdit, !localLabel.isChecked());
//				}
//	        });	
//			remoteLabel.SetOnClickListener( new View.OnClickListener() {
//				public void onClick(View v) {
//					collapseNote(remoteTitle, remoteEdit, !remoteLabel.isChecked());
//				}
//	        });	
		}

		protected void copyNote() {

			TLog.v(TAG, "user chose to create new copy for conflicting note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());

			// not doing a title difference, get new guid for new note
			
			if(!differentNotes) {
				UUID newid = UUID.randomUUID();
				remoteNote.setGuid(newid.ToString());
			}
			
			string localTitle = ((EditText)FindViewById(Resource.Id.local_title)).getText().ToString();
			string remoteTitle = ((EditText)FindViewById(Resource.Id.remote_title)).getText().ToString();
			localNote.setTitle(localTitle);
			remoteNote.setTitle(remoteTitle);
			
			if(!localNote.getTitle().Equals(remoteNote.getTitle())) { // different titles, just create new note
			}
			else {
				
				// validate against existing titles
				string newTitle = NoteManager.validateNoteTitle(this, string.format(GetString(Resource.String.old),localNote.getTitle()), localNote.getGuid());
				
				if(dateDiff < 0) { // local older, rename local
					localNote.setTitle(newTitle);
					pullNote(localNote); // update local note with new title
				}
				else { // remote older, rename remote
					remoteNote.setTitle(newTitle);
				}
			}
				
			// add remote note to local
			pullNote(remoteNote);

			if(!noRemote) {
				pushNote(localNote);
				pushNote(remoteNote);
			}
			finishForResult(new Intent());
		}

		protected void onChooseNote(string title, string content, bool choseLocal) {
			title = NoteManager.validateNoteTitle(this, title, localNote.getGuid());
			
			Time now = new Time();
			now.SetToNow();
			string time = now.Format3339(false);
			
			localNote.setTitle(title);
			localNote.setXmlContent(content);
			localNote.setLastChangeDate(time);

			// doing a title difference

			if(differentNotes) {
				if(choseLocal) { // chose to keep local, delete remote, push local
					pullNote(localNote);
					remoteNote.addTag("system:deleted");
					
					if(noRemote) {
						finishForResult(new Intent());
						return;
					}
					
					pushNote(localNote); // add for pushing
					pushNote(remoteNote); // add for deletion
					
				}
				else { // chose to keep remote, delete local, add remote, push remote back 
					deleteNote(localNote);
					remoteNote.setTitle(title);
					remoteNote.setXmlContent(content);
					remoteNote.setLastChangeDate(time);
					pullNote(remoteNote);

					if(!noRemote)
						pushNote(remoteNote);
				}
			}
			else { // just readd and push modified localNote
				pullNote(localNote);

				if(!noRemote) 
					pushNote(localNote);
			}
			finishForResult(new Intent());
		}

		// local is deleted, delete remote as well
		protected void onChooseDelete() { 
			TLog.v(TAG, "user chose to delete remote note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());

			// this will delete the note, since it already has the "system:deleted" tag
			pushNote(localNote);
			Finish();
		}

		private void pullNote(Note note) {
			SyncManager.getInstance().getCurrentService().addPullable(note);
		}

		private void pushNote(Note note) {
			SyncManager.getInstance().getCurrentService().addPushable(note);
		}
		
		private void deleteNote(Note note) {
			SyncManager.getInstance().getCurrentService().addDeleteable(note);
		}

		
		private void updateTextAttributes(EditText title, EditText content) {
			float baseSize = Float.parseFloat(Preferences.GetString(Preferences.Key.BASE_TEXT_SIZE));
			content.SetTextSize(baseSize);
			title.SetTextSize(baseSize*1.3f);

			title.SetTextColor(Color.Blue);
			title.PaintFlags = title.PaintFlags | PaintFlags.UnderlineText;
			title.SetBackgroundColor(0xffffffff);

			content.SetBackgroundColor(0xffffffff);
			content.SetTextColor(Color.DarkGray);
		}

		private void collapseNote(EditText title, EditText content, bool collapse) {
			if(collapse) {
				title.Visibility = ViewStates.Gone;
				content.Visibility = ViewStates.Gone;
			}
			else {
				title.Visibility = ViewStates.Visible;
				content.Visibility = ViewStates.Visible;
			}
			
		}

		private void finishForResult(Intent data){
			if (Parent == null) {
			    SetResult(Result.Ok, data);
			} else {
			    Parent.SetResult (Result.Ok, data);
			}
			Finish();
		}
	}
}