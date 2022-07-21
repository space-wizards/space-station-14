using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.Shared.CCVar;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.YAMLLinter
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return new Program().Run();
        }

        private int Run()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var errors = RunValidation().Result;

            if (errors.Count == 0)
            {
                Console.WriteLine($"No errors found in {(int) stopwatch.Elapsed.TotalMilliseconds} ms.");
                return 0;
            }

            foreach (var (file, errorHashset) in errors)
            {
                foreach (var errorNode in errorHashset)
                {
                    Console.WriteLine($"::error file={file},line={errorNode.Node.Start.Line},col={errorNode.Node.Start.Column}::{file}({errorNode.Node.Start.Line},{errorNode.Node.Start.Column})  {errorNode.ErrorReason}");
                }
            }

            Console.WriteLine($"{errors.Count} errors found in {(int) stopwatch.Elapsed.TotalMilliseconds} ms.");
            return -1;
        }

        private async Task<Dictionary<string, HashSet<ErrorNode>>> ValidateClient()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{DummyTicker = true, Disconnected = true});
            var client = pairTracker.Pair.Client;

            var cPrototypeManager = client.ResolveDependency<IPrototypeManager>();
            var clientErrors = new Dictionary<string, HashSet<ErrorNode>>();

            await client.WaitPost(() =>
            {
                clientErrors = cPrototypeManager.ValidateDirectory(new ResourcePath("/Prototypes"));
            });

            await pairTracker.CleanReturnAsync();

            return clientErrors;
        }

        private async Task<Dictionary<string, HashSet<ErrorNode>>> ValidateServer()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{DummyTicker = true, Disconnected = true});
            var server = pairTracker.Pair.Server;

            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var serverErrors = new Dictionary<string, HashSet<ErrorNode>>();

            await server.WaitPost(() =>
            {
                serverErrors = sPrototypeManager.ValidateDirectory(new ResourcePath("/Prototypes"));
            });

            await pairTracker.CleanReturnAsync();

            return serverErrors;
        }

        public async Task<Dictionary<string, HashSet<ErrorNode>>> RunValidation()
        {
            var allErrors = new Dictionary<string, HashSet<ErrorNode>>();

            var serverErrors = await ValidateServer();
            var clientErrors = await ValidateClient();

            foreach (var (key, val) in serverErrors)
            {
                var newErrors = val.Where(n => n.AlwaysRelevant).ToHashSet();
                if (clientErrors.TryGetValue(key, out var clientVal))
                {
                    newErrors.UnionWith(val.Intersect(clientVal));
                    newErrors.UnionWith(clientVal.Where(n => n.AlwaysRelevant));
                }

                if (newErrors.Count == 0) continue;
                allErrors[key] = newErrors;
            }

            return allErrors;
        }
    }
}
