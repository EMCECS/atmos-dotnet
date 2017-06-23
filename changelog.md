# Current Version 2.1.4.31

* Support for UTF-8 encoding of headers
* Support for ViPR object IDs (longer than 44 chars)
* Support for multiple extents and multipart response
* Added feature list to ServiceInformation
* Corresponding tests for all of the above

# Older Version 2.1.0.30

* Added support for new Atmos 2.1 features:
* Anonymous access tokens (upload and download)
* Key-value store (similar to S3 buckets)
* Content-Disposition support for shareable URLs

# Older Version 1.4.1.28

* Fixed buffer handling when performing byterange reads and using a buffer >1MB.

# Older Version 1.4.1.26

* Added ContentType support to UploadHelper
* Issue #5: Using checksum with append operation throws exception
* Issue #6: Add support for custom headers

# Older Version 1.4.1.23

* Fixed Using checksum with append operation throws exception.

# Older Version 1.4.1.20

* Added CalculateServerOffset method and ServerOffset property to EsuRestApi to compensate for clock between the local system and Atmos. See ClockSkew.
* Added LBEsuRestApi class that implements a simple software load balancer. This version simply round-robins requests across an array of Atmos hosts. This will improve performance in a multithreaded environment when a hardware load balancer is not available.
* Fixed ListDirectory with IncludeMetadata=true.

# Older Version 1.4.1.16

* Added Stream functionality: CreateObjectFromStream, CreateObjectFromStreamOnPath, UpdateObjectFromStream, ReadObjectStream
* Added configurable Proxy setting
* Bugfixes:
  * Metadata values can now be null on create/update
  * Issue #1: Timeout and ReadWriteTimeout are now configurable
  * Issue #2: ListDirectory now properly handles WebException
  * Issue #3: Unicode ObjectPaths are now supported (requires Atmos 1.4+)
  * Fixed ACL testcases

# Older Version 1.4.1.10

* New release to support Atmos 1.4.1 Features
* Added new ListDirectory method that takes ListOptions and can return metadata with the entries (new 1.4.1 feature)
* Added new ListObjects call that takes ListOptions for better control over response
* Added new ListOptions feature to control responses and provide proper support for x-emc-limit and x-emc-token. Note: It is strongly advised to switch over to the ListOptions versions to handle large responses without truncation. See Token Check.
* Fixed capitalization of GetShareableUrl
* Added missing EsuApiTest.exe.config

# Previous Version: 1.4.0.9

* Added ObjectPath support to UploadHelper and DownloadHelper

# Older Version: 1.4.0.6

* New release to support Atmos 1.4 Features
* Checksum support on upload and download for erasure coded replicas
* Support for Versioning
* Rename object in namespace
* Get service information (Atmos version)
* Get Object Info (replicas, retention, and deletion info)
* Bugfixes
  * Better support for reading and writing most Unicode characters in metadata
  * Fixed whitespace normalization when computing signatures (metadata with multiple consecutive spaces would fail)
  * UploadHelper can set the object MIME type.

# Older Version: 1.2.5.3 

* Fixed getShareableUrl()
