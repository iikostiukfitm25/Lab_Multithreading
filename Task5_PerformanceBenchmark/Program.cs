using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Task5_PerformanceBenchmark
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            int logicalCores = Environment.ProcessorCount;
            const int maxNumber = 1_500_000;
            const int trials = 3;

            Console.WriteLine($"Логічних процесорів (Environment.ProcessorCount): {logicalCores}");
            Console.WriteLine($"Діапазон пошуку простих чисел: [2 .. {maxNumber:N0}], повторів: {trials}\n");

            int checkSeq = CountPrimesRange(2, 100);
            int checkPar = CountPrimesParallel(2, 100, 2);
            Console.WriteLine($"Контрольна перевірка [2..100]: послідовно = {checkSeq}, паралельно = {checkPar}\n");

            long seqMedian = MeasureMedian(trials, () => CountPrimesRange(2, maxNumber), out int expectedPrimes);
            Console.WriteLine($"1 потік (послідовно): знайдено = {expectedPrimes:N0} | Медіана часу = {seqMedian} мс | Прискорення = 1.00x");

            long twoMedian = MeasureMedian(trials, () => CountPrimesParallel(2, maxNumber, 2), out int twoPrimes);
            Console.WriteLine($"2 потоки:             знайдено = {twoPrimes:N0} | Медіана часу = {twoMedian} мс | Прискорення = {(double)seqMedian / twoMedian:F2}x");

            long allMedian = MeasureMedian(trials, () => CountPrimesParallel(2, maxNumber, logicalCores), out int allPrimes);
            Console.WriteLine($"{logicalCores} потоків (усі ядра): знайдено = {allPrimes:N0} | Медіана часу = {allMedian} мс | Прискорення = {(double)seqMedian / allMedian:F2}x");
        }

        static long MeasureMedian(int trials, Func<int> action, out int result)
        {
            long[] times = new long[trials];
            result = 0;
            for (int t = 0; t < trials; t++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                result = action();
                sw.Stop();
                times[t] = sw.ElapsedMilliseconds;
            }
            Array.Sort(times);
            return times[trials / 2];
        }

        static int CountPrimesParallel(int start, int end, int threadCount)
        {
            Thread[] threads = new Thread[threadCount];
            int[] partialCounts = new int[threadCount];
            int totalRange = end - start + 1;
            int chunkSize = totalRange / threadCount;

            for (int t = 0; t < threadCount; t++)
            {
                int idx = t;
                int chunkStart = start + idx * chunkSize;
                int chunkEnd = (idx == threadCount - 1) ? end : chunkStart + chunkSize - 1;

                threads[idx] = new Thread(() =>
                {
                    partialCounts[idx] = CountPrimesRange(chunkStart, chunkEnd);
                });
                threads[idx].Start();
            }

            foreach (var thread in threads) thread.Join();
            return partialCounts.Sum();
        }

        static int CountPrimesRange(int from, int to)
        {
            int count = 0;
            for (int n = from; n <= to; n++)
            {
                if (IsPrime(n)) count++;
            }
            return count;
        }

        static bool IsPrime(int number)
        {
            if (number < 2) return false;
            if (number == 2) return true;
            if ((number & 1) == 0) return false;
            int limit = (int)Math.Sqrt(number);
            for (int d = 3; d <= limit; d += 2)
            {
                if (number % d == 0) return false;
            }
            return true;
        }
    }
}