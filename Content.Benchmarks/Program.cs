using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Content.Benchmarks
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<ComponentManagerGetAllComponents>();
        }
    }
}
