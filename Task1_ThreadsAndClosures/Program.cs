using System;
using System.Collections.Generic;
using System.Threading;

namespace Task1_ThreadsAndClosures
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Thread.CurrentThread.Name = "MainThread";

            if (args.Length > 0 && args[0] == "bg")
            {
                RunBackgroundExperiment();
                return;
            }

            Console.WriteLine("=== 1.1. ThreadStart, ParameterizedThreadStart, Name та Join ===");
            Thread t1 = new Thread(new ThreadStart(SimpleWork)) { Name = "Worker-Simple" };
            Thread t2 = new Thread(new ParameterizedThreadStart(ParameterizedWork)) { Name = "Worker-Param" };

            t1.Start();
            t2.Start("Ідентифікатор-А");

            t1.Join();
            t2.Join();
            Console.WriteLine($"[{Thread.CurrentThread.Name}] Усі дочірні потоки завершено штатно.\n");

            Console.WriteLine("=== 1.2. Порівняння помилки замикання та недетермінізму планувальника ===");
            Console.WriteLine("--- Некоректне замикання (захоплення змінної циклу i): ---");
            var badThreads = new List<Thread>();
            for (int i = 0; i < 4; i++)
            {
                var t = new Thread(() => Console.Write($"[Bad i={i}] ")) { Name = $"BadClosure-{i}" };
                badThreads.Add(t);
                t.Start();
            }
            foreach (var t in badThreads) t.Join();
            Console.WriteLine();

            Console.WriteLine("--- Виправлене замикання (локальна копія copy = i): ---");
            var goodThreads = new List<Thread>();
            for (int i = 0; i < 4; i++)
            {
                int copy = i;
                var t = new Thread(() => Console.Write($"[Good copy={copy} ({Thread.CurrentThread.Name})] "))
                {
                    Name = $"Good-{copy}"
                };
                goodThreads.Add(t);
                t.Start();
            }
            foreach (var t in goodThreads) t.Join();
            Console.WriteLine("\n");

            Console.WriteLine("=== 1.3. Пріоритетний потік (IsBackground = false) ===");
            Thread fgThread = new Thread(() =>
            {
                Thread.Sleep(300);
                Console.WriteLine($"[{Thread.CurrentThread.Name}] Пріоритетний потік завершив роботу після Main.");
            })
            { Name = "ForegroundWorker", IsBackground = false };
            fgThread.Start();
            fgThread.Join();
        }

        static void SimpleWork()
        {
            for (int k = 1; k <= 3; k++)
            {
                Console.WriteLine($"  [{Thread.CurrentThread.Name} | ID={Environment.CurrentManagedThreadId}] Крок {k}");
                Thread.Sleep(15);
            }
        }

        static void ParameterizedWork(object? state)
        {
            string label = state as string ?? "Без мітки";
            for (int k = 1; k <= 3; k++)
            {
                Console.WriteLine($"  [{Thread.CurrentThread.Name} | ID={Environment.CurrentManagedThreadId}] {label} -> ітерація {k}");
                Thread.Sleep(15);
            }
        }

        static void RunBackgroundExperiment()
        {
            Thread bgThread = new Thread(() =>
            {
                Console.WriteLine($"[{Thread.CurrentThread.Name}] Фоновий потік стартував (очікування 2 с)...");
                Thread.Sleep(2000);
                Console.WriteLine("Цей рядок НЕ буде виведено, оскільки CLR перерве фоновий потік.");
            })
            { Name = "BackgroundWorker", IsBackground = true };

            bgThread.Start();
            Thread.Sleep(100);
            Console.WriteLine("[MainThread] Головний потік завершується — процес CLR автоматично перерве BackgroundWorker.");
        }
    }
}