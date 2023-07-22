using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.YAMLLinter
{
    internal static class Program
    {
        private static async Task<int> Main(string[] _)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var (errors, staticIdErrors) = await RunValidation();

            var count = errors.Count + staticIdErrors.Count;

            if (count == 0)
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

            foreach (var err in staticIdErrors)
            {
                Console.WriteLine(err);
            }

            Console.WriteLine($"{count} errors found in {(int) stopwatch.Elapsed.TotalMilliseconds} ms.");
            return -1;
        }

        private static async Task<(Dictionary<string, HashSet<ErrorNode>>, List<string>)> ValidateClient()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { DummyTicker = true, Disconnected = true });
            var client = pairTracker.Pair.Client;

            var cPrototypeManager = client.ResolveDependency<IPrototypeManager>();
            (Dictionary<string, HashSet<ErrorNode>>, List<string>) clientErrors = default;

            await client.WaitPost(() =>
            {
                clientErrors = cPrototypeManager.ValidateDirectory(new ResPath("/Prototypes"));
            });

            await pairTracker.CleanReturnAsync();

            return clientErrors;
        }

        private static async Task<(Dictionary<string, HashSet<ErrorNode>>, List<string>)> ValidateServer()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { DummyTicker = true, Disconnected = true });
            var server = pairTracker.Pair.Server;

            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            (Dictionary<string, HashSet<ErrorNode>>, List<string>) serverErrors = default;

            await server.WaitPost(() =>
            {
                serverErrors = sPrototypeManager.ValidateDirectory(new ResPath("/Prototypes"));
            });

            await pairTracker.CleanReturnAsync();

            return serverErrors;
        }

        public static async Task<(Dictionary<string, HashSet<ErrorNode>> YamlErrors , List<string> StaticIdErrors)>
            RunValidation()
        {
            var yamlErrors = new Dictionary<string, HashSet<ErrorNode>>();

            var serverErrors = await ValidateServer();
            var clientErrors = await ValidateClient();

            foreach (var (key, val) in serverErrors.Item1)
            {
                // Include all server errors marked as always relevant
                var newErrors = val.Where(n => n.AlwaysRelevant).ToHashSet();

                // We include sometimes-relevant errors if they exist both for the client & server
                if (clientErrors.Item1.TryGetValue(key, out var clientVal))
                    newErrors.UnionWith(val.Intersect(clientVal));

                if (newErrors.Count != 0)
                    yamlErrors[key] = newErrors;
            }

            // Next add any always-relevant client errors.
            foreach (var (key, val) in clientErrors.Item1)
            {
                var newErrors = val.Where(n => n.AlwaysRelevant).ToHashSet();
                if (newErrors.Count == 0)
                    continue;

                if (yamlErrors.TryGetValue(key, out var errors))
                    errors.UnionWith(val.Where(n => n.AlwaysRelevant));
                else
                    yamlErrors[key] = newErrors;
            }

            // This will contain duplicate errors for shared files, but it doesn't really matter.
            var staticIdErrors = serverErrors.Item2.Concat(clientErrors.Item2).ToList();

            return (yamlErrors, staticIdErrors);
        }
    }
}
