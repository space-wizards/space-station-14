using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Shared.Prototypes;
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
                Console.WriteLine($"Found {errors.Count} Error(s)!");
                Console.WriteLine();
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }

                return -1;
            }

            return 0;
        }

        public HashSet<string> RunValidation()
        {
            var server = StartServer();
            server.WaitIdleAsync().Wait();
            var sprotoManager = server.ResolveDependency<IPrototypeManager>();
            var serverErrors = new HashSet<string>();
            server.WaitAssertion(() =>
                {
                    var res = sprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                    serverErrors = res.SelectMany(p =>
                        p.Value.Where(n => !n.node.Valid)
                            .SelectMany(n => n.node.Invalids().Select(i => $"{{{n.file}}} => {p.Key} <> {i}").ToList())).ToHashSet();
                }
            ).Wait();
            server.Stop();

            var client = StartClient();
            client.WaitIdleAsync().Wait();
            var cprotoManager = client.ResolveDependency<IPrototypeManager>();
            var clientErrors = new HashSet<string>();
            client.WaitAssertion(() =>
                {
                    var res = cprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                    clientErrors = res.SelectMany(p =>
                        p.Value.Where(n => !n.node.Valid)
                            .SelectMany(n => n.node.Invalids().Select(i => $"{{{n.file}}} => {p.Key} <> {i}").ToList())).ToHashSet();
                }
            ).Wait();

            return clientErrors.Intersect(serverErrors).ToHashSet();
        }
    }
}
