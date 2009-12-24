
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Net;
using MonoTouch.AudioToolbox;
using MonoTouch.CoreFoundation;
using MonoTouch.AVFoundation;
using System.Drawing;
using MonoTouch.ObjCRuntime;
using System.IO;

namespace Hanselminutes
{
	public class Application
	{
		static void Main (string[] args)
		{
			UIApplication.Main (args);
		}
	}

	// The name AppDelegate is referenced in the MainWindow.xib file.
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIImageView splashView;
		
		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// If you have defined a view, add it here:
			// window.AddSubview (navigationController.View);
			//UIApplication.SharedApplication.IdleTimerDisabled = true;
			
			//var r = new Reachability();
			//var reachable = r.IsHostReachable("www.google.com");
			
			
			HomeViewController hvc = new HomeViewController();
			hvc.View.Frame = new RectangleF(0f, 20f, 320f, 460f);
			window.AddSubview(hvc.View);
			window.MakeKeyAndVisible ();
			
			showSplashScreen();
			return true;
		}

		void showSplashScreen ()
		{
			splashView = new UIImageView(new RectangleF(0f, 0f, 320f, 480f));
			splashView.Image = UIImage.FromFile("Default.png");
			window.AddSubview(splashView);
			window.BringSubviewToFront(splashView);
			UIView.BeginAnimations("SplashScreen");
			UIView.SetAnimationDuration(0.5f);
			UIView.SetAnimationTransition(UIViewAnimationTransition.None, window, true);
			UIView.SetAnimationDidStopSelector(new Selector("StartupAnimationDone"));
		    splashView.Alpha = 0f;
		    splashView.Frame = new RectangleF(-60f, -60f, 440f, 600f);
		    UIView.CommitAnimations();
		}

		void StartupAnimationDone()
		{
			splashView.RemoveFromSuperview();
			splashView.Dispose();
		}

		public override void WillTerminate (UIApplication application)
		{
			// Clean up Partial files...
			var documents = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			var document = Directory.GetFiles(documents);
			foreach(var file in document.Where(x => x.Contains("_partial.mp3")))
			{	
				Console.WriteLine ("Should delete - " + file);
				File.Delete(file);
			}
			//var partialFilePath = Path.Combine(documents, fileName + "_partial.mp3");
		}

		
		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
	}
}
