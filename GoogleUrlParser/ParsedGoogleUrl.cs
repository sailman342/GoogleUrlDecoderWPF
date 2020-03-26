using System;
using System.Collections.Generic;
using System.Web;

namespace GoogleUrlParser
{
    public class ParsedGoogleUrl
    {
        // the constructor will  parse the Google Url and  decode it

        // at each step reached we turn on the corresponding flag !

        public enum StatusFlags {
            Unescaped = 1,
            CenterOk = 2, 
            ZoomOk = 4,
            SatelliteOk = 8,
            RouteExpandedOk = 16,
            MilesOk = 32,
            GoogleUserConnectedOK = 64,
            WaypointsOk = 128,
            InnerMatrixOk = 256,
            WaypointsAndViaPointsOk = 512
        }
        public StatusFlags Status { get; internal set; } = 0;

        public string OriginalGoogleUrl { get; private set; } = "Undefined !";
        public string UnescapedGoogleUrl { get; private set; } = "Undefined !";

        public string[] WaypointsStrings { get; private set; }

        public GeoPoint CenterOfTheMap { get; private set; } = new GeoPoint(-1000, -1000);
        public double ZoomFactor { get; private set; } = 10;

        public bool SatelliteView { get; private set; }
        public bool RouteIsExpanded { get; private set; } // left side pannel route !
        public bool UserConnectedToGoogle { get; private set; }

        // itinerary options
        public bool AvoidHighways { get; private set; } = false;
        public bool AvoidTolls { get; private set; } = false;
        public bool AvoidFerries { get; private set; } = false;

        // user means

        public bool UseCar { get; private set; } = false;
        public bool UseBicycle { get; private set; } = false;
        public bool UseWalk { get; private set; } = false;
        public bool UseTrain { get; private set; } = false;
        public bool UsePlane { get; private set; } = false;

        // distance in miles
        public bool DistanceInMiles { get; private set; } = false;

        // decoded from data fields
        public List<Waypoint> WaypointsAndViapoints { get; private set; } = new List<Waypoint> { };

        // make them acessible for printing/debug purposes !
        public List<DataItem> InnerMatrixTree { get; private set; } = new List<DataItem> { };
        public string DeletedOptionsString { get; private set; } = "";
        public string TestUrl { get; private set; } = "";


        public ParsedGoogleUrl(string GUrlArg)
        {
            OriginalGoogleUrl = GUrlArg;

            // check if URL is encoded then decode, check it is a valid url

            UnescapedGoogleUrl = UnescapeUrl(OriginalGoogleUrl);
            Status = Status | StatusFlags.Unescaped;

            CenterOfTheMap = GetTheCenterOfTheMap(UnescapedGoogleUrl);
            Status = Status | StatusFlags.CenterOk;

            ZoomFactor = GetTheZoomOfTheMap(UnescapedGoogleUrl);
            Status = Status | StatusFlags.ZoomOk;

            SatelliteView = GetSatelliteView(UnescapedGoogleUrl);
            Status = Status | StatusFlags.SatelliteOk;

            RouteIsExpanded = GetRouteIsExpanded(UnescapedGoogleUrl);
            Status = Status | StatusFlags.RouteExpandedOk;

            DistanceInMiles = GetMilesSelected(UnescapedGoogleUrl);
            Status = Status | StatusFlags.MilesOk;

            UserConnectedToGoogle = GetUserConnectedToGoogle(UnescapedGoogleUrl);
            Status = Status | StatusFlags.GoogleUserConnectedOK;

            WaypointsStrings = GetTheWaypoints(UnescapedGoogleUrl);
            Status = Status | StatusFlags.WaypointsOk;

            InnerMatrixTree = GetTheInnerMatrixTree(UnescapedGoogleUrl);
            Status = Status | StatusFlags.InnerMatrixOk;

            WaypointsAndViapoints =  DecodeTheWaypointsAndViapointsFromTheTree(InnerMatrixTree);
            Status = Status | StatusFlags.WaypointsAndViaPointsOk;

            // and build control url
            // note if more than 10 waypoints and viapoints maybe will not work in google !
            TestUrl = BuildTestUrl();
        }

        // PARSING THE URL

        /*
        * 
        *  Some browser returns an escaped sequence ( Chrome for instance ...) unescape the original Url
        * 
        *  before that, make sure the passed url is a direction maps reuqest !
        *  
        */

        private string UnescapeUrl(string urlArg)
        {
            // check this is a Google maps dir request

            if (!urlArg.ToLower().Contains("www.google") || !urlArg.ToLower().Contains("/maps/dir/"))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing www.Google or /maps/dir) !", this);
            }
            if (urlArg.Contains("%"))
            {
                try
                {
                    return HttpUtility.UrlDecode(OriginalGoogleUrl);
                }
                catch (Exception e)
                {
                    throw new GoogleParserException ("GoogleUrl : Unable to parse escaped URL : " + e.Message, this);
                }
            }
            else
            {
                return urlArg;
            }
        }

        /*
         * 
         * The coordinates of center of the map are located after /@ and before next /
         * 
         */

        private GeoPoint GetTheCenterOfTheMap(string urlArg)
        {
            // should have only one @ in url !
            string url = urlArg.Split('@')[1];
            if (String.IsNullOrEmpty(url))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing @) !", this);
            }
            url = url.Split('/')[0];
            if (String.IsNullOrEmpty(url))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing / after the @) !", this);
            }
            var splitUrl = url.Split(',');
            try
            {
                return new GeoPoint(Double.Parse(splitUrl[0]), Double.Parse(splitUrl[1]));
            }
            catch (Exception e)
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (cannot convert center lat/long) !" + e.Message, this);
            }

        }

        /*
         * 
         * The zoom of the map is located after /@ and after the lat,lon /
         * 
         */

        private double GetTheZoomOfTheMap(string urlArg)
        {
            // should have only one @ in url !
            string url = urlArg.Split('@')[1];
            if (String.IsNullOrEmpty(url))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing @) !", this);
            }
            url = url.Split('/')[0];
            if (String.IsNullOrEmpty(url))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing / after the @) !", this);
            }
            var splitUrl = url.Split(',');
            try
            {
                return Double.Parse(splitUrl[2].Replace('z', ' '));
            }
            catch (Exception e)
            {
                // this is not fatal, id user is in satellite mode
                // /@45.5940453,3.59898,511181m
                // where zoom is expressed in view size in meters ?
                if (GetSatelliteView(urlArg))
                {
                    return 10.0;
                }
                else
                {
                    throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request, cannot convert zoom! : " + e.Message, this);
                }
            }
        }

        /*
         * 
         *  if map is viewd in satellite mode it contains  3m with 1e3 at the beginning of the data
         * 
         */

        private bool GetSatelliteView(string urlArg)
        {
            string dataField = GetTheDataField(urlArg);
            //TODO refine this (demux matrix and check)
            return (dataField.IndexOf("3m") == 0 && dataField.Contains("1e3"));
        }


        /*
         * 
         * User expanded the left pannel showing details of the road
         * 
         */

        private bool GetRouteIsExpanded(string urlArg)
        {
            return urlArg.Contains("am=t");
        }

        /*
         * 
         * User selected miles, 4e1, this can be in matrix or at the end of the data
         * but outside the matrix (this will be checked in the big matrix as well)
         * 
         */

        private bool GetMilesSelected(string urlArg)
        {
            return urlArg.Contains("4e1");
        }

        /*
         * 
         * options selected by user are at the beginning of the data in a 3m matrix
         * 
         */

        private bool GetUserConnectedToGoogle(string urlArg)
        {
            string dataField = GetTheDataField(urlArg);
            //TODO refine this (demux matrix and check)
            return (dataField.IndexOf("3m") == 0 && dataField.Contains("4b1"));
        }

        /*
         * 
         * Get the list of the waypoints in the Google map blue window at left, waypoints are either adresses or coordinates
         * They are located after the /dir and before the /@ and separated with /
         * Note: if only one waypoint, second appears as "/ /" in the list
         * 
         */

        private string[] GetTheWaypoints(string urlArg)
        {
            // should have only one @ in url !
            string url = urlArg.Substring(0, urlArg.IndexOf("/@"));
            if (String.IsNullOrEmpty(url))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing @) !", this);
            }
            url = url.Substring(url.IndexOf("/maps/dir/") + 10);
            if (String.IsNullOrEmpty(url))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing @) !", this);
            }
            return url.Split('/');
        }

        /*
        * 
        * After the /data= we may have the options (satellite and user connected to Google in a 3m matrix)
        * see GetUserConnectedToGoogle(url)
        * and see GetSatelliteOption(url)
        *
        * following this we have a 4m matrix we call the big matrix of n items
        * the big matrix contains a single matrix counting n-1 items
        * lets call this matrix the inner matrix
        * 
        * exemple /data=!3m1!4b1!4m22!4m21!....
        * 
        * 4m22 is the big matrix, 4m21 the inner matrix
        *
        */

        private List<DataItem> GetTheInnerMatrixTree(string urlStrArg)
        {
            string dataField = GetTheInnerMatrix(urlStrArg);

            // get rid of language specification at the end of the string
            string urlStr = dataField;

            if (dataField.IndexOf("?hl=") > 0)
            {
                urlStr = dataField.Substring(0, dataField.IndexOf("?hl="));
            }

            string[] splittedUrl = urlStr.Split('!');

            DataItem itm1 = new DataItem(splittedUrl[0]);

            if (splittedUrl.Length - 1 != itm1.MatrixSize)  // -1 because the matrix descriptor is not counted !
            {
                throw new GoogleParserException ("GoogleUrl : Inner matrix size not equal to found dataItems !", this);
            }

            // the result list
            List<DataItem> retVal = new List<DataItem> { };

            int index = 1;
            while (index < itm1.MatrixSize)
            {
                // get one item and point next one !
                DataItem dataItem = new DataItem(splittedUrl[index++]);
                if (dataItem.IsMatrixElement)
                {
                    for (int idx = index; idx < index + dataItem.MatrixSize; idx++)
                    {
                        DataItem dataItemChild = new DataItem(splittedUrl[idx]);
                        // childs can also be matrixes, but this is laset level, no need to iterat further !
                        if (dataItemChild.IsMatrixElement)
                        {
                            for (int idx2 = 1; idx2 < dataItemChild.MatrixSize + 1; idx2++)
                            {
                                dataItemChild.Childs.Add(new DataItem(splittedUrl[idx + idx2]));
                            }
                            idx += dataItemChild.Childs.Count;
                        }
                        dataItem.Childs.Add(dataItemChild);
                    }
                    index += dataItem.MatrixSize;
                }
                retVal.Add(dataItem);
            }

            // remove options at the end of the matrix tree !
            retVal = ParseAndremoveOptionsFromInnerMatrix(retVal);

            return retVal;
        }

        /*
         * 
         * The inner matrix follows the big matrix
         * We check we are well located in the string by comparing their sizes
         * Inner matrix size must be Big matrix size -1
         * 
         */

        private string GetTheInnerMatrix(string urlArg)
        {
            // in fact get rid of the first item
            string innerMatrix = GetTheBigMatrix(urlArg);

            string firstItem = innerMatrix.Split('!')[0];
            // parse the first item
            DataItem itm1 = new DataItem(firstItem);

            string secondItem = innerMatrix.Split('!')[1];
            // parse the second item
            DataItem itm2 = new DataItem(secondItem);

            if (!itm1.IsMatrixElement || !itm2.IsMatrixElement || (itm1.MatrixSize != itm2.MatrixSize + 1))
            {
                throw new GoogleParserException ("GoogleUrl : The big matrix and inner matrix don't fit !", this);
            }
            else
            {
                innerMatrix = innerMatrix.Substring(firstItem.Length + 1); // get rid od the !
                return innerMatrix;
            }
        }

        /*
         * 
         * The data= is followed by an option 3m matrix or it is directly the big matrix
         * 
         */

        private string GetTheBigMatrix(string urlArg)
        {
            string bigMatrix = GetTheDataField(urlArg);

            // are options to be removed in front of big matrix ?

            if (bigMatrix.IndexOf("3m") == 0)
            {
                bigMatrix = bigMatrix.Substring(bigMatrix.IndexOf("4m"));
            }
            return bigMatrix;
        }

        /*
         * 
         * The data field follows the /data= statement in the Url
         * 
         */

        private string GetTheDataField(string urlArg)
        {
            string dataField = urlArg.Substring(urlArg.IndexOf("/data=") + 7);
            if (String.IsNullOrEmpty(dataField))
            {
                throw new GoogleParserException ("GoogleUrl : The format of the Url is Not a valid Google /maps/dir request (missing /data=) !", this);
            }
            return dataField;
        }

        private List<DataItem> ParseAndremoveOptionsFromInnerMatrix(List<DataItem> DataItemsTreeArg)
        {
            // make a copy of the list and return truncated list

            List<DataItem> WaypointsDataItemsTree = GetDataItemsListCopy(DataItemsTreeArg);

            // check if last dataItem is Itinerary option(s)
            //  1b1 is avoid highways
            //  2b1 is avoid tolls
            //  3b1 is avoid ferries

            //  4e1 is distance in miles

            // note 1b0,2b0,3b0, 4e0 are never present !

            //	3e0 car
            //	3e1 bicycle
            //	3e2 walk
            // 	3e3 train
            //	3e4 plane

            // user position in left itinerary on the screen Google Maps
            // 6m3!1i1!2i0!3i2

            bool lastIsToBeDeleted = false;

            do
            {
                lastIsToBeDeleted = false;

                // point to the last element in the top nodes
                int lastDataItemTopNodeIndex = WaypointsDataItemsTree.Count - 1;
                if (lastDataItemTopNodeIndex >= 0)
                {
                    // all coordinates are matrix element, so a top level non matrix is option
                    if (!WaypointsDataItemsTree[lastDataItemTopNodeIndex].IsMatrixElement)
                    {
                        //TODO not all cases seen here have been seen in the past, maybe not all options
                        // can appear as single item or embeeed in matrix !
                        // for the time being duplicate code with matrix just in case it would differ
                        // after heavy testing

                        // we know some options are exclusive, but collect them all tomake sure
                        // we can verify our decoding ( UseTrain and UseCar should not be both true !)

                        switch (WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue)
                        {
                            case "1b1":
                                AvoidHighways = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            case "2b1":
                                AvoidTolls = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            case "3b1":
                                AvoidFerries = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            case "3e0":
                                UseCar = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            case "3e1":
                                UseBicycle = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                break;

                            case "3e2":
                                UseWalk = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            case "3e3":
                                UseTrain = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            case "3e4":
                                UsePlane = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            // usually not part of the inner matrix but follows it!
                            case "4e1":
                                DistanceInMiles = true;
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                                lastIsToBeDeleted = true;
                                break;

                            // we have a problem !
                            default:
                                throw new GoogleParserException ("GoogleUrl : Invalid ItineraryOption in last dataItem", this);
                        }
                    }
                    else
                    {

                        // we have a matrix
                        // if it is 6m3 we can delete it
                        // this is the user position in the itinerary menu 

                        if (WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue == "6m3")
                        {
                            lastIsToBeDeleted = true;
                        }
                        else
                        {
                            // check if we have options in the matrix and delete it
                            // TODO again very likely not all have to be here
                            foreach (DataItem dataItem in WaypointsDataItemsTree[lastDataItemTopNodeIndex].Childs)
                            {
                                switch (dataItem.StringValue)
                                {
                                    case "1b1":
                                        AvoidHighways = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "2b1":
                                        AvoidTolls = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "3b1":
                                        AvoidFerries = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "3e0":
                                        UseCar = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "3e1":
                                        UseBicycle = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "3e2":
                                        UseWalk = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "3e3":
                                        UseTrain = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    case "3e4":
                                        UsePlane = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;

                                    // usually not part of the inner matrix but follows it!
                                    case "4e1":
                                        DistanceInMiles = true;
                                        DeletedOptionsString = "!" + dataItem.StringValue + DeletedOptionsString;
                                        lastIsToBeDeleted = true;
                                        break;
                                }
                            }
                            if (lastIsToBeDeleted)
                            {
                                DeletedOptionsString = "!" + WaypointsDataItemsTree[lastDataItemTopNodeIndex].StringValue + DeletedOptionsString;
                            }
                        }
                    }
                    if (lastIsToBeDeleted)
                    {
                        WaypointsDataItemsTree.RemoveAt(lastDataItemTopNodeIndex);
                    }
                    // as long as we have deleted one item/matreix continue to search options
                }
            } while (lastIsToBeDeleted);

            if (WaypointsDataItemsTree.Count == 0)
            {
                // no data in data field, use waypoints from list !
                throw new GoogleParserException ("Not implementted yet", this);
            }

            return WaypointsDataItemsTree;
        }

        // get a copy of the first level items !
        private List<DataItem> GetDataItemsListCopy(List<DataItem> listArg)
        {
            List<DataItem> retVal = new List<DataItem> { };
            foreach (DataItem itm in listArg)
            {
                retVal.Add(itm);
            }
            return retVal;
        }


        private List<Waypoint> DecodeTheWaypointsAndViapointsFromTheTree(List<DataItem> InnerMatrixTreeArg)
        {
            // the inner matrrix (see GetDataItemsTree for explanation)  can contain only the option fields in case there are no VIA points
            // in this case work is almost done as there are no other waypoints to decode than the waypoints list!

            // in addition to the possible options, the inner matrix can also contain n submatrixes where n is the number of waypoints
            // in which case the (n)th submatrix is the destination only (no via points)
            // the first n-1 submatrixes are then the legs with via points if any

            // if there are no via points in the leg, it can be a simple 1m0 in which case 
            // we have to use the waypoint from the waypoint list (the waypoint list is the first part of the url

            // at the end of the url, the inner matrix can contain a 6m matrix containing the user position in the display list 
            // if he clicked on a point on the itinerary in the left window ...
            // at the end are alos itinerary options items or matrixes
            // and the item miles/kilometers

            // Lets make a first pass and collect level -1 dataitems
            // note from here we abandon the string url to work with a list of DataItems ...
            // please also note we have still matrixes at this level but we don't expand them
            // intentionnaly as we are only concerned by 1m0, 1d and 2d items 

            // 1m0 means no data in data field, take address from waypoints list
            // 2m is Waypoint
            // 3m is via point

            List<Waypoint> retval = new List<Waypoint> { };

            int currentWaypointIndex = 0;

            foreach (DataItem innerMatrixChild in InnerMatrixTreeArg)
            {
                if (innerMatrixChild.IsMatrixElement)
                {
                    // false matrix to replace waypoint from waypoints list !
                    if (innerMatrixChild.StringValue == "1m0")
                    {
                        // we have to take one waypoint from the list of wpts !
                        retval.Add(new Waypoint(true, Double.Parse("0.0"), Double.Parse("0.0"), WaypointsStrings[currentWaypointIndex++]));
                    }
                    else
                    {
                        foreach (DataItem innerMatrixChildChild in innerMatrixChild.Childs)
                        {
                            if (innerMatrixChildChild.IsMatrixElement)
                            {
                                // detected is set when 1d.... is seen :-)
                                bool detected1d = false;
                                string matrixType = innerMatrixChildChild.StringValue.Substring(0, 2);
                                string lon = "";
                                string lat = "";
                                foreach (DataItem subChild in innerMatrixChildChild.Childs)
                                {
                                    if (detected1d)
                                    // 1d was detected on previous child, must be 2d.... now !
                                    {
                                        if (subChild.StringValue.Contains("2d"))
                                        {
                                            lat = subChild.StringValue.Substring(subChild.StringValue.IndexOf("2d") + 2);
                                            if (matrixType == "2m")
                                            {
                                                //  2m matrix is waypoint
                                                if (currentWaypointIndex > WaypointsStrings.Length - 1)
                                                {
                                                    throw new GoogleParserException ("GoogleUrl : Too many waypoints found in data compared to list !", this);
                                                }
                                                retval.Add(new Waypoint(true, Double.Parse(lat), Double.Parse(lon), WaypointsStrings[currentWaypointIndex++]));
                                                detected1d = false; // reset
                                            }
                                            else if (matrixType == "3m")
                                            {
                                                //  3m matrix is viapoint ?
                                                retval.Add(new Waypoint(false, Double.Parse(lat), Double.Parse(lon), ""));
                                                detected1d = false;
                                            }
                                            else
                                            {
                                                throw new GoogleParserException ("GoogleUrl : Geo coordinates not in 2m or 3 matrix !", this);
                                            }
                                        }
                                        else
                                        {
                                            throw new GoogleParserException ("GoogleUrl : Data 1d not followed by 2d !", this);
                                        }
                                    }
                                    else if (subChild.StringValue.Contains("1d"))
                                    {
                                        lon = subChild.StringValue.Substring(subChild.StringValue.IndexOf("1d") + 2);
                                        detected1d = true;
                                    }
                                    else
                                    {
                                        // reset for next couple detection
                                        detected1d = false; //
                                    }

                                }

                            }
                        }
                    }
                }
            }

            if (currentWaypointIndex != WaypointsStrings.Length)  // was ++ in loop so currentWaypointindex is size !
            {
                throw new GoogleParserException($"GoogleUrl : Number of waypoints found in data {currentWaypointIndex} not equal to found in list { WaypointsStrings.Length}", this);
            }

            return retval;
        }


        private string BuildTestUrl()
        {
            string retUrl = "https://www.google.com/maps/dir/";

            // can be empty if no via points declared !
            List<Waypoint> outList = WaypointsAndViapoints;
            if (outList.Count == 0)
            {
                foreach (string wptStr in WaypointsStrings)
                {
                    outList.Add(new Waypoint(true, Double.Parse("0.0"), Double.Parse("0.0"), wptStr));

                }
            }

            foreach (Waypoint wpt in outList)
            {
                if (wpt.IsWaypoint)
                {
                    retUrl += wpt.Address + "/";
                }
                else
                {
                    retUrl += wpt.Latitude.ToString() + "," + wpt.Longitude.ToString() + "/";
                }
            }
            retUrl += "@";
            retUrl += $"{CenterOfTheMap.Latitude},{CenterOfTheMap.Longitude},{ZoomFactor}z";
            string[] splitOptions = DeletedOptionsString.Split('!');
            // start with ! so length is -1 !
            retUrl += "/data=!" + $"4m{splitOptions.Length}!" + $"4m{splitOptions.Length - 1}" + DeletedOptionsString;

            return retUrl;
        }
    }
}
