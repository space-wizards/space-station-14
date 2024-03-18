#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Pair;
using Content.Server.Administration.Managers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Toolshed;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class ToolshedTest : IInvocationContext
{
    protected TestPair Pair = default!;

    protected virtual bool Connected => false;
    protected virtual bool AssertOnUnexpectedError => true;

    protected RobustIntegrationTest.ServerIntegrationInstance Server = default!;
    protected RobustIntegrationTest.ClientIntegrationInstance? Client = null;
    public ToolshedManager Toolshed { get; private set; } = default!;
    public ToolshedEnvironment Environment => Toolshed.DefaultEnvironment;

    protected IAdminManager AdminManager = default!;

    protected IInvocationContext? InvocationContext = null;

    [TearDown]
    public async Task TearDownInternal()
    {
        await Pair.CleanReturnAsync();
        await TearDown();
    }

    protected virtual async Task TearDown()
    {
        Assert.That(_expectedErrors, Is.Empty);
        ClearErrors();
    }

    [SetUp]
    public virtual async Task Setup()
    {
        Pair = await PoolManager.GetServerClient(new PoolSettings {Connected = Connected});
        Server = Pair.Server;

        if (Connected)
        {
            Client = Pair.Client;
            await Client.WaitIdleAsync();
        }

        await Server.WaitIdleAsync();

        Toolshed = Server.ResolveDependency<ToolshedManager>();
        AdminManager = Server.ResolveDependency<IAdminManager>();
    }

    protected bool InvokeCommand(string command, out object? result, ICommonSession? session = null)
    {
        return Toolshed.InvokeCommand(this, command, null, out result);
    }

    protected T InvokeCommand<T>(string command)
    {
        InvokeCommand(command, out var res);
        Assert.That(res, Is.AssignableTo<T>());
        return (T) res!;
    }

    protected void ParseCommand(string command, Type? inputType = null, Type? expectedType = null, bool once = false)
    {
        var parser = new ParserContext(command, Toolshed);
        var success = CommandRun.TryParse(false, parser, inputType, expectedType, once, out _, out _, out var error);

        if (error is not null)
            ReportError(error);

        if (error is null)
            Assert.That(success, $"Parse failed despite no error being reported. Parsed {command}");
    }

    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        if (InvocationContext is not null)
        {
            return InvocationContext.CheckInvokable(command, out error);
        }

        error = null;
        return true;
    }

    protected ICommonSession? InvocationSession { get; set; }
    public NetUserId? User => Session?.UserId;

    public ICommonSession? Session
    {
        get
        {
            if (InvocationContext is not null)
            {
                return InvocationContext.Session;
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
