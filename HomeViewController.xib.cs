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
using MonoTouch.ObjCRuntime;

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
		
		const string _podcastListFileName = "PodcastListXml.xml";
		const string _hanselminuteMp3RssFeed = "http://feeds.feedburner.com/HanselminutesCompleteMP3";
		string _documents = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		
		AVAudioPlayer _audioPlayer;
		List<Podcast> _podcastList;
		WebClient _webClient;
		NSTimer _updateTimer;
		NSTimer _updateTotalTimer;
		double _bytesReceived;
		bool _playing;
		string _fileName;
		bool _fileDownloaded;
		bool _downloadCompletedSuccessfully;
		UILoadingView _loadingView;
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			tableView.BackgroundColor = UIColor.Clear;
			
			SetAudioSession(AVAudioSession.CategoryPlayback);
			
			StartInternetActivity();
			StartLoadingScreen("Gathering Podcast List...");
				
			_podcastList = new List<Podcast>();
			tableView.Source = new TableViewSource(this);
			GetPodcastList ();
		}
		
		/// <summary>
		/// Gets a list of podcasts from local storage or online.
		/// </summary>
		private void GetPodcastList ()
		{
			var r = new Reachability();
			var HasConnection = r.IsHostReachable("www.google.com"); 
			
			if(HasConnection)
			{
				Console.WriteLine ("Have connection - get internet version");
				_webClient = new WebClient();
				// Make Async...
				var url = _hanselminuteMp3RssFeed;
				_webClient.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs e) {
					
					SaveOnDevice(e.Result);
					DisplayPodcastList (e.Result);
					
					StopLoadingScreen();
					StopInternetActivity();
				};
				_webClient.DownloadStringAsync(new Uri(url));
			}
			else if (!HasConnection && HasLocalXml())
			{
				Console.WriteLine ("No connection but use local version");
				DisplayPodcastList(GetLocalPodcastListXml());
			}
			else
			{
				Console.WriteLine ("No internet - no local version");
				using (var alert = new UIAlertView("Whoops", "You'll need to connect to the internet to get a list of Podcasts", null, "Ok", null))
				{	
					alert.Dismissed += delegate {
						Console.WriteLine ("Dismissed Alert");
						StopInternetActivity();
						StopLoadingScreen();
					};
					alert.Show();
				}	
			}
		}
		
		/// <summary>
		/// Uses the XML string to display podcasts in the table view.
		/// </summary>
		private void DisplayPodcastList (string podcastXml)
		{
			var podcasts = PodcastHelper.ParsePodcastXml(podcastXml);
			(tableView.Source as TableViewSource)._podcastList = podcasts;
			InvokeOnMainThread( delegate { 
				tableView.ReloadData();
				ResetProgressBar();
				StopInternetActivity();
				StopLoadingScreen();
			});
		}

		public void PlayAudioUrl (string url)
		{
			
			ResetProgressBar();
			
			_fileDownloaded = false;
			_downloadCompletedSuccessfully = false;
			_fileName = Path.GetFileNameWithoutExtension(url);

			CancelCurrentAudioPlayback ();
			CancelCurrentClientCalls();
						
			var fileLength = 0;
			_playing = false;
			slider.Enabled = false;
			
			var partialFilePath = Path.Combine(_documents, _fileName + "_partial.mp3");
			var offlineFilePath = Path.Combine(_documents, _fileName + ".mp3");
			if(OfflineMp3Available(offlineFilePath))
			{
				_fileDownloaded = true;
				PlaybackAudio(offlineFilePath);
			}
			else
			{
				StartInternetActivity();
				StartLoadingScreen("Buffering...");
				DownloadPodcastFile (partialFilePath, offlineFilePath, url);
			}
			
		}
		
		void DownloadPodcastFile (string partialFilePath, string offlineFilePath, string url)
		{
			_webClient = new WebClient();
			_webClient.DownloadFileCompleted += delegate(object sender, AsyncCompletedEventArgs e) {
				_fileDownloaded = true;
				if(_downloadCompletedSuccessfully)
				{
					File.Copy(partialFilePath, offlineFilePath);
				}
				StopInternetActivity();
			};
			_webClient.DownloadProgressChanged += HandleClientDownloadProgressChanged;
			_webClient.DownloadFileAsync(new Uri(url), partialFilePath);
		}
		

		
		void CancelCurrentAudioPlayback ()
		{
			if(_audioPlayer != null)
			{
				if(_audioPlayer.Playing)
					_audioPlayer.Stop();
				_audioPlayer.Dispose();
			}
		}

		void CancelCurrentClientCalls ()
		{
			if(_webClient != null)
			{
				_webClient.CancelAsync();
				_webClient.Dispose();
				_webClient = null;
			}
		}

		void HandleClientDownloadProgressChanged (object sender2, DownloadProgressChangedEventArgs e2)
		{
			_bytesReceived = e2.BytesReceived;
			_fileDownloaded = false;
			
			if(e2.ProgressPercentage == 100)
			{	
				_downloadCompletedSuccessfully = true;
			}
			
			InvokeOnMainThread( delegate {
				progressBar.Progress = (float) (e2.ProgressPercentage * 0.01f);	
			});
			
			if(BufferedSignificantly(_playing, e2.ProgressPercentage))
			{
				PlaybackAudio (Path.Combine(_documents, _fileName + "_partial.mp3"));
			}
		}
		
		void PlaybackAudio (string filePath)
		{
			StopLoadingScreen();
			InvokeOnMainThread( delegate {
				
				slider.Enabled = true;
				_playing = true;
				
				_audioPlayer = AVAudioPlayer.FromUrl(NSUrl.FromFilename(filePath));
				_audioPlayer.BeginInterruption += delegate(object sender, EventArgs e) {
					Console.WriteLine ("Begin Interruptions");
					UpdateViewForPlayerState();
				};
				_audioPlayer.EndInterruption += delegate(object sender, EventArgs e) {
					Console.WriteLine ("End Interruption");
					StartPlayback();
				};
				_audioPlayer.DecoderError += delegate(object sender, AVErrorEventArgs e) {
					Console.WriteLine ("Decoder Error");
				};
				
				_audioPlayer.FinishedPlaying += delegate(object sender, AVStatusEventArgs e) {
					Console.WriteLine ("Finished Playing..." + e.Status);
					_audioPlayer.CurrentTime = 0;
				};
				
				UpdateViewForPlayerInfo();
				UpdateViewForPlayerState();
	
				slider.TouchDown += delegate {
					PausePlayback();
				};
				
				slider.ValueChanged += delegate	{
					_audioPlayer.CurrentTime = slider.Value;
					UpdateCurrentTime();
				};
				
				// Start playing selected Podcast
				StartPlayback();
			});
		}

		bool BufferedSignificantly (bool playing, int ProgressPercentage)
		{
			// Should add in proper logic to buffer the track - for now,
			// when 1% of a track has downloaded - start playing.
			return !playing && ProgressPercentage == 1;
		}
		
		void StartPlayback()	
		{
			if(_audioPlayer != null) {
				if(_audioPlayer.Play())
				{
					slider.Enabled = true;
					UpdateViewForPlayerState();
				}
				else
					Console.WriteLine ("Could not play " + _audioPlayer.Url);
			}
		}
		
		void PausePlayback()
		{
			_audioPlayer.Stop();// We might just stop it instead...
			UpdateViewForPlayerState();
		}

		#region Timer Methods and Updates
		
		/// <summary>
		/// Handle update timers and button presses.
		/// </summary>
		void UpdateViewForPlayerState ()
		{
			UpdateCurrentTime();
			
			if (_updateTimer != null) 
				_updateTimer.Invalidate();
			
			if (_updateTotalTimer != null)
				_updateTotalTimer.Invalidate();
		
			if(!_fileDownloaded)
			{
				_updateTotalTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromSeconds(0.01), UpdateTotalTime);
			}
			
			if(_audioPlayer.Playing)
				_updateTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromSeconds(0.01), UpdateCurrentTime);
			else
				_updateTimer = null;
			
			//[_playButton setImage:((self._player.playing == YES) ? _pauseBtnBG : _playBtnBG) forState:UIControlStateNormal];
			modalButton.SetTitle((_audioPlayer.Playing) ? "Pause" : "Play", UIControlState.Normal); 
		}

		/// <summary>
		/// Updates the current playing time.
		/// </summary>
		void UpdateCurrentTime()
		{
			InvokeOnMainThread( delegate {
				if(_audioPlayer.CurrentTime.ToString() == "NaN")
					currentTime.Text = "0:00";
				else
					currentTime.Text = String.Format("{0}:{1:00}", (int) _audioPlayer.CurrentTime / 60, _audioPlayer.CurrentTime % 60);
			
				slider.Value = (int) _audioPlayer.CurrentTime;
					
			});
		}
		
		/// <summary>
		/// Update the Total time from byte count.
		/// </summary>
		void UpdateTotalTime()
		{
			
			// Update ending time since we're progressively downloading stuff...
			var currentDuration = GetCurrentAudioLength(_bytesReceived);
			
			totalTime.Text = String.Format("{0}:{1:00}", (int) currentDuration / 60, currentDuration % 60);
			slider.MaxValue = (int) currentDuration;
		}
		
		/// <summary>
		/// Update the Total Time from the duration. 
		/// </summary>
		void UpdateViewForPlayerInfo()
		{
		    totalTime.Text = String.Format("{0}:{1:00}", (int) _audioPlayer.Duration / 60, _audioPlayer.Duration % 60);
		
			slider.MaxValue = (int) _audioPlayer.Duration;
		}
		
		#endregion
		
		#region Audio Helper Methods
		
		/// <summary>
		/// Converts byte count and bit rate into time in seconds - will use 96kbps as bit rate.
		/// </summary>
		double GetCurrentAudioLength(double byteCount)
		{	
			return GetCurrentAudioLength(byteCount, Convert.ToDouble(96));
		}
		
		/// <summary>
		/// Converts byte count and bit rate into time in seconds.
		/// </summary>
		double GetCurrentAudioLength(double byteCount, double bitRate)
		{
			var bits = byteCount * 8;
			var bitsInBitRate = bits / bitRate;
			var seconds = bitsInBitRate / 1000;
			return seconds;
		}
		
		/// <summary>
		/// Sets the Audio Session for the iPhone application.
		/// </summary>
		void SetAudioSession (string audioCategory)
		{
			var audioSession = AVAudioSession.SharedInstance();
			audioSession.SetActive(true, new NSError());
			audioSession.SetCategory(audioCategory, new NSError());
		}
		
		#endregion
		
		#region UI Helper Methods
		
		

		/// <summary>
		/// Resets the progress bar.
		/// </summary>
		void ResetProgressBar ()
		{
			progressBar.Progress = 0;
		}

		void StopInternetActivity ()
		{
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
		}
		
		void StartInternetActivity ()
		{
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
		}
		
		/// <summary>
		/// Creates a Loading Screen with the specified message.
		/// </summary>
		void StartLoadingScreen(string message)
		{
			using (var pool = new NSAutoreleasePool())
			{
				_loadingView = new UILoadingView(message);
				this.View.BringSubviewToFront(_loadingView);
				this.View.AddSubview(_loadingView);
				this.View.UserInteractionEnabled = false;
			}
		}
		
		/// <summary>
		/// If a loading screen exists, it will fade it out.
		/// </summary>
		void StopLoadingScreen()
		{
			using (var pool = new NSAutoreleasePool())
			{
				if(_loadingView != null)
					_loadingView.FadeOutAndRemove();
				this.View.UserInteractionEnabled = true;
			}
		}
		
		#endregion
		
		#region File Helper Methods
		
		/// <summary>
		/// Checks if a Offline Mp3 is available.
		/// </summary>
		bool OfflineMp3Available (string offlineFilePath)
		{
			if(File.Exists(offlineFilePath))
				return true;
			return false;
		}
		
		/// <summary>
		/// Saves the podcast xml string locally as a file on the device.
		/// </summary>
		void SaveOnDevice (string podcastXmlString)
		{
			var filePath = Path.Combine(_documents, _podcastListFileName);
			File.WriteAllText(filePath, podcastXmlString);
		}
		
		/// <summary>
		/// Checks to see if we have a local podcast xml string.
		/// </summary>
		bool HasLocalXml ()
		{
			return File.Exists(Path.Combine(_documents, _podcastListFileName));
		}

		/// <summary>
		/// Reads in the local podcast xml string. 
		/// </summary>
		string GetLocalPodcastListXml ()
		{
			return File.ReadAllText(Path.Combine(_documents, _podcastListFileName));
		}
		
		#endregion
		
		#region Outlet Events
		
		partial void PlayButtonPressed (UIButton sender)
		{
			if(_audioPlayer != null && _audioPlayer.Playing)
				PausePlayback();	
			else if (_audioPlayer != null)
				StartPlayback();	
		}
		
		partial void DisplayButtonPressed (UIButton sender)
		{
			var pvc = new PlayerViewController(this);
			this.PresentModalViewController(pvc, true);
		}
		
		#endregion

		public class TableViewSource : UITableViewSource
		{
			HomeViewController _hvc;
			public List<Podcast> _podcastList {
				get;
				set;
			}
			
			public TableViewSource(HomeViewController hvc)
			{
				_hvc = hvc;
				_podcastList = hvc._podcastList;
			}
			
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				string cellIdentifier = "repeatCell";
				
				var cell = tableView.DequeueReusableCell (cellIdentifier);
				if (cell == null){
					cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);
					cell.Opaque = true;
					cell.TextLabel.ContentMode = UIViewContentMode.ScaleAspectFill;
				}
				cell.TextLabel.Text = _podcastList[indexPath.Row].Title; //mvc.samples [indexPath.Row].Title;
				
				if (HasOfflineVersion(_podcastList[indexPath.Row].Url)) 
					cell.Accessory = UITableViewCellAccessory.Checkmark;	
				else
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;	
				return cell;
			}

			bool HasOfflineVersion (string url)
			{
				var fileName = Path.GetFileNameWithoutExtension(url);
				var offlineFilePath = Path.Combine(_hvc._documents, fileName + ".mp3");
				return File.Exists(offlineFilePath);
			}

			
			public override int RowsInSection (UITableView tableview, int section)
			{
				return _podcastList.Count();
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				var url = _podcastList[indexPath.Row].Url;
				var fileName = Path.GetFileNameWithoutExtension(url);
				var offlineFilePath = Path.Combine(_hvc._documents, fileName + ".mp3");

				Console.WriteLine ("Selected " + _podcastList[indexPath.Row].Title);
				
				var r = new Reachability();
				
				if(!r.IsHostReachable("www.google.com") && _hvc.OfflineMp3Available(offlineFilePath)) 
					_hvc.PlayAudioUrl(url);
				
				else if(!r.IsHostReachable("www.google.com"))
				{
					using (var alert = new UIAlertView("Whoops", "You'll need to be on the internet to do this", null, "Ok", null))
					{
						alert.Show();
					}
				}
				else
					_hvc.PlayAudioUrl(url);	 

				tableView.DeselectRow(indexPath, true);
			}
		}
	}
}
