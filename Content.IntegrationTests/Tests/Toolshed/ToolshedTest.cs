#nullable enable
using System.Collections.Generic;
using Content.Server.Administration.Managers;
using Robust.Server.Player;
using Robust.Shared.Players;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Toolshed;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class ToolshedTest : IInvocationContext
{
    protected PairTracker PairTracker = default!;

    protected virtual bool NoClient => true;
    protected virtual bool AssertOnUnexpectedError => true;

    protected RobustIntegrationTest.ServerIntegrationInstance Server = default!;
    protected RobustIntegrationTest.ClientIntegrationInstance? Client = null;
    protected ToolshedManager Toolshed = default!;
    protected IAdminManager AdminManager = default!;

    protected IInvocationContext? Context = null;

    [TearDown]
    public async Task TearDownInternal()
    {
        await PairTracker.CleanReturnAsync();
        await TearDown();
    }
    
    protected virtual async Task TearDown()
    {
        Assert.IsEmpty(_expectedErrors);
        ClearErrors();
    }

    [SetUp]
    public virtual async Task Setup()
    {
        PairTracker = await PoolManager.GetServerClient(new PoolSettings {NoClient = NoClient});
        Server = PairTracker.Pair.Server;

        if (!NoClient)
        {
            Client = PairTracker.Pair.Client;
            await Client.WaitIdleAsync();
        }

        await Server.WaitIdleAsync();

        Toolshed = Server.ResolveDependency<ToolshedManager>();
        AdminManager = Server.ResolveDependency<IAdminManager>();
    }

    protected bool InvokeCommand(string command, out object? result, IPlayerSession? session = null)
    {
        return Toolshed.InvokeCommand(this, command, null, out result);
    }

    protected void ParseCommand(string command, Type? inputType = null, Type? expectedType = null, bool once = false)
    {
        var parser = new ForwardParser(command, Toolshed);
        var success = CommandRun.TryParse(false, false, parser, inputType, expectedType, once, out _, out _, out var error);

        if (error is not null)
            ReportError(error);

        if (error is null)
            Assert.That(success, $"Parse failed despite no error being reported. Parsed {command}");
    }

    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        if (Context is not null)
        {
            return Context.CheckInvokable(command, out error);
        }

        error = null;
        return true;
    }

    protected IPlayerSession? InvocationSession { get; set; }

    public ICommonSession? Session
    {
        get
        {
            if (Context is not null)
            {
                return Context.Session;
            }

            return InvocationSession;
        }
    }

    public void WriteLine(string line)
    {
        return;
    }

    private Queue<Type> _expectedErrors = new();

    private List<IConError> _errors = new();

    public void ReportError(IConError err)
    {
        if (_expectedErrors.Count == 0)
        {
            if (AssertOnUnexpectedError)
            {
                Assert.Fail($"Got an error, {err.GetType()}, when none was expected.\n{err.Describe()}");
            }

            goto done;
        }

        var ty = _expectedErrors.Dequeue();

        if (AssertOnUnexpectedError)
        {
            Assert.That(
                    err.GetType().IsAssignableTo(ty),
                    $"The error {err.GetType()} wasn't assignable to the expected type {ty}.\n{err.Describe()}"
                );
        }

        done:
        _errors.Add(err);
    }

    public IEnumerable<IConError> GetErrors()
    {
        return _errors;
    }

    public void ClearErrors()
    {
        _errors.Clear();
    }

    public Dictionary<string, object?> Variables { get; } = new();

    protected void ExpectError(Type err)
    {
        _expectedErrors.Enqueue(err);
    }

    protected void ExpectError<T>()
    {
        _expectedErrors.Enqueue(typeof(T));
    }
}
