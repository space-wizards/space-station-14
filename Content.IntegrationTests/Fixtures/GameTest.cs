#nullable enable
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.IntegrationTests.Pair;
using Content.IntegrationTests.Utility;
using NUnit.Framework.Interfaces;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Fixtures;

/// <summary>
/// <para>
///     A test fixture with an integrated <see cref="GameTest.Pair">test pair</see>,
///     proxy methods for efficient test writing, utilities for ensuring tests clean up correctly,
///     and dependency injection
///     (<see cref="SystemAttribute"/> and <see cref="SidedDependencyAttribute"/>).
/// </para>
/// <para>
///     Tests using GameTest support some additional class and method level attributes, namely
///     <see cref="RunOnSideAttribute"/>.
///     Attributes can be used to control how the test runs.
/// </para>
/// </summary>
/// <seealso cref="CompConstraintExtensions"/>
/// <seealso cref="LifeStageConstraintExtensions"/>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Property(TestProperties.TestFrameKind, nameof(GameTest))]
public abstract partial class GameTest
{
    /// <summary>
    ///     Set if the test manually marks itself dirty.
    /// </summary>
    private bool _pairDestroyed;

    /// <summary>
    ///     Tests-testing-tests assistant to run right before the pair is returned.
    /// </summary>
    public event Action? PreFinalizeHook;

    /// <summary>
    ///     The main thread of the game server.
    /// </summary>
    public Thread ServerThread { get; private set; } = null!; // NULLABILITY: This is always set during test setup.
    /// <summary>
    ///     The main thread of the game client.
    /// </summary>
    public Thread ClientThread { get; private set; } = null!; // NULLABILITY: This is always set during test setup.

    /// <summary>
    ///     Settings for the client/server pair.
    ///     By default, this gets you a client and server that have connected together.
    /// </summary>
    /// <remarks>
    ///     Always return a new instance whenever this is read. In other words, no backing field please. Arrow syntax only.
    /// </remarks>
    public virtual PoolSettings PoolSettings => new() { Connected = true };

    /// <summary>
    ///     The client and server pair.
    /// </summary>
    public TestPair Pair { get; private set; } = default!; // NULLABILITY: This is always set during test setup.

    /// <summary>
    ///     The game server instance.
    /// </summary>
    public RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;

    /// <summary>
    ///     The game client instance.
    /// </summary>
    public RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;

    /// <summary>
    ///     The test player's server session, if any.
    /// </summary>
    public ICommonSession? ServerSession => Pair.Player;

    /// <summary>
    ///     The server-side entity manager.
    /// </summary>
    [SidedDependency(Side.Server)]
    public IEntityManager SEntMan = null!;

    /// <summary>
    ///     The client-side entity manager.
    /// </summary>
    [SidedDependency(Side.Client)]
    public IEntityManager CEntMan = null!;

    /// <summary>
    ///     The server-side prototype manager.
    /// </summary>
    [SidedDependency(Side.Server)]
    public IPrototypeManager SProtoMan = null!;

    /// <summary>
    ///     The client-side prototype manager.
    /// </summary>
    [SidedDependency(Side.Client)]
    public IPrototypeManager CProtoMan = null!;

    /// <summary>
    ///     The server-side game-timing manager.
    /// </summary>
    [SidedDependency(Side.Server)]
    public IGameTiming SGameTiming = null!;

    /// <summary>
    ///     The client-side game-timing manager.
    /// </summary>
    [SidedDependency(Side.Client)]
    public IClientGameTiming CGameTiming = null!;

    /// <summary>
    ///     The test map we're using, if any.
    /// </summary>
    public TestMapData? TestMap => Pair.TestMap;

    /// <summary>
    ///     Primary setup task for the fixture.
    ///     Custom setup must run after this.
    /// </summary>
    [SetUp]
    public virtual async Task DoSetup()
    {
        _pairDestroyed = false;
        var testContext = TestContext.CurrentContext;


        var test = testContext.Test;

        var settings = PoolSettings;

        var pairAttribs = test.Method!.GetCustomAttributes<IGameTestPairConfigModifier>(false);
        var pairSuiteAttribs = test.Method!.TypeInfo.GetCustomAttributes<IGameTestPairConfigModifier>(true);

        if (pairAttribs.Length > 1 && pairAttribs.Any(x => x.Exclusive))
        {
            throw new InvalidOperationException(
                "More than one exclusive pair config attribute is present on the test member.");
        }

        if (pairSuiteAttribs.Length > 1 && pairSuiteAttribs.Any(x => x.Exclusive))
        {
            throw new InvalidOperationException(
                "More than one exclusive pair config attribute is present on the test fixture.");
        }

        foreach (var attribute in pairSuiteAttribs.Concat(pairAttribs))
        {
            attribute.ApplyToPairSettings(this, ref settings);
        }

        Pair = await PoolManager.GetServerClient(settings, new NUnitTestContextWrap(testContext, TestContext.Out));

        Task.WaitAll(
            Server.WaitPost(() => { ServerThread = Thread.CurrentThread; }),
            Client.WaitPost(() => ClientThread = Thread.CurrentThread)
        );

        await Pair.ReallyBeIdle(5); // Arbitrary setup time wait.

        InjectDependencies(this);

        var attribs = test.Method!.GetCustomAttributes<IGameTestModifier>(false);
        var suiteAttribs = test.Method!.TypeInfo.GetCustomAttributes<IGameTestModifier>(true);

        foreach (var attribute in suiteAttribs.Concat(attribs))
        {
            await attribute.ApplyToTest(this);
        }

        await DoPreTestOverrides();

        await Pair.RunUntilSynced();
    }

    /// <summary>
    ///     Injects <see cref="SidedDependencyAttribute"/> and <see cref="SystemAttribute"/> dependencies into the
    ///     target object.
    /// </summary>
    /// <remarks>
    ///     This is called on the GameTest itself automatically. Don't call it twice on the same object.
    /// </remarks>
    /// <param name="target">The object to inject into.</param>
    public void InjectDependencies(object target)
    {
        foreach (var field in target.GetType().GetAllFields())
        {
            if (field.GetCustomAttribute<SidedDependencyAttribute>() is { } depAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (depAttrib.Side is Side.Server)
                {
                    field.SetValue(target, Server.EntMan.EntitySysManager.DependencyCollection.ResolveType(field.FieldType));
                }
                else
                {
                    // Must be initially connected for this...
                    if (Client.Session is not null)
                        field.SetValue(target, Client.EntMan.EntitySysManager.DependencyCollection.ResolveType(field.FieldType));
                    else
                        field.SetValue(target, Client.InstanceDependencyCollection.ResolveType(field.FieldType));
                }
            }
        }
    }

    /// <summary>
    ///     Primary teardown task for the fixture.
    ///     Custom teardown must run before this.
    /// </summary>
    [TearDown]
    public virtual async Task DoTeardown()
    {
        try
        {
            // In some cool future we might be able to make this only throw out the pair
            // if the test threw exceptions. But that'd require fixing all of them to do cleanup properly.
            //
            // So not yet.
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                _pairDestroyed = true; // Blow it up, we failed and it might be screwed.
                return;
            }

            // Roll forward til sync for teardown.
            await Pair.RunUntilSynced();

            await CleanUpEntities();

            // And other teardown logic will go here. Eventually.

        }
        catch (Exception)
        {
            _pairDestroyed = true;
            throw;
        }
        finally
        {
            PreFinalizeHook?.Invoke();

            if (!_pairDestroyed)
                await Pair.CleanReturnAsync();
            else
                await Pair.DisposeAsync();
        }
    }
}
