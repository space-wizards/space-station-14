using System.Collections.Generic;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.YAMLLinter
{
    public class ServerLinter : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Server;

        public IEnumerable<KeyValuePair<string, HashSet<ErrorNode>>> ValidateServer()
        {
            var resourceManager = IoCManager.Resolve<IResourceManager>();
            var sPrototypeManager = IoCManager.Resolve<IPrototypeManager>();

            var method = resourceManager.GetType().GetMethod("MountContentDirectory", new[] {typeof(string), typeof(ResourcePath)})!;
            method!.Invoke(resourceManager, new object[] {"../../Resources/", null});

            return sPrototypeManager.ValidateDirectory(new ResourcePath("/Prototypes"));
        }
    }
}
