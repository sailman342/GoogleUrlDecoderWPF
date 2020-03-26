# Google /maps /dir Url Decoder



## 1. Introduction

When visiting https://www.google.com/maps/dir to edit a road trip, Google stores your inputs (way-points, via-points, options...) in the browser URL.

The URL looks like this 

https://www.google.com/maps/dir/Paris,+France/Bordeaux,+France/@47.3871276,-2.0595992,7z/data=!4m26!4m25!1m15!1m1!1s0x47e66e1f06e2b70f:0x40b82c3688c9460!2m2!1d2.3522219!2d48.856614!3m4!1m2!1d1.5107641!2d47.3019686!3s0x47fb4f97d88435f9:0x4c9d5a49f570de86!3m4!1m2!1d-1.1837439!2d47.3735083!3s0x480603654c811eed:0x378ec1d089229747!1m5!1m1!1s0xd5527e8f751ca81:0x796386037b397a89!2m2!1d-0.57918!2d44.837789!2m1!1b1!3e0?hl=fr

It is sometime useful to recover this information to use it in another utility, like building a gpx file, providing input to your own route planner...

The ParsedGoogleUrl class provided here parses the Google map directions Urls and provides properties to retrieve the stored information.

Please note that the format of the Urls is not documented and that the format was recovered/guessed from numerous tests and trials, the complete logic, if it exists, is not fully understood.

So if you find Urls that will not be correctly decoded, raise an issue and I will have a look on it !

The Visual Studio 2019 solution GoogleUrlDecoderWPF is composed of two projects, the GoogleUrlParser library implementing the ParsedGoogleUrl  class, and, a WPF application demonstrating its use.



## 2. Usage

The ParsedGoogleUrl class has only one constructor with the url string to parse passed as argument.

`

				private ParsedGoogleUrl parsedUrl = null;
				// assume the url is pasted in a text box
				string urlToParse = googleUrlTextBox.Text;
				try
				{
					parsedUrl = new ParsedGoogleUrl(urlToParse);
				}
				catch (GoogleParserException ex)
				{
					resultsTextBox.Text += $"\r\n\r\n Exception raised in GoogleUrl 								constructor : {ex.Message}\r\n\r\n\r\n";
	
					// recover the incomplete parsed url
	
					parsedUrl = (ParsedGoogleUrl)ex.ParsedUrl;
				}
	
				if (parsedUrl != null)
				{
					// make your stuff here
				}
`

In case an error is found, the constructor will raise a GoogleParserException (child class of Exception). The partly parsed url can be recovered from the ParsedUrl property of the exception argument. This can be useful if you don't need the complete parsing to succeed for your purpose (f.i. needing only the way-points and not needing the via points).

The ParsedGoogleUrl  instance has a status property allowing you to detect how far the decoding went and what information are available. If no exception was raised, all information is available and you don't need to check the status before reading the properties.

As an example, here is the code to read the center of the map

					if ((parsedUrl.Status & ParsedGoogleUrl.StatusFlags.CenterOk) == ParsedGoogleUrl.StatusFlags.CenterOk)
					{
						resultsTextBox.Text += $" Center of the map : {parsedUrl.CenterOfTheMap.ToFormattedString()} \r\n\r\n";
					}
					else
					{
						resultsTextBox.Text += $" Center of the map did not succeed \r\n\r\n";
					}
`

## 3. Tips on the Google Url format

Depending on your browser and on the operating system, the url string may need to be un-escaped (%).

The first part of the url (up to the /data=) is easy to decode ans most of the time will not cause any problem.

The first part of the url starts with the way-points addresses or coordinates separated by '/'.

`/Paris,+France/Bordeaux,+France/`



![image-20200326111048100](C:\Users\alain\source\repos\GoogleUrlDecoderWPF\waypoints.png)



Then, following the '@' sign, the coordinates of the center of the map and the zoom factor (here 7)

`@47.3871276,-2.0595992,7z`

There are also some tags to indicate if satellite view is road or terrain, if user expanded the road on the left panel (details), if the user is connected to his Google account ! Reading the code for understanding that is pretty straightforward.

The items following the /data are a bit messy and more complicated to understand.

`data=!4m26...`

The data are items in the format integer-letter-value (4m26) separated by '!'.

The meaning of the first integer is ?

Some of the letters found a sense for me:

- m : matrix element (value = number of elements in matrix)
- b : boolean (value 0 = false, 1= true)
- d : decimal number (value = the latitude,longitude,....)
- ...

In the above example, we have 1b1 a true boolean  = option avoid highways set to true, note that 1b0 is never present ! Other fields are missing :

 Itinerary options : 

-  Avoid HighWays (1b1) : True 
-  Avoid Tolls (2b1) : False 
-  Avoid Ferries (3b1) : False 

 Used mean : 

-  Use Car (3e0) : False 
-  Use Bicycle (3e1) : False 
-  Use Walk (3e2) : False 
-  Use Train (3e3) : False 
-  Use Plane (3e4) : False 

 Distance in Miles (4e1) : False 

So coming back to the data field :

`data=!4m26!4m25!1m15!`

The data is composed of a 26 elements matrix, of which the first element is a 25 elements matrix (we call it the inner matrix). The first element of the inner matrix is a 15 elements matrix and is followed by a 5 element matrix.

Decoding the data gives the following tree, far more easy to understand than a long wording could do :

    1m15 
        1m1 
            1s0x47e66e1f06e2b70f:0x40b82c3688c9460 
        2m2 
            1d2.3522219 
            2d48.856614 
        3m4 
            1m2 
            1d1.5107641 
            2d47.3019686 
            3s0x47fb4f97d88435f9:0x4c9d5a49f570de86 
        3m4 
            1m2 
            1d-1.1837439 
            2d47.3735083 
            3s0x480603654c811eed:0x378ec1d089229747 
    1m5 
        1m1 
            1s0xd5527e8f751ca81:0x796386037b397a89 
        2m2 
            1d-0.57918 
            2d44.837789 


The above case is pretty simple, we have two way-points, therefore two legs (1m15 and 1m5), the first leg has the start point, two via points. The last leg is always the destination point. More complex cases can happen with 1m0 matrices ( if coordinates are known in the way-points list)  ... Please read the code if you need further understanding.

Following this are itinerary  (the 1b1 mentioned above), used mean, units and language options !

The demo program provided decodes the urls 

![](C:\Users\alain\source\repos\GoogleUrlDecoderWPF\decoded.png)

Simply paste the to be decoded url in the text box and hit return ...

The "clear" button clears the data in both windows.

The "Open in Browser" button, opens the default browser with the input url and with the test url provided by the ParsedGoogleUrl  instance property TestUrl. This url is build with all original way-points and via-points in a single way-points list. This allows to visualize if the result fits the input data.