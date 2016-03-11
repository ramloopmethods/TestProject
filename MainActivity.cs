using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
using Android.Util;
using System.IO;
using SQLite;
using System.Collections.Generic;
using Android.Telephony;
using Android.Provider;


namespace Location
{
	[Activity (Label = "Location", MainLauncher = true)]

	//Implement ILocationListener interface to get location updates
	public class MainActivity : Activity, ILocationListener
	{
		LocationManager locMgr;
		string tag = "MainActivity";
		
		TextView latitude;
		TextView longitude;
		TextView provider;
        Button button;

        public double Meters;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			Log.Debug (tag, "OnCreate called");

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

            SetContentView(Resource.Layout.Main);
            button = FindViewById<Button>(Resource.Id.myButton);
            latitude = FindViewById<TextView>(Resource.Id.latitude);
            longitude = FindViewById<TextView>(Resource.Id.longitude);
            provider = FindViewById<TextView>(Resource.Id.provider);           
            
           

            string ID = Android.OS.Build.Serial;

            Toast.MakeText(this, ID.ToString(), ToastLength.Short).Show();

			
			latitude = FindViewById<TextView> (Resource.Id.latitude);
			longitude = FindViewById<TextView> (Resource.Id.longitude);
			provider = FindViewById<TextView> (Resource.Id.provider);

            CreateDB();
			
		}



		protected override void OnStart ()
		{
			base.OnStart ();
			Log.Debug (tag, "OnStart called");
		}

		// OnResume gets called every time the activity starts, so we'll put our RequestLocationUpdates
		// code here, so that 
		protected override void OnResume ()
		{
			base.OnResume (); 
			Log.Debug (tag, "OnResume called");

			// initialize location manager
			locMgr = GetSystemService (Context.LocationService) as LocationManager;

            if (locMgr.AllProviders.Contains (LocationManager.NetworkProvider)
                    && locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 0, 0, this);
            }
            else
            {
                Toast.MakeText(this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show();
            }



            button.Click += delegate
            {
                button.Text = "Location Service Running";

                // pass in the provider (GPS), 
                // the minimum time between updates (in seconds), 
                // the minimum distance the user needs to move to generate an update (in meters),
                // and an ILocationListener (recall that this class impletents the ILocationListener interface)
                if (locMgr.AllProviders.Contains(LocationManager.NetworkProvider)
                    && locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
                {
                    locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 0, 0, this);
                }
                else
                {
                    Toast.MakeText(this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show();
                }
            };
		}

		protected override void OnPause ()
		{
			base.OnPause ();

			// stop sending location updates when the application goes into the background
			// to learn about updating location in the background, refer to the Backgrounding guide
			// http://docs.xamarin.com/guides/cross-platform/application_fundamentals/backgrounding/


			// RemoveUpdates takes a pending intent - here, we pass the current Activity
			locMgr.RemoveUpdates (this);
			Log.Debug (tag, "Location updates paused because application is entering the background");
		}

		protected override void OnStop ()
		{
			base.OnStop ();
			Log.Debug (tag, "OnStop called");
		}

		public void OnLocationChanged (Android.Locations.Location location)
		{
			Log.Debug (tag, "Location changed");
            			 
			double Lat = location.Latitude;
			double Long = location.Longitude;

            //var telephonyManager = (TelephonyManager)GetSystemService(TelephonyService);
            //var id = telephonyManager.DeviceId;

           var id= Android.OS.Build.Serial;
            //var Path = Android.OS.Environment.ExternalStorageDirectory + "/GPSLocation.DB";

           var documents1 = global::Android.OS.Environment.ExternalStorageDirectory + "/";
            var _pathToDatabase = Path.Combine(documents1, "GPSLocation.db");

            if (File.Exists(_pathToDatabase))
            {
                using (var conn = new SQLite.SQLiteConnection(_pathToDatabase))
                {
                    var cmd = new SQLiteCommand(conn);
                    cmd.CommandText = "INSERT INTO DeviceTable(DeviceID,Lattitude,Longitude,TimeStamp)  VALUES ('" + id.ToString() + "','" + Lat.ToString() + "','" + Long.ToString() + "','" + DateTime.Now.ToString() + "')";
                    cmd.ExecuteNonQuery();

                    using (var db = new SQLiteConnection(_pathToDatabase))
                    {

                        var _Obj = db.Query<DeviceTable>("select DeviceID,Lattitude,Longitude, Max(TimeStamp) as TimeStamp from DeviceTable where DeviceID=?", id.ToString());
                        if(_Obj!=null)
                        {
                        
                            foreach(var R in _Obj)
                            {
                                double Lattitude = Convert.ToDouble(R.Lattitude);
                                double Longitude = Convert.ToDouble(R.Longitude);
                                double Radius = distance(Lattitude, Longitude, 28.5593021, 77.1999493, Convert.ToChar("K"));
                                Meters = Radius * 1000;
                            }
                        }
                        else
                        { Toast.MakeText(this, "Nothing to show", ToastLength.Short).Show(); }
                    }

                    conn.Close();
                }
            }
            else
            {
                Toast.MakeText(this, "DBNotAvailable", ToastLength.Short).Show();
            }
			if (Meters<15)
			{
   	    		var callDialog = new AlertDialog.Builder(this);
				callDialog.SetMessage("You are just "+Math.Round(Meters,1).ToString()+" Meters away from the reception");
				callDialog.SetNegativeButton("Welcome", delegate {                    
                    StartActivity(typeof(LoopMethods));

                });
               // callDialog.SetNegativeButton("Cancel", delegate { });
				callDialog.Show();
                                               
			}
				else
			{Toast.MakeText (this, "Out of Range", ToastLength.Long).Show ();
			
				var callDialog = new AlertDialog.Builder(this);
                callDialog.SetMessage("You are just " + Math.Round(Meters, 1).ToString() + " Meters away from the reception");
				//callDialog.SetNegativeButton("You are not in RANGE", delegate { });
                callDialog.SetNegativeButton("Cancel", delegate { });
				callDialog.Show();
			}

			latitude.Text = "Latitude: " + location.Latitude.ToString();
			longitude.Text = "Longitude: " + location.Longitude.ToString();
			provider.Text = "Provider: " + location.Provider.ToString();

		}


		private double distance(double lat1, double lon1, double lat2, double lon2, char unit) {
			double theta = lon1 - lon2;
			double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
			dist = Math.Acos(dist);
			dist = rad2deg(dist);
			dist = dist * 60 * 1.1515;
			if (unit == 'K') {
				dist = dist * 1.609344;
			} else if (unit == 'N') {
				dist = dist * 0.8684;
			}
			return (dist);
		}

				private double deg2rad(double deg) {
			return (deg * Math.PI / 180.0);
		}

	
		private double rad2deg(double rad) {
			return (rad / Math.PI * 180.0);
		}

		public void OnProviderDisabled (string provider)
		{
			Log.Debug (tag, provider + " disabled by user");
		}
		public void OnProviderEnabled (string provider)
		{
			Log.Debug (tag, provider + " enabled by user");
		}
		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
			Log.Debug (tag, provider + " availability has changed to " + status.ToString());
		}

        void CreateDB()
        {
            string dbName = "GPSLocation.db";
            var _extStorage = global::Android.OS.Environment.ExternalStorageDirectory+"/"+ dbName;
           // var _ExtPath = Path.Combine(_extStorage);
            // Check if your DB has already been extracted.
            if (!File.Exists(_extStorage))
            {
                using (BinaryReader br = new BinaryReader(Assets.Open(dbName)))
                {
                    using (BinaryWriter bw = new BinaryWriter(new FileStream(_extStorage, FileMode.Create)))
                    {
                        byte[] buffer = new byte[2048];
                        int len = 0;
                        while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            bw.Write(buffer, 0, len);
                        }
                    }
                }
            }
        }
	}
}


