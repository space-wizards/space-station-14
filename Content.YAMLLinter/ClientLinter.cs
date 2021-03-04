using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Tests;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.YAMLLinter
{
    public class ClientLinter : ContentUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        public Task<Dictionary<string, HashSet<ErrorNode>>> ValidateClient()
        {
            BaseSetup();

            var resourceManager = IoCManager.Resolve<IResourceManager>();

            // You will be happier if you skip reading the next two lines of code
            var method = resourceManager.GetType().GetMethod("MountContentDirectory", new[] {typeof(string), typeof(ResourcePath)})!;
            method!.Invoke(resourceManager, new object[] {"../../Resources/", null});

            var cPrototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var clientErrors = new Dictionary<string, HashSet<ErrorNode>>();

            foreach (var (file, errors) in cPrototypeManager.ValidateDirectory(new ResourcePath("/Prototypes")))
            {
                clientErrors.GetOrNew(file).UnionWith(errors);
            }

            return Task.FromResult(clientErrors);
        }
    }
}
