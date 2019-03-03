// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace EOS_calib
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		UIKit.UIImageView CrossHair { get; set; }

		[Outlet]
		UIKit.UIImageView ImagePreview { get; set; }

		[Outlet]
		UIKit.UIView liveCameraStream { get; set; }

		[Outlet]
		UIKit.UIButton takeimageButton { get; set; }

		[Action ("captureSpectraTouchUpInside:")]
		partial void captureSpectraTouchUpInside (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (CrossHair != null) {
				CrossHair.Dispose ();
				CrossHair = null;
			}

			if (ImagePreview != null) {
				ImagePreview.Dispose ();
				ImagePreview = null;
			}

			if (liveCameraStream != null) {
				liveCameraStream.Dispose ();
				liveCameraStream = null;
			}

			if (takeimageButton != null) {
				takeimageButton.Dispose ();
				takeimageButton = null;
			}
		}
	}
}
