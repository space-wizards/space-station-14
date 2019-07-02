using Content.Client;
using Content.Client.Interfaces.Parallax;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.UnitTesting;

namespace Content.IntegrationTests
{
    public abstract class ContentIntegrationTest : RobustIntegrationTest
    {
        protected override ClientIntegrationInstance StartClient(ClientIntegrationOptions options = null)
        {
            options = options ?? new ClientIntegrationOptions();
            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
                {
                    ClientBeforeIoC = () =>
                    {
                        IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    }
                });
            };
            return base.StartClient(options);
        }
    }
}
