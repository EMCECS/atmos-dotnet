Requirements
------------

 * .NET 3.5 or higher
 * Visual Studio 2010 or higher
   - Only required for building or running unit tests
 * Sandcastle Help File Builder (http://www.codeplex.com/SHFB)
   - Only required for building help
 
Usage
-----
To use the API, include esuapi.dll as well as the required above libraries to
your project's classpath.  The Debug version is located under bin/Debug and
the release version is located under bin/Release.

In order to use the API, you need to construct an instance of the EsuRestApi
class.  This class contains the parameters used to connect to the server.

EsuApi esu = new EsuRestApi( "host", port, "uid", "shared secret" );

Where host is the hostname or IP address of a ESU node that you're authorized
to access, port is the IP port number used to connect to the server (generally
80 for HTTP), UID is the username to connect as, and the shared secret is the
shared secret key assigned to the UID you're using.  The UID and shared secret
are available from your ESU tennant administrator.  The secret key should be
a base-64 encoded string as shown in the tennant administration console, e.g
"jINDh7tV/jkry7o9D+YmauupIQk=".

After you have created your EsuRestApi object, you can use the methods on the
object to manipulate data in the cloud.  For instance, to create a new, empty
object in the cloud, you can simply call:

ObjectId id = esu.createObject( null, null, null, null );

The createObject method will return an ObjectId you can use in subsequent calls
to modify the object.

The helper classes provide some basic functionality when working with ESU like
uploading a file to the cloud.  To create a helper, simply construct the
appropriate class (UploadHelper or DownloadHelper).  The first, required 
argument is your EsuRestApi object.  The second argument is optional and defines
the transfer size used for requests.  By default, your file will be uploaded
to the server in 4MB chunks.  After constructing the helper object, there are
a couple ways to upload and download objects.  You can either give the helper
a file to transfer or a stream.  When passing a stream, you can optionally pass
an extra argument telling the helper whether you want the descriptor closed 
after the transfer has completed.

UploadHelper helper = new UploadHelper( esu, null );
ObjectId id = helper.createObjectFromFile( new File( "readme.txt" ) );

The helper classes also allow you to register listener classes that implement
the ProgressListener interface.  If you register listeners, they will be 
notified of transfer progress, when the transfer completes, or when an error 
occurs.  You can also access the same status information through the helper 
object's methods.

Note that since a transfer's status is directly connected to the helper class,
the helper class should not be used for more than one transfer.  Doing so can
produce undesired results.

.NET and "100-Continue"
-----------------------

By default, when posting data .Net will not post a body on the first 
request and tell the server it expects "100 Continue".  After getting a
"100 Continue" response it then posts the data.  This is done to avoid sending
the body when redirects or errors occur but can be detrimental to
performance since more round trips are required.

If you know your code will be connecting directly to an unmodified ESU
server, you may see some performance increases by turning off this 
functionality. To do so, call Set100Continue( false ) on your EsuRestApi 
object.  If you are connecting to ESU through a proxy or firewall that may 
send redirects, you should leave this functionality enabled. 

Source Code
-----------

The source code is broken into two namespaces.

 * EsuApiLib - This namespace contains the core objects used in the
   API (Metadata, Acl, Extent, etc) as well as the generic EsuApi interface.
 * EsuApiLib.Rest - This package contains the REST implementation of
   the EsuApi interface.

Logging
-------

The API uses the standard .NET Debug class for logging debugging information.
If you use the Debug version of the .dll these messages should display on 
your debug console.


Help
----

All of the source code is documented using the standard .Net XML documentation
format.  You can regenerate the help using Microsoft's Sandcastle.  Since
Sandcastle is difficult to use by itself, the Sandcastle Help File Builder is
recommended: http://www.codeplex.com/SHFB.  See docgen.shfb for the Sandcastle
Help File Builder project file.


