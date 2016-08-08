# Getting Started with the Service Fabric Priority Queue

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


