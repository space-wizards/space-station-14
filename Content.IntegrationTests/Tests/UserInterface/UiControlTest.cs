using System.Linq;
using Content.Client.Chat.UI;
using Content.Client.LateJoin;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.UserInterface;

[TestFixture]
public sealed class UiControlTest
{
    // You should not be adding to this.
    private Type[] _ignored = new Type[]
    {
        typeof(EmotesMenu),
        typeof(LateJoinGui),
    };

    /// <summary>
    /// Tests that all windows can be instantiated successfully.
    /// </summary>
    [Test]
    public async Task TestWindows()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings()
        {
            Connected = true,
        });
        var activator = pair.Client.ResolveDependency<IDynamicTypeFactory>();
        var refManager = pair.Client.ResolveDependency<IReflectionManager>();
        var loader = pair.Client.ResolveDependency<IModLoader>();

        await pair.Client.WaitAssertion(() =>
        {
            foreach (var type in refManager.GetAllChildren(typeof(BaseWindow)))
            {
                if (type.IsAbstract || _ignored.Contains(type))
                    continue;

                if (!loader.IsContentType(type))
                    continue;

                // If it has no empty ctor then skip it instead of figuring out what args it needs.
                var ctor = type.GetConstructor(Type.EmptyTypes);

                if (ctor == null)
                    continue;

                // Don't inject because the control themselves have to do it.
                activator.CreateInstance(type, oneOff: true, inject: false);
            }
        });

        await pair.CleanReturnAsync();
    }
}
