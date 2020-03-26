using GoogleUrlParser;
using System.Windows;
using System.Windows.Input;

namespace GoogleUrlDecoderWPF
{
	/// <summary>
	/// Logique d'interaction pour MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ParsedGoogleUrl parsedUrl = null;

		public MainWindow()
		{
			InitializeComponent();

			resultsTextBox.Text = "";
			googleUrlTextBox.Text = "";
		}

		private void clearButtonClick(object sender, RoutedEventArgs e)
		{
			resultsTextBox.Text = "";
			googleUrlTextBox.Text = "";
		}

		private void googleUrlTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				string urlToParse = googleUrlTextBox.Text;
				resultsTextBox.Text = "";

				try
				{
					parsedUrl = new ParsedGoogleUrl(urlToParse);
				}
				catch (GoogleParserException ex)
				{
					resultsTextBox.Text += $"\r\n\r\n Exception raised in GoogleUrl constructor : {ex.Message}\r\n\r\n\r\n";

					// recover the incomplete parsed url

					parsedUrl = (ParsedGoogleUrl)ex.ParsedUrl;
				}

				if (parsedUrl != null)
				{
					resultsTextBox.Text += $" Original Google URL : {parsedUrl.OriginalGoogleUrl} \r\n\r\n";

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.Unescaped) == ParsedGoogleUrl.StatusFlags.Unescaped)
					{
						resultsTextBox.Text += $" Unescaped Google URL : {parsedUrl.UnescapedGoogleUrl} \r\n\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Unescape Url did not succeed \r\n\r\n";
					}

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.CenterOk) == ParsedGoogleUrl.StatusFlags.CenterOk)
					{
						resultsTextBox.Text += $" Center of the map : {parsedUrl.CenterOfTheMap.ToFormattedString()} \r\n\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Center of the map did not succeed \r\n\r\n";
					}

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.ZoomOk) == ParsedGoogleUrl.StatusFlags.ZoomOk)
					{
						resultsTextBox.Text += $" Zoom of the map : {parsedUrl.ZoomFactor.ToString()} \r\n\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Zoom of the map did not succeed \r\n\r\n";
					}

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.SatelliteOk) == ParsedGoogleUrl.StatusFlags.SatelliteOk)
					{
						resultsTextBox.Text += $" Satellite view (1e3)  : {parsedUrl.SatelliteView.ToString()}";
						if (parsedUrl.SatelliteView)
						{
							resultsTextBox.Text += " -> (zoom set to 10)  \r\n\r\n";
						}
						else
						{
							resultsTextBox.Text += "\r\n\r\n";
						}
					}
					else
					{
						resultsTextBox.Text += $" Satellite view did not succeed \r\n\r\n";
					}


					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.RouteExpandedOk) == ParsedGoogleUrl.StatusFlags.RouteExpandedOk)
					{
						resultsTextBox.Text += $" Route is expanded on left pannel (am=t) : {parsedUrl.RouteIsExpanded.ToString()} \r\n\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Route expanded did not succeed \r\n\r\n";
					}

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.GoogleUserConnectedOK) == ParsedGoogleUrl.StatusFlags.GoogleUserConnectedOK)
					{
						resultsTextBox.Text += $" User connected to Google (4b1) : {parsedUrl.UserConnectedToGoogle.ToString()} \r\n\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Google user connected did not succeed \r\n\r\n";
					}


					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.WaypointsOk) == ParsedGoogleUrl.StatusFlags.WaypointsOk)
					{
						resultsTextBox.Text += $" Waypoints string list, between double quotes to show empty strings ({parsedUrl.WaypointsStrings.Length}) :  \r\n";
						foreach (string str in parsedUrl.WaypointsStrings)
						{
							resultsTextBox.Text += $"      Waypoint :  \"{str}\" \r\n";
						}
						resultsTextBox.Text += $"\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Waypoints did not succeed \r\n\r\n";
					}

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.InnerMatrixOk) == ParsedGoogleUrl.StatusFlags.InnerMatrixOk)
					{
						resultsTextBox.Text += $"Inner Matrix content without options ({parsedUrl.InnerMatrixTree.Count}): \r\n";
						foreach (DataItem dataItem in parsedUrl.InnerMatrixTree)
						{
							resultsTextBox.Text += $"    {dataItem.StringValue} \r\n";
							if (dataItem.IsMatrixElement)
							{
								foreach (DataItem dataItemChild in dataItem.Childs)
								{
									resultsTextBox.Text += $"        {dataItemChild.StringValue} \r\n";
									if (dataItemChild.IsMatrixElement)
									{
										foreach (DataItem subChild in dataItemChild.Childs)
										{
											resultsTextBox.Text += $"            {subChild.StringValue} \r\n";
										}
									}
								}
							}
						}
						resultsTextBox.Text += $"\r\n";

						resultsTextBox.Text += $" Itinerary options : \r\n";
						resultsTextBox.Text += $" Avoid HighWays (1b1) : {parsedUrl.AvoidHighways.ToString()} \r\n";
						resultsTextBox.Text += $" Avoid Tolls (2b1) : {parsedUrl.AvoidTolls.ToString()} \r\n";
						resultsTextBox.Text += $" Avoid Ferries (3b1) : {parsedUrl.AvoidFerries.ToString()} \r\n\r\n";

						resultsTextBox.Text += $" Used mean : \r\n";
						resultsTextBox.Text += $" Use Car (3e0) : {parsedUrl.UseCar.ToString()} \r\n";
						resultsTextBox.Text += $" Use Bicycle (3e1) : {parsedUrl.UseBicycle.ToString()} \r\n";
						resultsTextBox.Text += $" Use Walk (3e2) : {parsedUrl.UseWalk.ToString()} \r\n";
						resultsTextBox.Text += $" Use Train (3e3) : {parsedUrl.UseTrain.ToString()} \r\n";
						resultsTextBox.Text += $" Use Plane (3e4) : {parsedUrl.UsePlane.ToString()} \r\n\r\n";

						resultsTextBox.Text += $" Distance in Miles (4e1) : {parsedUrl.DistanceInMiles.ToString()} \r\n\r\n";

					}
					else
					{
						resultsTextBox.Text += $" Inner matrix did not succeed \r\n\r\n";
					}




					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.WaypointsAndViaPointsOk) == ParsedGoogleUrl.StatusFlags.WaypointsAndViaPointsOk)
					{
						resultsTextBox.Text += $"List of Waypoints and Viapoints \r\n";

						if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.WaypointsAndViaPointsOk) == ParsedGoogleUrl.StatusFlags.WaypointsAndViaPointsOk)
						{
							foreach (Waypoint wpt in parsedUrl.WaypointsAndViapoints)
							{
								string tmpStr = "ViaPoint  ";
								if (wpt.IsWaypoint)
								{
									tmpStr = "WayPoint";
								}

								resultsTextBox.Text += $"  {tmpStr} :  IsWaypoint : {wpt.IsWaypoint} , Lat : {wpt.Latitude} , Lon : {wpt.Longitude} , Addr : {wpt.Address} \r\n";
							}

							resultsTextBox.Text += $"\r\n\r\n";

							resultsTextBox.Text += $"Google Url build with all waypoints and via points in the waypoints list (may exeed the Google 10 wpts limit !)\r\n";
							resultsTextBox.Text += $"{parsedUrl.TestUrl}\r\n";
						}
						else
						{
							resultsTextBox.Text += $"Parsing of Waypoints and Via points was not successfull !\r\n";
						}
						resultsTextBox.Text += $"\r\n\r\n";
					}
				}
			}
		}

		private void openInBrowserButton_Click(object sender, RoutedEventArgs e)
		{
			if (googleUrlTextBox.Text != "")
			{
				System.Diagnostics.Process.Start(googleUrlTextBox.Text);
			}

			if ((parsedUrl != null)  && ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.WaypointsAndViaPointsOk) == ParsedGoogleUrl.StatusFlags.WaypointsAndViaPointsOk))
			{
				System.Diagnostics.Process.Start(parsedUrl.TestUrl);
			}
		}
	}
}
