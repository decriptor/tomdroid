//import java.io.File;
//import java.io.FileNotFoundException;
//import java.io.FileOutputStream;
//import java.io.IOException;
//import java.io.OutputStreamWriter;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Text;

using TomDroidSharp.Note;
using TomDroidSharp.NoteManager;
using TomDroidSharp.ui.Tomdroid;

namespace TomDroidSharp.util
{
	public class Send {

		private string TAG = "Send";

		private Activity activity;
		private Note note;
		private SpannablestringBuilder noteContent;
		private int DIALOG_CHOOSE = 0;
		private bool sendAsFile;;
		
		public Send(Activity activity, Uri uri, bool sendAsFile) {
			this.activity = activity;
			this.sendAsFile = sendAsFile;
			this.note = NoteManager.getNote(activity, uri);
		}
		
		public void send() {
			if (note != null) {
				noteContent = note.getNoteContent(noteContentHandler);
			}
		}
		
		private Handler noteContentHandler = new Handler() {

			@Override
			public void handleMessage(Message msg) {
				
				//parsed ok - show
				if(msg.what == NoteContentBuilder.PARSE_OK) {
					if(sendAsFile)
						sendNoteAsFile();
					else
						sendNoteAsText();

				//parsed not ok - error
				} else if(msg.what == NoteContentBuilder.PARSE_ERROR) {
					activity.showDialog(Tomdroid.DIALOG_PARSE_ERROR);

	        	}
			}
		};
		
		private void sendNoteAsFile() {
			note.cursorPos = 0;
			note.width = 0;
			note.height = 0;
			note.X = -1;
			note.Y = -1;
			
			string xmlOutput = note.getXmlFilestring();	
			
			FileOutputStream outFile = null;
			Uri noteUri = null;
			try {
				clearFilesDir();
				
				outFile = activity.openFileOutput(note.getGuid()+".note", activity.MODE_WORLD_READABLE);
				OutputStreamWriter osw = new OutputStreamWriter(outFile);
				osw.write(xmlOutput);
				osw.flush();
				osw.close();
				
				File noteFile = activity.getFileStreamPath(note.getGuid()+".note");
				noteUri = Uri.fromFile(noteFile);
				 
			} catch (FileNotFoundException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			} catch (IOException e) {
				// TODO Auto-generated catch block
				e.printStackTrace();
			}
			if(noteUri == null) {
				TLog.e(TAG, "Unable to create note to send");
				return;
			}
			
		    // Create a new Intent to send messages
		    Intent sendIntent = new Intent(Intent.ACTION_SEND);

		    // Add attributes to the intent
		    sendIntent.putExtra(Intent.EXTRA_STREAM, noteUri);
		    sendIntent.setType("text/plain");

		    activity.startActivity(Intent.createChooser(sendIntent, note.getTitle()));
			return;
		}
		
		private void sendNoteAsText() {
			string body = noteContent.tostring();
			
		    // Create a new Intent to send messages
		    Intent sendIntent = new Intent(Intent.ACTION_SEND);
		    // Add attributes to the intent

		    sendIntent.putExtra(Intent.EXTRA_SUBJECT, note.getTitle());
		    sendIntent.putExtra(Intent.EXTRA_TEXT, body);
		    sendIntent.setType("text/plain");

		    activity.startActivity(Intent.createChooser(sendIntent, note.getTitle()));
		}

		private void clearFilesDir() {
			File dir = activity.getFilesDir();
			if(dir == null || !dir.Exists())
				return;
	        string[] children = dir.list();
	        for (string s : children) {
	            File f = new File(dir, s);
	            if(f.getName().endsWith(".note"))
	            	f.delete();
	        }
		}
	}
}