using System.Collections.Concurrent;

namespace ThreadedConsumerProducer
{
    internal class Program
    {
        static int storageCapacity = 3;
        static int maxProducersConsumers = 3;

        static int[] productionPlan = [3, 5, 2];
        static int[] consumptionPlan = [3, 3, 4];

        static Semaphore storageAccessSemaphore = new(maxProducersConsumers, maxProducersConsumers);
        static Semaphore storageSpaceSemaphore = new(storageCapacity, storageCapacity);
        static Semaphore productionSemaphore = new(0, storageCapacity);

        static ConcurrentQueue<string> storage = new();

        static void Main(string[] args)
        {
            Thread[] producers = new Thread[productionPlan.Length];
            Thread[] consumers = new Thread[consumptionPlan.Length];

            for (int i = 0; i < productionPlan.Length; i++)
            {
                int producerIndex = i;
                producers[producerIndex] = new Thread(() => Producer(producerIndex, productionPlan[producerIndex]));
                producers[producerIndex].Start();
            }

            for (int i = 0; i < consumptionPlan.Length; i++)
            {
                int consumerIndex = i;
                consumers[consumerIndex] = new Thread(() => Consumer(consumerIndex, consumptionPlan[consumerIndex]));
                consumers[consumerIndex].Start();
            }

            foreach (var producer in producers)
                producer.Join();
            foreach (var consumer in consumers)
                consumer.Join();

            Console.WriteLine("Програму завершено.");
        }

        static void Producer(int id, int productionAmount)
        {
            Random random = new Random();

            for (int i = 0; i < productionAmount; i++)
            {
                storageSpaceSemaphore.WaitOne();
                storageAccessSemaphore.WaitOne();
                Thread.Sleep(random.Next(1000, 3000));

                string product = $"{id}-{i}";
                storage.Enqueue(product);
                Console.WriteLine($"Виробник {id} додав продукт {product}\t Час: {GetTimeMSec()}");

                productionSemaphore.Release();
                storageAccessSemaphore.Release();
            }
        }

        static void Consumer(int id, int consumptionAmount)
        {
            Random random = new Random();

            for (int i = 0; i < consumptionAmount; i++)
            {
                productionSemaphore.WaitOne();
                storageAccessSemaphore.WaitOne();

                if (storage.TryDequeue(out string product))
                {
                    Console.WriteLine($"Споживач {id} взяв продукцію {product}\t Час: {GetTimeMSec()}");
                }
                else
                {
                    Console.WriteLine($"Споживач {id} не знайшов продукції у сховищі\t Час: {GetTimeMSec()}");
                }

                storageSpaceSemaphore.Release();
                storageAccessSemaphore.Release();

                Thread.Sleep(random.Next(1000, 3000));
            }
        }

        private static long initialTime = DateTime.Now.Ticks;
        static long GetTimeMSec()
        {
            return (DateTime.Now.Ticks - initialTime) / TimeSpan.TicksPerMillisecond;
        }
    }
}
