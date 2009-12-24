
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Hanselminutes
{
	public partial class PlayerViewController : UIViewController
	{
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public PlayerViewController (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public PlayerViewController (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		public PlayerViewController (HomeViewController hvc) : base("PlayerViewController", null)
		{
			_hvc = hvc;
			Initialize ();
		}

		void Initialize ()
		{
		}
		
		public HomeViewController _hvc {
			get;
			set;
		}
		#endregion
		
		partial void dismissModal (UIBarButtonItem sender)
		{
			_hvc.DismissModalViewControllerAnimated(true);
		}

		
	}
}
