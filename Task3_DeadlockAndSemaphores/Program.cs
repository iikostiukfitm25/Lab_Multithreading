using System;
using System.Threading;

namespace Task3_DeadlockAndSemaphores
{
    class Program
    {
        private static readonly object _resourceA = new object();
        private static readonly object _resourceB = new object();

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== 3.1. Демонстрація та виявлення Deadlock (з таймаутом діагностики) ===");
            DemonstrateDeadlockWithTimeout();

            Console.WriteLine("\n=== 3.2. Усунення Deadlock через узгоджений порядок захоплення (A -> B) ===");
            DemonstrateDeadlockPrevention();

            Console.WriteLine("\n=== 3.3. Обмежений пул доступу через SemaphoreSlim (3 слоти, 8 клієнтів) ===");
            DemonstrateSemaphoreSlim();

            Console.WriteLine("\n=== 3.4. Міжпроцесна синхронізація через іменований Mutex ===");
            DemonstrateNamedMutex();
        }

        static void DemonstrateDeadlockWithTimeout()
        {
            Thread t1 = new Thread(() =>
            {
                lock (_resourceA)
                {
                    Console.WriteLine("  [Потік 1] Захопив Resource A, очікує Resource B...");
                    Thread.Sleep(100);
                    if (!Monitor.TryEnter(_resourceB, 1000))
                        Console.WriteLine("  [Потік 1] ТАЙМАУТ! Виявлено взаємне блокування (Deadlock) при очікуванні B.");
                    else
                        Monitor.Exit(_resourceB);
                }
            });

            Thread t2 = new Thread(() =>
            {
                lock (_resourceB)
                {
                    Console.WriteLine("  [Потік 2] Захопив Resource B, очікує Resource A...");
                    Thread.Sleep(100);
                    if (!Monitor.TryEnter(_resourceA, 1000))
                        Console.WriteLine("  [Потік 2] ТАЙМАУТ! Виявлено взаємне блокування (Deadlock) при очікуванні A.");
                    else
                        Monitor.Exit(_resourceA);
                }
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
        }

        static void DemonstrateDeadlockPrevention()
        {
            void SafeWorker(string name)
            {
                lock (_resourceA)
                {
                    Console.WriteLine($"  [{name}] Захопив Resource A");
                    Thread.Sleep(50);
                    lock (_resourceB)
                    {
                        Console.WriteLine($"  [{name}] Захопив Resource B і виконав критичну секцію");
                    }
                }
            }

            Thread t1 = new Thread(() => SafeWorker("Потік 1"));
            Thread t2 = new Thread(() => SafeWorker("Потік 2"));

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            Console.WriteLine("  Обидва потоки успішно завершилися без взаємного блокування.");
        }

        static void DemonstrateSemaphoreSlim()
        {
            const int maxSlots = 3;
            const int totalClients = 8;
            using SemaphoreSlim pool = new SemaphoreSlim(initialCount: maxSlots, maxCount: maxSlots);

            Thread[] clients = new Thread[totalClients];
            for (int i = 1; i <= totalClients; i++)
            {
                int clientId = i;
                clients[i - 1] = new Thread(() =>
                {
                    Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] Клієнт-{clientId}: очікує вільний слот (доступно: {pool.CurrentCount}/{maxSlots})");
                    pool.Wait();
                    try
                    {
                        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] Клієнт-{clientId}: ОТРИМАВ слот (залишилось: {pool.CurrentCount}/{maxSlots})");
                        Thread.Sleep(250);
                    }
                    finally
                    {
                        int prev = pool.Release();
                        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] Клієнт-{clientId}: ЗВІЛЬНИВ слот (стало доступно: {prev + 1}/{maxSlots})");
                    }
                });
                clients[i - 1].Start();
                Thread.Sleep(25);
            }

            foreach (var c in clients) c.Join();
        }

        static void DemonstrateNamedMutex()
        {
            using Mutex appMutex = new Mutex(initiallyOwned: false, name: @"Global\LabMultithreading_SingleInstanceDemo");
            bool acquired = appMutex.WaitOne(TimeSpan.FromMilliseconds(200));
            if (acquired)
            {
                try
                {
                    Console.WriteLine("  Іменований Mutex захоплено: поточний процес є єдиним власником міжпроцесного ресурсу.");
                }
                finally
                {
                    appMutex.ReleaseMutex();
                }
            }
            else
            {
                Console.WriteLine("  Інший екземпляр застосунку вже утримує іменований Mutex.");
            }
        }
    }
}