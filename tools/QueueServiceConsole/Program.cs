// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using QuickService.Common;
using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Query;
using System.Threading;
using System.Threading.Tasks;
using QuickService.QueueClient;

namespace QueueServiceConsole
{
    public sealed class Program
    {
        const string c_outputFilename = "TestOutput.csv";
        const string c_ListenerName = "OwinListener";
        const string c_ServiceUri = "fabric:/PriorityQueueSample/PriorityQueueService";

        static Random rnd = new Random(DateTime.Now.Millisecond + DateTime.Now.DayOfYear + DateTime.Now.Second + DateTime.Now.Minute);
        static long _partitionCount = 0;
        static long[] _servicePartitionLowKeys;
        static Uri _serviceUri = new Uri(c_ServiceUri);

        static int _batch = 1;
        static int _threads = 10;
        static int _requests = 1000;
        static int _instanceBytes = 256;
        static int _itemCount = 1000;
        static int _itemSkip = 0;
        static int _itemPartition = 0;

        static void Main(string[] args)
        {
            if (false == ParseCommandLine(args))
            {
                ShowHelp();
                Console.ReadLine();
                return;
            }

            // Initialize variables.
            DiscoverServiceParametersAsync().GetAwaiter().GetResult();
            CancellationTokenSource cts = new CancellationTokenSource();

            do
            {
                int temp;
                TimeSpan duration;
                ReadTest<Item> rt;
                WriteTest<Item> wt;
                Stopwatch sw = new Stopwatch();
                const int MinimumMinutes = 60;

                // If not arguments were specified, go interactive.
                if (0 == args.Length)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nCommands: [C]ount, [D]efaults, [E]mpty, [F]aults, [I]tems, [L]onghaul, pri[O]rities, [P]eek, [R]ead test, R/W [T]est, [W]rite test or e[X]it? ");
                    Console.WriteLine($"\tThreads: {_threads}\tRequests: {_requests}\tBatch: {_batch}\tSize: {_instanceBytes}");
                    Console.ResetColor();

                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    Console.WriteLine();
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.C:
                            GetItemCountAsync().Wait();
                            break;

                        case ConsoleKey.D:
                            Console.WriteLine($"Enter the number of threads. Current value is {_threads}: ");
                            _threads = int.TryParse(Console.ReadLine(), out temp) ? temp : _threads;
                            Console.WriteLine($"Enter the number of requests. Current value is {_requests}: ");
                            _requests = int.TryParse(Console.ReadLine(), out temp) ? temp : _requests;
                            Console.WriteLine($"Enter the size of a batch. Current value is {_batch}: ");
                            _batch = int.TryParse(Console.ReadLine(), out temp) ? temp : _batch;
                            break;

                        case ConsoleKey.E:
                            //EmptyPartition();
                            break;

                        case ConsoleKey.F:
                            Console.WriteLine("[C]haos or [F]ailover test: ");
                            keyInfo = Console.ReadKey();
                            Console.WriteLine($"Enter the test duration in minutes ({MinimumMinutes} minute minimum): ");
                            duration = TimeSpan.FromMinutes(int.Parse(Console.ReadLine()));
                            if (duration > TimeSpan.FromMinutes(MinimumMinutes))
                            {
                                var ft = new FaultTest((keyInfo.Key == ConsoleKey.C) ? FaultTest.TestType.Chaos : FaultTest.TestType.Failover, duration);
                                ft.RunAsync(_serviceUri).GetAwaiter().GetResult();
                            }
                            break;

                        case ConsoleKey.I:
                            GetItems();
                            break;

                        case ConsoleKey.L:
                            Console.WriteLine("Enter the test duration in minutes: ");
                            duration = TimeSpan.FromMinutes(int.Parse(Console.ReadLine()));
                            if (duration > TimeSpan.Zero)
                            {
                                var lh = new Longhaul<Item>(_threads, _requests, _serviceUri, c_ListenerName, CreateRandomInstance);
                                lh.RunAsync(duration, cts.Token).GetAwaiter().GetResult();
                            }
                            break;

                        case ConsoleKey.O:
                            GetPriorityCount();
                            break;

                        case ConsoleKey.P:
                            Peek();
                            break;

                        case ConsoleKey.R:
                            rt = new ReadTest<Item>(_batch, _threads, _requests, c_outputFilename, cts.Token);
                            rt.RunAsync(cts.Token).GetAwaiter().GetResult();
                            rt = null;
                            break;

                        case ConsoleKey.T:
                            rt = new ReadTest<Item>(_batch, _threads, _requests, c_outputFilename, cts.Token);
                            wt = new WriteTest<Item>(_batch, _threads, _requests, c_outputFilename, CreateRandomInstance, cts.Token);
                            Task.WhenAll(rt.RunAsync(cts.Token), wt.RunAsync(cts.Token));
                            break;

                        case ConsoleKey.W:
                            wt = new WriteTest<Item>(_batch, _threads, _requests, c_outputFilename, CreateRandomInstance, cts.Token);
                            wt.RunAsync(cts.Token).GetAwaiter().GetResult();
                            wt = null;
                            break;

                        case ConsoleKey.X:
                            cts.Cancel();
                            return;
                    }
                }
            } while (0 == args.Length);
        }

        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        private static async Task GetItemCountAsync()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(_serviceUri, c_ListenerName);
            long priorityCount = await qc.PriorityCountAsync().ConfigureAwait(false);

            Stopwatch sw = Stopwatch.StartNew();
            Task[] tasks = new Task[priorityCount + 4];
            long itemCount = 0, allQueueCount = 0, leasedCount = 0, expiredCount = 0;
            long itemTimes = 0, allQueueTimes = 0, leasedTimes = 0, expiredTimes = 0;

            tasks[0] = qc.CountAsync(QueueType.ItemQueue).ContinueWith((t) => { itemCount = t.Result; itemTimes = sw.ElapsedMilliseconds; });
            tasks[1] = qc.CountAsync(QueueType.AllQueues).ContinueWith((t) => { allQueueCount = t.Result; allQueueTimes = sw.ElapsedMilliseconds; });
            tasks[2] = qc.CountAsync(QueueType.LeaseQueue).ContinueWith((t) => { leasedCount = t.Result; leasedTimes = sw.ElapsedMilliseconds; });
            tasks[3] = qc.CountAsync(QueueType.ExpiredQueue).ContinueWith((t) => { expiredCount = t.Result; expiredTimes = sw.ElapsedMilliseconds; });

            long[] queues = new long[priorityCount];
            for(int i=0; i < priorityCount; i++)
            {
                var index = i;
                tasks[i + 4] = qc.CountAsync(i).ContinueWith((t) => { queues[index] = t.Result; });
            }

            // Wait for all queue calls to complete.
            Task.WaitAll(tasks);

            Console.WriteLine($"        Item count: {itemCount:N0} in {itemTimes:N0}ms");
            Console.WriteLine($" Leased item count: {leasedCount:N0} in {allQueueTimes:N0}ms");
            Console.WriteLine($"Expired item count: {expiredCount:N0} in {leasedTimes:N0}ms");
            Console.WriteLine($"  All queues count: {allQueueCount:N0} in {expiredTimes:N0}ms");
            for (int i = 0; i < queues.Length; i++)
            {
                Console.WriteLine($"          Queue {i,2:N0}: {queues[i]:N0}");
            }
        }

        /// <summary>
        /// Gets a set of items.
        /// </summary>
        private static void GetItems()
        {
            int temp;

            Console.WriteLine($"Enter the partition number to retrieve items from. Current value is {_itemPartition}: ");
            _itemPartition = int.TryParse(Console.ReadLine(), out temp) ? temp : _itemPartition;
            Console.WriteLine($"Enter the number of items to retrieve. Current value is {_itemCount}: ");
            _itemCount = int.TryParse(Console.ReadLine(), out temp) ? temp : _itemCount;
            Console.WriteLine($"Enter the number of items to skip. Current value is {_itemSkip}: ");
            _itemSkip = int.TryParse(Console.ReadLine(), out temp) ? temp : _itemSkip;

            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(_serviceUri, c_ListenerName);
            var items = qc.GetItemsAsync(_itemPartition, _itemCount, _itemSkip).GetAwaiter().GetResult();
            if (null != items)
            {
                foreach (var item in items)
                {
                    Console.WriteLine(item.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the next item that would be returned from the queue.
        /// </summary>
        private static void Peek()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(_serviceUri, c_ListenerName);
            var item = qc.PeekItemAsync().GetAwaiter().GetResult();
            if (QueueItem<Item>.Default == item)
                Console.WriteLine("No items to retrieve.");
            else
                Console.WriteLine($"{item.ToString()}");
        }

        /// <summary>
        /// Gets the number of priorities.
        /// </summary>
        private static void GetPriorityCount()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(_serviceUri, c_ListenerName);
            long totalCount = qc.PriorityCountAsync().GetAwaiter().GetResult();
            Console.WriteLine($"There are {totalCount} priorities.");
        }

        /// <summary>
        /// Discover the parameters of the running stateful service.
        /// </summary>
        private static async Task DiscoverServiceParametersAsync()
        {
            FabricClient client = new FabricClient();
            ServicePartitionList partitions = await client.QueryManager.GetPartitionListAsync(new Uri(c_ServiceUri)).ConfigureAwait(false);

            _partitionCount = partitions.Count;
            _servicePartitionLowKeys = new long[_partitionCount];

            for (int i = 0; i < partitions.Count; i++)
            {
                Int64RangePartitionInformation pi = (Int64RangePartitionInformation)partitions[i].PartitionInformation;
                _servicePartitionLowKeys[i] = pi.LowKey;
            }
        }

        /// <summary>
        /// Empties a partition of all items.
        /// </summary>
        private static async Task EmptyAsync()
        {
            int partition = 0, temp;

            Console.WriteLine($"Enter the partition number to retrieve items from. Current value is {partition}: ");
            partition = int.TryParse(Console.ReadLine(), out temp) ? temp : partition;

            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(_serviceUri, c_ListenerName);

            int count = 0;

            do
            {
                var items = await qc.GetItemsAsync(_itemPartition, _itemCount, _itemSkip);
                foreach (var item in items)
                {
                    count++;
                    var i = await qc.DeleteItemAsync(item.Key, CancellationToken.None);
                }
            } while (count > 0);

            Console.WriteLine("Items removed from partition.");
        }

        /// <summary>
        /// Creates a random instance of type Item.
        /// </summary>
        /// <returns></returns>
        internal static Item CreateRandomInstance()
        {
            Item item = new Item()
            {
                Added = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(RandomThreadSafe.Instance.NextDouble()),
                Id = Guid.NewGuid(),
                IntegerValue = RandomThreadSafe.Instance.Next(),
                LongIntegerValue = RandomThreadSafe.Instance.Next(),
                Name = $"ItemValue_{RandomThreadSafe.Instance.Next()}",
                Bytes = new byte[_instanceBytes]
            };

            RandomThreadSafe.Instance.NextBytes(item.Bytes);
            return item;
        }

        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        private static bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToUpper())
                {
                    case "-H":
                    case "/H":
                    case "-?":
                    case "/?":
                        ShowHelp();
                        break;

                    default:
                        ShowHelp();
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Shows the command line help.
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("QueueServiceConsole.exe [options]");
            Console.WriteLine("\t-H\tShow this screen.");
        }
    }
}
