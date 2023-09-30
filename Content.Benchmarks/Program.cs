using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Content.IntegrationTests;
using Content.Server.Maps;
#if DEBUG
using BenchmarkDotNet.Configs;
#else
using Robust.Benchmarks.Configs;
#endif
using Robust.Shared.Prototypes;

namespace Content.Benchmarks
{
    internal static class Program
    {

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            PoolManager.Startup(typeof(Program).Assembly);
            var pair = await PoolManager.GetServerClient();
            var gameMaps = pair.Server.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<GameMapPrototype>().ToList();
            MapLoadBenchmark.MapsSource = gameMaps.Select(x => x.ID);
            await pair.CleanReturnAsync();
            PoolManager.Shutdown();

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nWARNING: YOU ARE RUNNING A DEBUG BUILD, USE A RELEASE BUILD FOR AN ACCURATE BENCHMARK");
            Console.WriteLine("THE DEBUG BUILD IS ONLY GOOD FOR FIXING A CRASHING BENCHMARK\n");
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
#else
            var config = Environment.GetEnvironmentVariable("ROBUST_BENCHMARKS_ENABLE_SQL") != null ? DefaultSQLConfig.Instance : null;
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
#endif
        }
    }
}
