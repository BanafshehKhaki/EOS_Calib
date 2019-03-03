using System;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using UIKit;
using AssetsLibrary;
using CoreImage;
using CoreMedia;


namespace EOS_calib
{
    public partial class ViewController : UIViewController
    {

        AVCaptureSession captureSession;
        AVCaptureDeviceInput captureDeviceInput;
        AVCaptureStillImageOutput stillImageOutput;
        AVCaptureVideoPreviewLayer videoPreviewLayer;
        NSData jpegAsByteArray; //pixel values that will be passed to resultViewController. 

        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            AuthorizeCameraUse();
            SetupLiveCameraStream();
            // Perform any additional setup after loading the view, typically from a nib.
        }
        //Connected to button to take the image
        partial void captureSpectraTouchUpInside(Foundation.NSObject sender)
        {
            /*
             * Erasing previous image because if user moves fast between pages, 
             * resultViewController could be calculating the previous image data
            */

            takeseriesPicsAsync();
            //when i used await, it crashed and hence not using it.  

        }

        private async void takeseriesPicsAsync()
        {
            for (int i =0; i < 10; i++)
            {
                jpegAsByteArray = null;
                takeThepictureAsync();
                await Task.Delay(30000); //30 seconds
            }

        }




        /*
         * Setting the connection to be from camera feed and capture the image
         * jpegAsByteArray is used to save image pixel values to send to resultViewController to calculate  concentration of sample     
         * Asynchoronouse task needs async 
         */
        public async Task takeThepictureAsync()
        {

            var videoConnection = stillImageOutput.ConnectionFromMediaType(AVMediaType.Video);
            var sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync(videoConnection);

            var jpegImageAsNsData = AVCaptureStillImageOutput.JpegStillToNSData(sampleBuffer);

            jpegAsByteArray = jpegImageAsNsData;

            var imageMeta = CIImage.FromData(jpegImageAsNsData);

            // creating an image and putting it in ImagePreview frame on top of the screen
            var imagePrev = new UIImage(jpegImageAsNsData);
            ImagePreview.Image = imagePrev;


           // Saving the complete image into the phone gallery with addition to its meta data 
            var meta = imageMeta.Properties.Dictionary.MutableCopy() as NSMutableDictionary;
           
            var library = new ALAssetsLibrary();
            library.WriteImageToSavedPhotosAlbum(jpegImageAsNsData, meta, (assetUrl, error) =>
            {
                if (error == null)
                {
                    Console.WriteLine("saved: ");//+jpegImageAsNsData);
                }
                else
                {
                    Console.WriteLine(error);
                    UIAlertView alert = new UIAlertView()
                    {
                        Title = "Alert!",
                        Message = "There was a problem with saving your image, please take a new picture"
                    };
                    alert.AddButton("OK");
                    alert.Show();

                }
            });

        }

        async Task AuthorizeCameraUse()
        {
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);
            if (authorizationStatus != AVAuthorizationStatus.Authorized)
            {
                await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
        }

        public void SetupLiveCameraStream()
        {
            captureSession = new AVCaptureSession();

            //.PresetPhoto for camera image feed
            captureSession.SessionPreset = AVCaptureSession.PresetPhoto;

            var viewLayer = liveCameraStream.Layer;
            videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
            {
                Frame = this.View.Frame,
            };

            liveCameraStream.Layer.AddSublayer(videoPreviewLayer);
            var captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);

            ConfigureCameraForDevice(captureDevice);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);
            captureSession.AddInput(captureDeviceInput);

            var dictionary = new NSMutableDictionary
            {
                [AVVideo.CodecKey] = new NSNumber((int)AVVideoCodec.JPEG)
            };
            stillImageOutput = new AVCaptureStillImageOutput
            {
                OutputSettings = new NSDictionary()
            };

            captureSession.AddOutput(stillImageOutput);
            captureSession.StartRunning();

        }


        /*
         * Setting up camera setting for EOSpec:
         * Focus: 0.3 locked : Empirically chosen for set distance to current version of EOSpec
         * ISO : Avg between max and min ISO of device          
         * Exposure : Empirically set to 1/10       
         * RGB gain: R and G normalized using max Gain values, 
         * however Blue is normalized using min Gain value due to it saturating more than green and red channels (my phone: 1.858154 , 1 ,  1)
        */
        void ConfigureCameraForDevice(AVCaptureDevice device)
        {
            var error = new NSError();
            if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                device.LockForConfiguration(out error);
                device.FocusMode = AVCaptureFocusMode.Locked;
                device.SetFocusModeLocked((float)0.3, null);

                Console.WriteLine("devidce device.FocusMode: " + device.FocusMode);
                device.UnlockForConfiguration();
            }

            if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
            {
                device.LockForConfiguration(out error);
                device.ExposureMode = AVCaptureExposureMode.Custom;

                var iso = (device.ActiveFormat.MaxISO + device.ActiveFormat.MinISO) / 2;
                CMTime exposureTime = new CMTime(1, 10);
                device.LockExposure(exposureTime, iso, null);
                Console.WriteLine("deviceexposure: " + device.ExposureDuration + " = " + exposureTime + " iso: " + iso);

                device.UnlockForConfiguration();

                //Setting RGB gains using WhiteBalance
                device.LockForConfiguration(out error);
                AVCaptureWhiteBalanceGains gains = device.DeviceWhiteBalanceGains;
                //normalizign Gains
                gains.RedGain = Math.Max(1, gains.RedGain);
                gains.GreenGain = Math.Max(1, gains.GreenGain);
                gains.BlueGain = Math.Min(1, gains.BlueGain);

                float maxGain = device.MaxWhiteBalanceGain;
                gains.RedGain = Math.Min(maxGain, gains.RedGain);
                gains.GreenGain = Math.Min(maxGain, gains.GreenGain);
                gains.BlueGain = Math.Min(maxGain, gains.BlueGain);
                Console.WriteLine("devidce device.WhiteBalanceMode gains RGB: " + gains.RedGain + " " + gains.GreenGain + " " + gains.BlueGain);

                device.SetWhiteBalanceModeLockedWithDeviceWhiteBalanceGains(gains, null);
                device.UnlockForConfiguration();
            }
        }


        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
        }

        protected override void Dispose(bool disposing)
        {
            captureSession.Dispose();
            captureDeviceInput.Dispose();
            stillImageOutput.Dispose();
            jpegAsByteArray.Dispose();
            base.Dispose(disposing);
        }


    }
}