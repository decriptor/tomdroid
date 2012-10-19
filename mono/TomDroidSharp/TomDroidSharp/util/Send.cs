//import java.io.File;
//import java.io.FileNotFoundException;
//import java.io.FileOutputStream;
//import java.io.IOException;
//import java.io.OutputStreamWriter;

using System;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Text;

using Java.IO;

namespace TomDroidSharp.util
{
	public class Send {

		private string TAG = "Send";

		private Activity activity;
		private Note note;
		private StringBuilder noteContent;
		private int DIALOG_CHOOSE = 0;
		private bool sendAsFile;
		
		public Send(Activity activity, Android.Net.Uri uri, bool sendAsFile) {
			this.activity = activity;
			this.sendAsFile = sendAsFile;
			this.note = NoteManager.getNote(activity, uri);
		}
		
		public void send() {
			if (note != null) {
				noteContent = note.getNoteContent(noteContentHandler);
			}
		}
		
//		private Handler noteContentHandler = new Handler() {
//
//			public override void handleMessage(Message msg) {
//				
//				//parsed ok - show
//				if(msg.what == NoteContentBuilder.PARSE_OK) {
//					if(sendAsFile)
//						sendNoteAsFile();
//					else
//						sendNoteAsText();
//
//				//parsed not ok - error
//				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {
//					activity.ShowDialog(Tomdroid.DIALOG_PARSE_ERROR);
//
//	        	}
//			}
//		};
		
		private void sendNoteAsFile() {
			note.cursorPos = 0;
			note.width = 0;
			note.height = 0;
			note.X = -1;
			note.Y = -1;
			
			string xmlOutput = note.GetXmlFilestring();	
			
			FileOutputStream outFile = null;
			Android.Net.Uri noteUri = null;
			try {
				clearFilesDir();
				
				outFile = activity.OpenFileOutput(note.getGuid()+".note", FileCreationMode.WorldReadable);
				OutputStreamWriter osw = new OutputStreamWriter(outFile);
				osw.Write(xmlOutput);
				osw.Flush();
				osw.Close();

				File noteFile = activity.GetFileStreamPath(note.getGuid()+".note");
				noteUri = Android.Net.Uri.FromFile(noteFile);
				 
			} catch (FileNotFoundException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (IOException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			}
			if(noteUri == null) {
				TLog.e(TAG, "Unable to create note to send");
				return;
			}
			
		    // Create a new Intent to send messages
		    Intent sendIntent = new Intent(Intent.ActionSend);

		    // Add attributes to the intent
		    sendIntent.PutExtra(Intent.ExtraStream, noteUri);
		    sendIntent.Type = "text/plain";

		    activity.StartActivity(Intent.CreateChooser(sendIntent, note.getTitle()));
			return;
		}
		
		private void sendNoteAsText() {
			string body = noteContent.ToString();
			
		    // Create a new Intent to send messages
		    Intent sendIntent = new Intent(Intent.ActionSend);
		    // Add attributes to the intent

		    sendIntent.PutExtra(Intent.ExtraSubject, note.getTitle());
		    sendIntent.PutExtra(Intent.ExtraText, body);
		    sendIntent.Type = "text/plain";

		    activity.StartActivity(Intent.CreateChooser(sendIntent, note.getTitle()));
		}

		private void clearFilesDir() {
			File dir = activity.FilesDir;
			if(dir == null || !dir.Exists())
				return;
	        string[] children = dir.List();
	        foreach (string s in children) {
	            File f = new File(dir, s);
	            if(f.Name.EndsWith(".note"))
	            	f.Delete();
	        }
		}
	}
}