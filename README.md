# ServiceFabricSignalR
##SignalR persistent connections running on top of Service Fabric

This example show you how to use SignalR with Service Fabric. Both Reliable Actors and Reliable Service are demonstarted.

Service Fabric cluster needs to be configured with 2 node types:

1. Frontend - hosting SignalR service
2. Backend - hosting Reliable Service and Reliable Actors 


In the SignalRSelfHost project modify ApplicationManifest.Xml file: 
  

Use  `<Parameter Name="UseActors" DefaultValue="False" />` if you like to use Reliable Service

  Use  `<Parameter Name="UseActors" DefaultValue="True" />` if you like to use Reliable Actors
  
**For Reliable Services deploy:**

1. ScaleOut
2. SignalRSelfHost
3. EchoTestService

**For Reliable Actors deploy:**

1. ScaleOut
2. SignalRSelfHost
3. EchoTestActor


  
  

  


