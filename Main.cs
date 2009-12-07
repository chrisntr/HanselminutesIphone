
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Net;
using MonoTouch.AudioToolbox;
using MonoTouch.CoreFoundation;
using MonoTouch.AVFoundation;

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
		
		//AVAudioSession a;
		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// If you have defined a view, add it here:
			// window.AddSubview (navigationController.View);
			//UIApplication.SharedApplication.IdleTimerDisabled = true;
			
			//var r = new Reachability();
			//var reachable = r.IsHostReachable("www.google.com");
			
			HomeViewController hvc = new HomeViewController();
			window.AddSubview(hvc.View);
			window.MakeKeyAndVisible ();
			
			return true;
		}

		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
	}
}
