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
            var errors = new Program().RunValidation();
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

        public Dictionary<string, HashSet<ErrorNode>> RunValidation()
        {
            var server = StartServer();
            server.WaitIdleAsync().Wait();
            var sprotoManager = server.ResolveDependency<IPrototypeManager>();
            var serverErrors = new Dictionary<string, HashSet<ErrorNode>>();
            server.WaitAssertion(() =>
                {
                    serverErrors = sprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                }
            ).Wait();
            server.Stop();

            var client = StartClient();
            client.WaitIdleAsync().Wait();
            var cprotoManager = client.ResolveDependency<IPrototypeManager>();
            var clientErrors = new Dictionary<string, HashSet<ErrorNode>>();
            client.WaitAssertion(() =>
                {
                    clientErrors = cprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                }
            ).Wait();
            client.Stop();

            var allErrors = new Dictionary<string, HashSet<ErrorNode>>();
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
