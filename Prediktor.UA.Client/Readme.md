Prediktor.UA.Client
===================

Purpose
-------

The purpose of this assembly is to make connecting to an OPC UA client easier than what it is when using the OPC UA Foundations SDK directly. 
This is done by creating a SessionFactory that handles the creation of the UA session. 
Commonly used methods will be placed in SessionExtensions class as extension methods for the Session.


How to connect
==============

Prerequisites
-------------
There is a set of configuration parameters which must be set in order to connect to a UA Server. These parameters can be set both programmatically or by reading it from a file. 
The latter option is assumed here. 

The file used is an xml file. 
The uaconfig_example.xml in this project is an example of this config file. 
When using this assembly you can copy this file and modify it to your needs.


Anonymous unsecure connection
-----------------------------

An anonymous unsecure connection is not optimal, but can be used for testing purposes. It is by far the easiest way to connect to an OPC UA Server. 
First we need to create an ApplicationConfiguration instance by calling ApplicationConfigurationFactory.LoadFromFile(string file, bool secure). 
The first argument is the file path, and the other argument should be false, since we're not going to create a secure connection. 

After the ApplicationConfiguration has been loaded, create a SessionFactory and call CreateSessionAnonymously(string url, false) with the url to the server.

That's it. Now you can use the Session object and call methods.

**Remember to Dispose session after use**.

Secure connection
-----------------

###Certificate
When creating a secure connection, we need a client certificate. This certificate will be created if it does not exist beforehand. 
The CertGen program in this solution can also be used to create a certificate.

_Note that the certificate needs have the extension fields: Alternate SubjectName and the key DataEncipherment._

The the folder specified in the config file indicates what folder the certificates are stored in:

`<ApplicationCertificate>  `
`  <StoreType>Directory</StoreType>  `
`  <StorePath>testcertificates/pki</StorePath>  `
`  <SubjectName>TestApp</SubjectName>  `
`</ApplicationCertificate>  `

In the example above. The certifcates will be stored in the a subfolder of the working directory. 

The **public** key must be placed in **testcertificates/pki/certs**
The **private** key must be placed in **testcertificates/pki/private**

The public key file must end with the .der extension. It must have a SubjectName indicated in the config file, and it must have an Alternate Subject Name containing the URI of the application.
The URI can be any valid URI, but should indicate something about the client. In addition it needs a Key indicating that it supports DataEncipherment.

###Connecting
First we need to create an ApplicationConfiguration instance by calling ApplicationConfigurationFactory.LoadFromFile(string file, bool secure). 
The first argument is the file path, and the other argument should be true, since we're going to create a secure connection. 

After the ApplicationConfiguration has been loaded, create a SessionFactory and call CreateSession(string endpointURL, IUserIdentity user, bool useSecurity) with the url to the server, the user identity and the last argument shall be true.
If the certifacte is valid, the connection should now be established.


**Remember to Dispose Session after use**.

###ApisHive as OPC UA Server
If ApisHive is the UA Server, the client certificate must be trusted by ApisHive. This is done in ApisBuddy.

Tools -> Configure OPC UA

Select the Hive instance, and click Edit.
Select the Certificate tab, and click Manage Certificates and trust the certificate of your client.

After this the client can be reconnected.

It is also possible to copy the client's public key into ApisHives pki/trusted folder.
