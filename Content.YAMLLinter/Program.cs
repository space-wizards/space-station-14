using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Content.YAMLLinter
{
    internal class Program : ContentIntegrationTest
    {
        private static int Main(string[] args)
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

        private async Task<Dictionary<string, HashSet<ErrorNode>>> RunValidation()
        {
            var allErrors = new Dictionary<string, HashSet<ErrorNode>>();

            var clientErrors = await Task.Run(() => new ClientLinter().ValidateClient());

            foreach (var (key, val) in new ServerLinter().ValidateServer())
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
