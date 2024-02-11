using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.YAMLLinter
{
    internal static class Program
    {
        private static async Task<int> Main(string[] _)
        {
            PoolManager.Startup(null);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var (errors, fieldErrors) = await RunValidation();

            var count = errors.Count + fieldErrors.Count;

            if (count == 0)
            {
                Console.WriteLine($"No errors found in {(int) stopwatch.Elapsed.TotalMilliseconds} ms.");
                PoolManager.Shutdown();
                return 0;
            }

            foreach (var (file, errorHashset) in errors)
            {
                foreach (var errorNode in errorHashset)
                {
                    Console.WriteLine($"::error file={file},line={errorNode.Node.Start.Line},col={errorNode.Node.Start.Column}::{file}({errorNode.Node.Start.Line},{errorNode.Node.Start.Column})  {errorNode.ErrorReason}");
                }
            }

            foreach (var error in fieldErrors)
            {
                Console.WriteLine(error);
            }

            Console.WriteLine($"{count} errors found in {(int) stopwatch.Elapsed.TotalMilliseconds} ms.");
            PoolManager.Shutdown();
            return -1;
        }

        private static async Task<(Dictionary<string, HashSet<ErrorNode>> YamlErrors, List<string> FieldErrors)>
            ValidateClient()
        {
            await using var pair = await PoolManager.GetServerClient();
            var client = pair.Client;
            var result = await ValidateInstance(client);
            await pair.CleanReturnAsync();
            return result;
        }

        private static async Task<(Dictionary<string, HashSet<ErrorNode>> YamlErrors, List<string> FieldErrors)>
            ValidateServer()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var result = await ValidateInstance(server);
            await pair.CleanReturnAsync();
            return result;
        }

        private static async Task<(Dictionary<string, HashSet<ErrorNode>>, List<string>)> ValidateInstance(
            RobustIntegrationTest.IntegrationInstance instance)
        {
            var protoMan = instance.ResolveDependency<IPrototypeManager>();
            Dictionary<string, HashSet<ErrorNode>> yamlErrors = default!;
            List<string> fieldErrors = default!;

            await instance.WaitPost(() =>
            {
                var engineErrors = protoMan.ValidateDirectory(new ResPath("/EnginePrototypes"), out var engPrototypes);
                yamlErrors = protoMan.ValidateDirectory(new ResPath("/Prototypes"), out var prototypes);

                // Merge engine & content prototypes
                foreach (var (kind, instances) in engPrototypes)
                {
                    if (prototypes.TryGetValue(kind, out var existing))
                        existing.UnionWith(instances);
                    else
                        prototypes[kind] = instances;
                }

                foreach (var (kind, set) in engineErrors)
                {
                    if (yamlErrors.TryGetValue(kind, out var existing))
                        existing.UnionWith(set);
                    else
                        yamlErrors[kind] = set;
                }

                fieldErrors = protoMan.ValidateFields(prototypes);
            });

            return (yamlErrors, fieldErrors);
        }

        public static async Task<(Dictionary<string, HashSet<ErrorNode>> YamlErrors , List<string> FieldErrors)>
            RunValidation()
        {
            var yamlErrors = new Dictionary<string, HashSet<ErrorNode>>();

            var serverErrors = await ValidateServer();
            var clientErrors = await ValidateClient();

            foreach (var (key, val) in serverErrors.YamlErrors)
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
            foreach (var (key, val) in clientErrors.YamlErrors)
            {
                var newErrors = val.Where(n => n.AlwaysRelevant).ToHashSet();
                if (newErrors.Count == 0)
                    continue;

                if (yamlErrors.TryGetValue(key, out var errors))
                    errors.UnionWith(val.Where(n => n.AlwaysRelevant));
                else
                    yamlErrors[key] = newErrors;
            }

            // Finally, combine the prototype ID field errors.
            var fieldErrors = serverErrors.FieldErrors
                .Concat(clientErrors.FieldErrors)
                .Distinct()
                .ToList();

            return (yamlErrors, fieldErrors);
        }
    }
}
