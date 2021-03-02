using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Serialization
{
    public class ValidationTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var sprotoManager = server.ResolveDependency<IPrototypeManager>();
            var serverErrors = new HashSet<string>();
            await server.WaitAssertion(() =>
                {
                    var res = sprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                    serverErrors = res.SelectMany(p =>
                        p.Value.Where(n => !n.node.Valid)
                            .SelectMany(n => n.node.Invalids().Select(i => $"{{{n.file}}} => {p.Key} <> {i}").ToList())).ToHashSet();
                }
            );
            server.Stop();

            var client = StartClient();
            await client.WaitIdleAsync();
            var cprotoManager = client.ResolveDependency<IPrototypeManager>();
            var clientErrors = new HashSet<string>();
            await client.WaitAssertion(() =>
                {
                    var res = cprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                    clientErrors = res.SelectMany(p =>
                        p.Value.Where(n => !n.node.Valid)
                            .SelectMany(n => n.node.Invalids().Select(i => $"{{{n.file}}} => {p.Key} <> {i}").ToList())).ToHashSet();
                }
            );

            var actualErrors = clientErrors.Intersect(serverErrors).ToHashSet();
            Assert.Multiple(() =>
            {
                if(actualErrors.Count > 0) Assert.Fail($"Total Errors: {actualErrors.Count}");
                foreach (var actualError in actualErrors)
                {
                    Assert.Fail(actualError);
                }
            });
        }
    }
}
