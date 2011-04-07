// Map code borrowed from concept dev https://github.com/conceptdev/Monospace09
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.MapKit;  // required
using MonoTouch.CoreLocation;  // required

namespace MonoTouchCoreLocationExample
{
	public class MapViewController : UIViewController
	{
		private MKMapView mapView;
		public UILabel labelDistance;
		
		public CLLocationCoordinate2D MyLocation;
		private CLLocationManager locationManager;

		private MapFlipViewController _mfvc;
		public MapViewController (MapFlipViewController mfvc):base()
		{
			_mfvc = mfvc;
			MyLocation = new CLLocationCoordinate2D(33.4683,-111.682974); //Hero's Sports Bar, Mesa AZ
		}
		
		public void SetLocation(CLLocationCoordinate2D toLocation)
		{
			Console.WriteLine("SetLocation to {0},{1}", toLocation.Latitude, toLocation.Longitude);
			if (toLocation.Equals(new CLLocationCoordinate2D(0,0)))
			{
				toLocation = locationManager.Location.Coordinate;
			}
			mapView.CenterCoordinate = toLocation;
		}
		
		public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
			// no XIB !
			mapView = new MKMapView()
			{
				ShowsUserLocation = true
			};
			
			labelDistance = new UILabel()
			{
				Frame = new RectangleF (10, 5, 292, 44),
				Lines = 2,
				BackgroundColor = UIColor.Black,
				TextColor = UIColor.White
			};
			
			// Black looks good!
			this.View.BackgroundColor = UIColor.Black;

			var segmentedControl = new UISegmentedControl();
			segmentedControl.Frame = new RectangleF(20, 360, 282,30);
			segmentedControl.InsertSegment("Map", 0, false);
			segmentedControl.InsertSegment("Satellite", 1, false);
			segmentedControl.InsertSegment("Hybrid", 2, false);
			segmentedControl.SelectedSegment = 0;
			segmentedControl.ControlStyle = UISegmentedControlStyle.Bar;
			segmentedControl.TintColor = UIColor.DarkGray;
			
			segmentedControl.ValueChanged += delegate {
				if (segmentedControl.SelectedSegment == 0)
					mapView.MapType = MonoTouch.MapKit.MKMapType.Standard;
				else if (segmentedControl.SelectedSegment == 1)
					mapView.MapType = MonoTouch.MapKit.MKMapType.Satellite;
				else if (segmentedControl.SelectedSegment == 2)
					mapView.MapType = MonoTouch.MapKit.MKMapType.Hybrid;
			};
			
			mapView.Delegate = new MapViewDelegate(this);  // RegionChanged, GetViewForAnnotation 

			// Set the web view to fit the width of the app.
            mapView.SizeToFit();

            // Reposition and resize the receiver
            mapView.Frame = new RectangleF (0, 50, this.View.Frame.Width, this.View.Frame.Height - 50);

			//mapView.SetCenterCoordinate(confLoc, true); 	
			MKCoordinateSpan span = new MKCoordinateSpan(0.2,0.2);
			MKCoordinateRegion region = new MKCoordinateRegion(MyLocation,span);
			mapView.SetRegion(region, true);
			
			MyAnnotation a = new MyAnnotation(MyLocation
                                , "Hero's Sports Bar"
                                , "2855 N Power Rd, Mesa, Arizona"
                              );
			Console.WriteLine("This adds a custom placemark for the Conference Venue");
			mapView.AddAnnotationObject(a); 
			
			
			locationManager = new CLLocationManager();
			locationManager.Delegate = new LocationManagerDelegate(mapView, this);
			locationManager.StartUpdatingLocation();
			
			
            // Add the table view as a subview
            this.View.AddSubview(mapView);
			this.View.AddSubview(labelDistance);
			this.View.AddSubview(segmentedControl);
			
			// Add the 'info' button to flip
			Console.WriteLine("make flip button");
			var flipButton = UIButton.FromType(UIButtonType.InfoLight);
			flipButton.Frame = new RectangleF(290,17,20,20);
			flipButton.Title (UIControlState.Normal);
			flipButton.TouchDown += delegate {
				_mfvc.Flip();
			};
			Console.WriteLine("flipbutton ready to add");
			this.View.AddSubview(flipButton);
		}	
		
		/// <summary>
		/// 
		/// </summary>
		public class MapViewDelegate : MKMapViewDelegate
		{
			private MapViewController _mvc;
			public MapViewDelegate (MapViewController controller):base()
			{
				_mvc = controller;
			}
			/// <summary>
			/// When user moves the map, update lat,long text in label
			/// </summary>
			public override void RegionChanged (MKMapView mapView, bool animated)
			{
				Console.WriteLine("Region did change");
			}
			/// <summary>
			/// Seems to work in the Simulator now
			/// </summary>
			public override MKAnnotationView GetViewForAnnotation (MKMapView mapView, NSObject annotation)
			{
				Console.WriteLine("attempt to get view for MKAnnotation "+annotation);
				try
				{
					var anv = mapView.DequeueReusableAnnotation("thislocation");
					if (anv == null)
					{
						Console.WriteLine("creating new MKAnnotationView");
						var pinanv = new MKPinAnnotationView(annotation, "thislocation");
						pinanv.AnimatesDrop = true;
						pinanv.PinColor = MKPinAnnotationColor.Green;
						pinanv.CanShowCallout = true;
						anv = pinanv;
					}
					else 
					{
						anv.Annotation = annotation;
					}
					return anv;
				} catch (Exception ex)
				{
					Console.WriteLine("GetViewForAnnotation Exception " + ex);
					return null;
				}
			}
		}
		/// <summary>
		/// MonoTouch definition seemed to work without too much trouble
		/// </summary>
		private class LocationManagerDelegate: CLLocationManagerDelegate
		{
			private MKMapView _mapview;
			private MapViewController _appd;
			private int _count = 0;
			public LocationManagerDelegate(MKMapView mapview, MapViewController mapvc):base()
			{
				_mapview = mapview;
				_appd=mapvc;
				Console.WriteLine("Delegate created");
			}
			/// <summary>
			/// Whenever the GPS sends a new location, update text in label
			/// and increment the 'count' of updates AND reset the map to that location 
			/// </summary>
			public override void UpdatedLocation (CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
			{
				//MKCoordinateSpan span = new MKCoordinateSpan(0.2,0.2);
				//MKCoordinateRegion region = new MKCoordinateRegion(newLocation.Coordinate,span);
				//_appd.mylocation = newLocation;
				//_mapview.SetRegion(region, true);
				double distanceToConference = MapHelper.Distance (new Coordinate(_appd.MyLocation), new Coordinate(newLocation.Coordinate), UnitsOfLength.Miles);
				
				_appd.labelDistance.Text = String.Format("{0} miles from Hero's!", Math.Round(distanceToConference,0));
				Console.WriteLine("Distance: {0}", distanceToConference);
				
				//Console.WriteLine("Location updated");
			}
			public override void Failed (CLLocationManager manager, NSError error)
			{
				//_appd.labelInfo.Text = "Failed to find location";
				Console.WriteLine("Failed to find location");
				base.Failed (manager, error);
			}
		}
	}
	
	
	/// <summary>
	/// MKAnnotation is an abstract class (in Objective C I think it's a protocol).
	/// Therefore we must create our own implementation of it. Since all the properties
	/// are read-only, we have to pass them in via a constructor.
	/// </summary>
	public class MyAnnotation : MKAnnotation
	{
		private CLLocationCoordinate2D _coordinate;
		private string _title, _subtitle;
		public override CLLocationCoordinate2D Coordinate {
			get {
				return _coordinate;
			}
			set {
				_coordinate = value;
			}
		}
		public override string Title {
			get {
				return _title;
			}
		}
		public override string Subtitle {
			get {
				return _subtitle;
			}
		}
		/// <summary>
		/// custom constructor
		/// </summary>
		public MyAnnotation (CLLocationCoordinate2D coord, string t, string s) : base()
		{
			_coordinate=coord;
		 	_title=t; 
			_subtitle=s;
		}
	}
}
