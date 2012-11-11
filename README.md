#NHibernate Query Service
##Overview
This repository contains experiments in creating a read only NHibernate OData service. There is a project which explores the latest WCF Data Services implementation and another that looks at the ASP.Net Web API (Fall 2012 Update).

##Setting up the database
There is a SQL 2012 backup of the database in the root folder which requires a login "queryservice".

## ASP.Net Web API Implementation
There are a number of issues to solve out of the box when trying to serve NHibernate objects directly over ASP.Net Web API. 

### "You should be using DTOs", they will say
A lot of people will argue that you should really be serving DTOs instead of your actual model classes over the service boundary. They are normally correct but as this is the query side of a sort of CQRS implementation, my concerns are different. I actually just want to serve queryable data to the client.

###Providing an NHibernate session
To execute any command in Nhibernate the API call must be made in the context of "Session". This session represents the lifetime of the database connection and the first level cache of retrieved objects and their change tracking.

We could just construct our session in our controller action but that would involve some boilerplate repetition. Another issue is that, due to the compositional nature of action filters and hence the Web API OData implementation, we need the session to survive beyond the lifetime of the action method so that it can service the late bound evaluation of LINQ operators like Where and Expand.

To solve this issue the web api sample here uses constructor based dependency injection and Ninject to pass the session to the api controller and dispose of it when the request is complete. using an override of the Dispose method on the controller.

###Serialization of Dynamic Proxies
NHibernate uses dynamic proxy types which are derived versions of model types, generated at runtime. When code tries to evaluate any properties on instances of these types, commands will be issued to the database in order to lazily fetch the data to populate the instance. 

This lazy loading means that any serializer will cause the endless fetching of all data related to an entity regardless of whether it was requested or not.

To solve this issue for the JSON.Net serializer we use a ContractResolver to serialize the dynamic proxy class as if it were its base type. We then use a custom JsonConverter to write null out for any proxy instance or an uninitialized collection, the collection equivalent of the dynamic proxy.

###Processing the $expand OData operator
The $expand operator allows the requester to specify the depth of the data that they want retrieved. This is equivalent to the Include operator in Entity Framework and the NHibernate LINQ equivalent is Fetch. To implement this the sample uses an Action Filter which will run its Executed method after the Web API OData action filters. In this it will apply any expands directives and force the enumeration of the results.

Unfortunately the ASP.Net Web API throws an exception when an $expand querystring parameter is found. This is due to the fact that it has not yet been implemented in the fall update and is not expected till 2013. Therefore we also need to derive our own QueryableAttribute and override the validate method to allow $expand

###Outstanding Issues
* Need to implement odata model and enable $metadata support.
* Need to switch on syscache2 support for sql dependency based cache eviction.

