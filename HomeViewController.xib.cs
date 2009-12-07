
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Net;
using System.ComponentModel;
using System.IO;
using MonoTouch.AudioToolbox;
using MonoTouch.AVFoundation;
using System.Xml.Linq;

namespace Hanselminutes
{
	public partial class HomeViewController : UIViewController
	{
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public HomeViewController (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public HomeViewController (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		public HomeViewController () : base("HomeViewController", null)
		{
			Initialize ();
		}

		void Initialize ()
		{
		}
		
		#endregion
		
		AVAudioPlayer ap;
		//AVAudioSession a;
		List<Podcast> podcastList;
		WebClient client;
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			

			// This will work in 1.4 - Stops the playback from stopping
			// When phone is locked.
			/*var audioSession = AVAudioSession.SharedInstance();
			audioSession.SetActive(true, new NSError());
			audioSession.SetCategory(AVAudioSession.CategoryPlayback, new NSError());
			*/
			
			client = new WebClient();
			// Make Async...
			var twitterXML = client.DownloadString("http://feeds.feedburner.com/HanselminutesCompleteMP3");
			
			XDocument xDoc = XDocument.Parse(twitterXML);
			XNamespace ns = "";
			//XNamespace ns = "http://www.w3.org/2005/Atom";
			Console.WriteLine ("Entry descendants: " + xDoc.Descendants(ns + "item").Count());
			var podcasts = from x in xDoc.Descendants(ns + "item")
			select new Podcast
			{
				Url = x.Descendants( ns + "enclosure").Attributes("url").First().Value,
				Title = x.Descendants( ns + "title").First().Value
			};
			podcastList = podcasts.ToList();
			progressBar.Progress = 0;
			
			tableView.Source = new TableViewSource(this);
		}
		
		public void PlayAudioUrl(string url)
		{
			
			progressBar.Progress = 0;
			var fileName = Path.GetFileNameWithoutExtension(url);
			Console.WriteLine ("Playing Audio Url");
			if(ap != null)
			{
				Console.WriteLine ("We have an audio player already");
				if(ap.Playing)
					ap.Stop();
				ap.Dispose();
			}
			
			if(client != null)
			{
				client.CancelAsync();
				client.Dispose();
				client = null;
			}
			
			Boolean playing = false;
			client = new WebClient();
			var documents = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			client.DownloadFileCompleted += delegate(object sender, AsyncCompletedEventArgs e) {
				Console.WriteLine (	"Done!");
			};
			
			client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e) {
				InvokeOnMainThread( delegate {
					progressBar.Progress = (float) (e.ProgressPercentage * 0.01f);	
				});
				
				if(!playing && e.ProgressPercentage == 1)
				{
					Console.WriteLine ("Start playing mp3");
					InvokeOnMainThread( delegate {
						playing = true;
						ap = AVAudioPlayer.FromUrl(NSUrl.FromFilename(Path.Combine(documents, fileName + ".mp3")));
						
						ap.Volume = 1;
						
						Console.WriteLine ("PLAYING!!! duration  " + ap.Duration);
						Console.WriteLine ("PLAYING!!! url  " + ap.Url);
						Console.WriteLine ("Playing!!! time " + ap.CurrentTime);
						
						var worked = ap.Play();
						
						ap.FinishedPlaying += delegate(object sender2, AVStatusEventArgs e2) {
							Console.WriteLine ("clean up audio player...");
							ap.Dispose();
						};
						Console.WriteLine ("worked? " + worked);
						Console.WriteLine ("Playing? " + ap.Playing);
							
					});
				}
			};
			client.DownloadFileAsync(new Uri(url), Path.Combine(documents, fileName + ".mp3"));
			
		}
		
		public class TableViewSource : UITableViewSource
		{
			HomeViewController _hvc;
			public TableViewSource(HomeViewController hvc)
			{
				_hvc = hvc;
			}
			
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				string cellIdentifier = "repeatCell";
				
				var cell = tableView.DequeueReusableCell (cellIdentifier);
				if (cell == null){
					cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}
				cell.TextLabel.Text = _hvc.podcastList[indexPath.Row].Title; //mvc.samples [indexPath.Row].Title;
				return cell;
			}
			
			public override int RowsInSection (UITableView tableview, int section)
			{
				return _hvc.podcastList.Count();
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				Console.WriteLine ("Selected " + _hvc.podcastList[indexPath.Row].Title);
				_hvc.PlayAudioUrl(_hvc.podcastList[indexPath.Row].Url);
				tableView.DeselectRow(indexPath, true);
			}


		}
		
		partial void buttonPressed (UIButton sender)
		{
			UIView view = new UIView(UIScreen.MainScreen.Bounds);
			view.BackgroundColor = UIColor.Brown;
			this.View.AddSubview(view);
		}

	}
}
