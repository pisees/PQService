# Getting Started with the Service Fabric Priority Queue

## Prerequisites

* Visual Studio 2015 Update 3
* Microsoft Azure Service Fabric SDK and Tools - 2.1.163 

## Visual Studio solution

There are seven projects within the *PriorityQueueSample* solution.

1. **Common** - Contains common components that are used in multiple projects and can be re-used in your projects too
  * **Configuration** - Contains a configuration provider that can make using Service Fabric configuration easier to use
  * **Diagnostics** - Contains the minimal event sources used in the library code and diagnostic classes
  * **ErrorHandling** - Contains a number of retry policy classes
  * **Http** - Http helper classes
  * **Other** helper classes such as Guard, a thread safe Random number generator and base class for a Int64 based stateful Service
2. **PriorityQueueSample** - This is the main Service Fabric project. Your application manifest resides here an you can publish to Azure or local from this project
3. **PriorityQueueService** - Stateful Priority Queue Service
4. **QueueClient** - Client library to facilitate communication with the stateful Service
5. **QueueService** - Base queue service from which other queue can be built. Provides the majority of the priority queue service code
6. **QueueServiceConsole** - Test console built using the queue client library. Shows how to use the library and allows interaction with the priority queue service including load, fault and chaos tests
7. **QueueServiceTests** - Contain various unit and component level tests


# Publish to local cluster, test and run

1. Right click the **PriorityQueueSample** project and choose **Publish...**
2. In the Publish Service Fabric Application dialog, change the target profile to **PublishProfiles\Local.xml**
  * Connection Endpoint will change to *Local Cluster*
  * Application Parameters File will change to *ApplicationParameters\Local.xml*
3. Click **Publish**
4. Open **Service Fabric Explorer** using the **Service Fabric Local Cluster Management** application to watch the application deploy
5. Run tests by opening Test Explorer in Visual Studio and clicking Run All. Some of the tests require that the application is deployed and running locally in order to pass.
6. Run *Queue Service Console* by ensuring the QueueServiceConsole project is set as the start up project and then pressing F5 in Visual Studio. A console window will open
7. In the console window, pressing **'W'** will run a write test adding 1000 items to the queue using 10 threads. When completed you'll see some statistics for the run 