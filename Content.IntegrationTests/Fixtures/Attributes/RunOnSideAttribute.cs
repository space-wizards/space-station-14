#nullable enable
using Content.IntegrationTests.NUnit.Utilities;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Ensures a test method runs on the given side (client or server).
/// </summary>
/// <remarks>
///     This only works for <see cref="GameTest"/> fixtures.
/// </remarks>
/// <seealso cref="GameTest"/>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RunOnSideAttribute : Attribute, IWrapTestMethod, IImplyFixture, IApplyToTest
{
    public const string RunOnSideProperty = "RanOnSide";

    /// <summary>
    ///     Which side to run the inner test code on, if not the test thread.
    /// </summary>
    public Side RunOnSide { get; }

    public RunOnSideAttribute(Side side)
    {
        RunOnSide = side;
        if (side is not Side.Client and not Side.Server)
            throw new NotSupportedException("Test run-on-side can only be the client or server, not both or neither.");
    }

    TestCommand ICommandWrapper.Wrap(TestCommand command)
    {
        return new SidedTestCommand(command, RunOnSide);
    }

    private sealed class SidedTestCommand : DelegatingTestCommand
    {
        private Side _side;

        public SidedTestCommand(TestCommand inner, Side side) : base(inner)
        {
            _side = side;
        }

        public override TestResult Execute(TestExecutionContext context)
        {
            innerCommand.Test.EnsureFixtureIsGameTest(typeof(RunOnSideAttribute), out var gt);

            if (_side is not Side.Client and not Side.Server)
                throw new NotSupportedException($"Sided tests need to specify a specific side. {Test}");

            if (_side is Side.Client)
            {
                gt.Client.WaitAssertion(() =>
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    })
                    .Wait();
            }
            else
            {
                gt.Server.WaitAssertion(() =>
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    })
                    .Wait();
            }

            return context.CurrentResult;
        }
    }

    public void ApplyToTest(Test test)
    {
        test.Properties.Add(RunOnSideProperty, RunOnSide.ToString());
    }
}
