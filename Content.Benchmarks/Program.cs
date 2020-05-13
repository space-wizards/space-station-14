using BenchmarkDotNet.Running;

namespace Content.Benchmarks
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<StereoToMonoBenchmark>();
        }
    }
}
