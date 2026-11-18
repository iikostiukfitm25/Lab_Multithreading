using System;
using System.Diagnostics;
using System.Threading;

namespace Task2_DataRaceAndLock
{
    class Program
    {
        private static int _sharedCounter;
        private static readonly object _syncRoot = new object();

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            const int threadCount = 4;
            const int iterationsPerThread = 2_500_000;
            const int expectedTotal = threadCount * iterationsPerThread;

            Console.WriteLine($"Потоків: {threadCount}, ітерацій на потік: {iterationsPerThread:N0}, очікуваний результат: {expectedTotal:N0}\n");

            // 1. Без синхронізації (Data Race)
            for (int run = 1; run <= 3; run++)
            {
                _sharedCounter = 0;
                long elapsedMs = RunExperiment(threadCount, () =>
                {
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        _sharedCounter++;
                    }
                });
                int lost = expectedTotal - _sharedCounter;
                Console.WriteLine($"[Без lock | Прогін {run}] Факт: {_sharedCounter:N0} | Втрачено: {lost:N0} | Час: {elapsedMs} мс");
            }

            Console.WriteLine();

            // 2. Синхронізація через lock на кожній ітерації
            for (int run = 1; run <= 3; run++)
            {
                _sharedCounter = 0;
                long elapsedMs = RunExperiment(threadCount, () =>
                {
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        lock (_syncRoot)
                        {
                            _sharedCounter++;
                        }
                    }
                });
                Console.WriteLine($"[З lock (кожна ітерація) | Прогін {run}] Факт: {_sharedCounter:N0} | Втрачено: {expectedTotal - _sharedCounter} | Час: {elapsedMs} мс");
            }

            Console.WriteLine();

            // 3. Спостереження: укрупнений lock (раз на 1000 ітерацій)
            _sharedCounter = 0;
            long batchedMs = RunExperiment(threadCount, () =>
            {
                int localSum = 0;
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    localSum++;
                    if (localSum == 1000)
                    {
                        lock (_syncRoot) { _sharedCounter += localSum; }
                        localSum = 0;
                    }
                }
                if (localSum > 0)
                {
                    lock (_syncRoot) { _sharedCounter += localSum; }
                }
            });
            Console.WriteLine($"[З lock (пакетами по 1000)] Факт: {_sharedCounter:N0} | Час: {batchedMs} мс");
        }

        static long RunExperiment(int threadCount, ThreadStart worker)
        {
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
                threads[i] = new Thread(worker);

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }
}