using System;
using System.Collections.Generic;
using System.Threading;

namespace Task4_ProducerConsumerMonitor
{
    public class BoundedBuffer<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly int _capacity;
        private readonly object _lockObj = new object();

        public BoundedBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public int Count
        {
            get { lock (_lockObj) return _queue.Count; }
        }

        public void Produce(T item, string producerName)
        {
            lock (_lockObj)
            {
                while (_queue.Count >= _capacity)
                {
                    Console.WriteLine($"  [{producerName}] Буфер ПОВНИЙ ({_queue.Count}/{_capacity}) -> Monitor.Wait()");
                    Monitor.Wait(_lockObj);
                }

                _queue.Enqueue(item);
                Console.WriteLine($"  [{producerName}] Додав: {item} | Заповненість: {_queue.Count}/{_capacity}");
                Monitor.PulseAll(_lockObj);
            }
        }

        public T Consume(string consumerName)
        {
            lock (_lockObj)
            {
                while (_queue.Count == 0)
                {
                    Console.WriteLine($"  [{consumerName}] Буфер ПОРОЖНІЙ (0/{_capacity}) -> Monitor.Wait()");
                    Monitor.Wait(_lockObj);
                }

                T item = _queue.Dequeue();
                Console.WriteLine($"  [{consumerName}] Забрав: {item} | Заповненість: {_queue.Count}/{_capacity}");
                Monitor.PulseAll(_lockObj);
                return item;
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            const int bufferCapacity = 4;
            const int itemsPerProducer = 6;
            const int producerCount = 2;
            const int consumerCount = 2;
            const int totalPlan = itemsPerProducer * producerCount;

            var buffer = new BoundedBuffer<int>(bufferCapacity);
            int producedTotal = 0;
            int consumedTotal = 0;

            Thread[] producers = new Thread[producerCount];
            Thread[] consumers = new Thread[consumerCount];

            for (int p = 0; p < producerCount; p++)
            {
                int id = p + 1;
                producers[p] = new Thread(() =>
                {
                    for (int i = 1; i <= itemsPerProducer; i++)
                    {
                        int value = id * 100 + i;
                        buffer.Produce(value, $"Producer-{id}");
                        Interlocked.Increment(ref producedTotal);
                        Thread.Sleep(40);
                    }
                });
            }

            for (int c = 0; c < consumerCount; c++)
            {
                int id = c + 1;
                int itemsToConsume = totalPlan / consumerCount;
                consumers[c] = new Thread(() =>
                {
                    for (int i = 0; i < itemsToConsume; i++)
                    {
                        buffer.Consume($"Consumer-{id}");
                        Interlocked.Increment(ref consumedTotal);
                        Thread.Sleep(75);
                    }
                });
            }

            foreach (var c in consumers) c.Start();
            foreach (var p in producers) p.Start();

            foreach (var p in producers) p.Join();
            foreach (var c in consumers) c.Join();

            Console.WriteLine($"\nПідсумок: вироблено = {producedTotal}, спожито = {consumedTotal}, залишок у буфері = {buffer.Count}");
        }
    }
}