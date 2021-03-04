using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;

namespace Content.YAMLLinter
{
    class Program : ContentIntegrationTest
    {
        static int Main(string[] args)
        {
            var errors = new Program().RunValidation().Result;
            if (errors.Count != 0)
            {
                foreach (var (file, errorHashset) in errors)
                {
                    foreach (var errorNode in errorHashset)
                    {
                        Console.WriteLine($"({file} | L:{errorNode.Node.Start.Line}-{errorNode.Node.End.Line}) | C:{errorNode.Node.Start.Column}-{errorNode.Node.End.Column}): {errorNode.ErrorReason}");
                    }
                }

                return -1;
            }

            Console.WriteLine("No errors found!");

            return 0;
        }

        private async Task<Dictionary<string, HashSet<ErrorNode>>> ValidateClient()
        {
            var client = StartClient();

            await client.WaitIdleAsync();

            var cPrototypeManager = client.ResolveDependency<IPrototypeManager>();
            var clientErrors = new Dictionary<string, HashSet<ErrorNode>>();

            await client.WaitAssertion(() =>
            {
                clientErrors = cPrototypeManager.ValidateDirectory(new ResourcePath("/Prototypes"));
            });

            client.Stop();

            return clientErrors;
        }

        private async Task<Dictionary<string, HashSet<ErrorNode>>> ValidateServer()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var serverErrors = new Dictionary<string, HashSet<ErrorNode>>();

            await server.WaitAssertion(() =>
            {
                serverErrors = sPrototypeManager.ValidateDirectory(new ResourcePath("/Prototypes"));
            });

            server.Stop();

            return serverErrors;
        }

        public async Task<Dictionary<string, HashSet<ErrorNode>>> RunValidation()
        {
            var allErrors = new Dictionary<string, HashSet<ErrorNode>>();

            var tasks = await Task.WhenAll(ValidateClient(), ValidateServer());
            var clientErrors = tasks[0];
            var serverErrors = tasks[1];

            foreach (var (key, val) in serverErrors)
            {
                if (clientErrors.TryGetValue(key, out var clientVal))
                {
                    var newErrors = val.Intersect(clientVal).ToHashSet();
                    newErrors.UnionWith(val.Where(n => n.AlwaysRelevant));
                    newErrors.UnionWith(clientVal.Where(n => n.AlwaysRelevant));
                    if (newErrors.Count == 0) continue;

                    allErrors[key] = newErrors;
                }
            }

            return allErrors;
        }
    }
}
