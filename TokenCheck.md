# Introduction #

When calling listObjects or listDirectory and a large number of results is returned, Atmos may truncate the results and return an x-emc-token. This token can then be used in subsequent request(s) to fetch the remaining results.

In the Atmos 1.4.1 connector, a new ListOptions object was added. You should always create this object and pass it into your listObjects or listDirectory method calls. When the call returns, you should then check to see if a token was returned. If it was, you should then keep calling the same method until the token is null.

For example:

```
        ListOptions options = new ListOptions();
        List<DirectoryEntry> dirList = esu.ListDirectory( dirPath, options );
        while( options.Token != null ) {
        	// Subsequent pages
        	dirList.AddRange( esu.ListDirectory( dirPath, options ) );
        }
```

or

```
        ListOptions options = new ListOptions();
        List<ObjectResult> objects = esu.ListObjects( "listable", options );
        while( options.Token != null ) {
        	// Subsequent pages
        	objects.AddRange( esu.ListObjects( "listable", options) );
        }
```