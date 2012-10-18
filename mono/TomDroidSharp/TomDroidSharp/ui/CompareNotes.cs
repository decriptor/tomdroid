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

//import java.util.Arrays;
//import java.util.UUID;
//import java.util.regex.Matcher;
//import java.util.regex.Pattern;

using TomDroidSharp.Note;
using TomDroidSharp.NoteManager;
using TomDroidSharp.R;
using TomDroidSharp.sync.SyncManager;
using TomDroidSharp.ui.actionbar.ActionBarActivity;
using TomDroidSharp.util.Preferences;
using TomDroidSharp.util.TLog;

//import difflib.Delta;
//import difflib.DiffUtils;
//import difflib.Patch;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;

namespace TomDroidSharp.ui
{

	public class CompareNotes : ActionBarActivity {	
		private static readonly string TAG = "SyncDialog";
		
		private Note localNote;
		private boolean differentNotes;
		private Note remoteNote;
		private int dateDiff;
		private boolean noRemote;

		@Override	
		public void onCreate(Bundle savedInstanceState) {	
			super.onCreate(savedInstanceState);	
			
			if(!this.getIntent().hasExtra("datediff")) {
				TLog.v(TAG, "no date diff");
				finish();
				return;
			}
			TLog.v(TAG, "starting CompareNotes");
			
			setContentView(R.layout.note_compare);
			
			Bundle extras = this.getIntent().getExtras();

			remoteNote = new Note();
			remoteNote.setTitle(extras.getstring("title"));
			remoteNote.setGuid(extras.getstring("guid"));
			remoteNote.setLastChangeDate(extras.getstring("date"));
			remoteNote.setXmlContent(extras.getstring("content"));	
			remoteNote.setTags(extras.getstring("tags"));
			
			ContentValues values = new ContentValues();
			values.put(Note.TITLE, extras.getstring("title"));
			values.put(Note.FILE, extras.getstring("file"));
			values.put(Note.GUID, extras.getstring("guid"));
			values.put(Note.MODIFIED_DATE, extras.getstring("date"));
			values.put(Note.NOTE_CONTENT, extras.getstring("content"));
			values.put(Note.TAGS, extras.getstring("tags"));
			 
			dateDiff = extras.getInt("datediff");
			noRemote = extras.getBoolean("noRemote");
			
			// check if we're comparing two different notes with same title
			
			differentNotes = getIntent().hasExtra("localGUID"); 
			if(differentNotes) {
				localNote = NoteManager.getNoteByGuid(this, extras.getstring("localGUID"));
				TLog.v(TAG, "comparing two different notes with same title");
			}
			else {
				localNote = NoteManager.getNoteByGuid(this, extras.getstring("guid"));
				TLog.v(TAG, "comparing two versions of the same note");
			}
			
			boolean deleted = localNote.getTags().contains("system:deleted"); 
			
			string message;

			Button localBtn = (Button)findViewById(R.id.localButton);
			Button remoteBtn = (Button)findViewById(R.id.remoteButton);
			Button copyBtn = (Button)findViewById(R.id.copyButton);
			
			TextView messageView = (TextView)findViewById(R.id.message);
			
			ToggleButton diffLabel = (ToggleButton)findViewById(R.id.diff_label);
			ToggleButton localLabel = (ToggleButton)findViewById(R.id.local_label);
			ToggleButton remoteLabel = (ToggleButton)findViewById(R.id.remote_label);

			EditText localTitle = (EditText)findViewById(R.id.local_title);
			EditText remoteTitle = (EditText)findViewById(R.id.remote_title);
			
			TextView diffView = (TextView)findViewById(R.id.diff);
			EditText localEdit = (EditText)findViewById(R.id.local);
			EditText remoteEdit = (EditText)findViewById(R.id.remote);


			updateTextAttributes(localTitle, localEdit);
			updateTextAttributes(remoteTitle, remoteEdit);
			
			if(deleted) {
				TLog.v(TAG, "comparing deleted with remote");
				message = getstring(R.string.sync_conflict_deleted);
				
				diffLabel.setVisibility(View.GONE);
				localLabel.setVisibility(View.GONE);
				diffView.setVisibility(View.GONE);
				localEdit.setVisibility(View.GONE);
				localTitle.setVisibility(View.GONE);

				copyBtn.setVisibility(View.GONE);
				
				// if importing note, offer cancel import option to open main screen
				if(noRemote) {
					localBtn.setText(getstring(R.string.btnCancelImport));
					localBtn.setOnClickListener( new View.OnClickListener() {
						public void onClick(View v) {
							finishForResult(new Intent());
						}
			        });
				}
				else {
					localBtn.setText(getstring(R.string.delete_remote));
					localBtn.setOnClickListener( new View.OnClickListener() {
						public void onClick(View v) {
							onChooseDelete();
						}
			        });
				}
			}
			else {
				string diff = "";
				boolean titleMatch = localNote.getTitle().equals(extras.getstring("title"));
				
				if(differentNotes)
					message = getstring(R.string.sync_conflict_titles_message);
				else
					message = getstring(R.string.sync_conflict_message);
				
				if(!titleMatch) {
					diff = "<b>"+getstring(R.string.diff_titles)+"</b><br/><i>"+getstring(R.string.local_label)+"</i><br/> "+localNote.getTitle()+"<br/><br/><i>"+getstring(R.string.remote_label)+"</i><br/>"+extras.getstring("title");		
				}

				if(localNote.getXmlContent().equals(extras.getstring("content").replaceAll("</*link:[^>]+>", ""))) {
					TLog.v(TAG, "compared notes have same content");
					if(titleMatch) { // same note, fix the dates
						if(extras.getInt("datediff") < 0) { // local older
							TLog.v(TAG, "compared notes have same content and titles, pulling newer remote");
							pullNote(remoteNote);
						}
						else if(extras.getInt("datediff") == 0 || noRemote) {
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
							finish();
						
						return;
					}
					else {
						TLog.v(TAG, "compared notes have different titles");
			            diffView.setText(diff);
		    			localEdit.setVisibility(View.GONE);
		    			remoteEdit.setVisibility(View.GONE);					
					}
				}
				else {
					TLog.v(TAG, "compared notes have different content");
					if(titleMatch && !differentNotes) {
		    			localTitle.setVisibility(View.GONE);
		    			remoteTitle.setVisibility(View.GONE);
					}
					else
		    			diff += "<br/><br/>";

					Patch patch = DiffUtils.diff(Arrays.asList(TextUtils.split(localNote.getXmlContent(), "\\r?\\n|\\r")), Arrays.asList(TextUtils.split(extras.getstring("content"), "\\r?\\n|\\r")));
		            string diffResult = "";
					for (Delta delta: patch.getDeltas()) {
		            	diffResult += delta.tostring()+"<br/>";
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
	                		"<b>"+string.format(getstring(R.string.line_x),string.valueOf(Integer.parseInt(matcher.group(1)) + 1))+"</b><br/><i>"
	                		+getstring(R.string.local_label)+":</i><br/>"+matcher.group(2)+"<br/><br/><i>"
	                		+getstring(R.string.remote_label)+":</i><br/>"+matcher.group(3)+"<br/>"
		                );
		            }
		            matcher.appendTail(result);

		            matcher = secondPattern.matcher(result);
		            result = new stringBuffer();
		            while (matcher.find())
		            {
		                matcher.appendReplacement(
		                	result, 
							"<b>"+string.format(getstring(R.string.line_x),string.valueOf(Integer.parseInt(matcher.group(1)) + 1))+"</b><br/><i>"
		                	+getstring(R.string.remote_label)+":</i><br/>"+matcher.group(2)+"<br/><br/>"

		                );
		            }
		            matcher.appendTail(result);

		            matcher = thirdPattern.matcher(result);
		            result = new stringBuffer();
		            while (matcher.find())
		            {
		                matcher.appendReplacement(
		                	result, 
							"<b>"+string.format(getstring(R.string.line_x),string.valueOf(Integer.parseInt(matcher.group(1)) + 1))+"</b><br/><i>"
		                	+getstring(R.string.local_label)+":</i><br/>"+matcher.group(2)+"<br/><br/>"

		                );
		            }
		            matcher.appendTail(result);
		            
					diff += "<b>"+getstring(R.string.diff_content)+"</b><br/>";		
		            
		            diff += result;
					
		            diff = diff.replace("\n","<br/>");
		            
		            diffView.setText(Html.fromHtml(diff));
					
				}
				
				if(noRemote) {
					localBtn.setText(getstring(R.string.btnCancelImport));
					message = getstring(R.string.sync_conflict_import_message);
				}
				
				localBtn.setOnClickListener( new View.OnClickListener() {
					public void onClick(View v) {

						// check if there is no remote (e.g. we are receiving a note file that conflicts with a local note - see Receive.java), just finish
						if(noRemote)
							finish();
						else {
							// take local
							TLog.v(TAG, "user chose local version for note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());
							onChooseNote(localTitle.getText().tostring(),localEdit.getText().tostring(), true);
						}
					}
		        });
				localTitle.setText(localNote.getTitle());
				localEdit.setText(localNote.getXmlContent());
			}
			
			messageView.setText(string.format(message,localNote.getTitle()));
			remoteTitle.setText(extras.getstring("title"));
			remoteEdit.setText(extras.getstring("content"));

			remoteBtn.setOnClickListener( new View.OnClickListener() {
				public void onClick(View v) {
	            	// take local
					TLog.v(TAG, "user chose remote version for note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());
					onChooseNote(remoteTitle.getText().tostring(),remoteEdit.getText().tostring(), false);
				}
	        });
			
			copyBtn.setOnClickListener( new View.OnClickListener() {
				public void onClick(View v) {
	            	// take local
					TLog.v(TAG, "user chose to create copy for note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());
					copyNote();
				}
	        });
			
			// collapse notes
			collapseNote(localTitle, localEdit, true);
			collapseNote(remoteTitle, remoteEdit, true);
			diffView.setVisibility(View.GONE);

			diffLabel.setOnClickListener( new View.OnClickListener() {
				public void onClick(View v) {
					diffView.setVisibility(diffLabel.isChecked()?View.VISIBLE:View.GONE);
				}
	        });	
			
			localLabel.setOnClickListener( new View.OnClickListener() {
				public void onClick(View v) {
					collapseNote(localTitle, localEdit, !localLabel.isChecked());
				}
	        });	
			remoteLabel.setOnClickListener( new View.OnClickListener() {
				public void onClick(View v) {
					collapseNote(remoteTitle, remoteEdit, !remoteLabel.isChecked());
				}
	        });	
		}

		protected void copyNote() {
			
			TLog.v(TAG, "user chose to create new copy for conflicting note TITLE:{0} GUID:{1}", localNote.getTitle(),localNote.getGuid());

			// not doing a title difference, get new guid for new note
			
			if(!differentNotes) {
				UUID newid = UUID.randomUUID();
				remoteNote.setGuid(newid.tostring());
			}
			
			string localTitle = ((EditText)findViewById(R.id.local_title)).getText().tostring();
			string remoteTitle = ((EditText)findViewById(R.id.remote_title)).getText().tostring();
			localNote.setTitle(localTitle);
			remoteNote.setTitle(remoteTitle);
			
			if(!localNote.getTitle().equals(remoteNote.getTitle())) { // different titles, just create new note
			}
			else {
				
				// validate against existing titles
				string newTitle = NoteManager.validateNoteTitle(this, string.format(getstring(R.string.old),localNote.getTitle()), localNote.getGuid());
				
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

		protected void onChooseNote(string title, string content, boolean choseLocal) {
			title = NoteManager.validateNoteTitle(this, title, localNote.getGuid());
			
			Time now = new Time();
			now.setToNow();
			string time = now.format3339(false);
			
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
			finish();
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
			float baseSize = Float.parseFloat(Preferences.getstring(Preferences.Key.BASE_TEXT_SIZE));
			content.setTextSize(baseSize);
			title.setTextSize(baseSize*1.3f);

			title.setTextColor(Color.BLUE);
			title.setPaintFlags(title.getPaintFlags() | Paint.UNDERLINE_TEXT_FLAG);
			title.setBackgroundColor(0xffffffff);

			content.setBackgroundColor(0xffffffff);
			content.setTextColor(Color.DKGRAY);
		}

		private void collapseNote(EditText title, EditText content, boolean collapse) {
			if(collapse) {
				title.setVisibility(View.GONE);
				content.setVisibility(View.GONE);
			}
			else {
				title.setVisibility(View.VISIBLE);
				content.setVisibility(View.VISIBLE);
			}
			
		}

		private void finishForResult(Intent data){
			if (getParent() == null) {
			    setResult(Activity.RESULT_OK, data);
			} else {
			    getParent().setResult(Activity.RESULT_OK, data);
			}
			finish();
		}
	}
}