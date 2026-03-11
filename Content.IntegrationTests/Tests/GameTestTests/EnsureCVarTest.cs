#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests.GameTestTests;

[TestOf(typeof(GameTest))]
[TestOf(typeof(EnsureCVarAttribute))]
[Description("Ensures EnsureCVar actually sets CVars as expected.")]
[EnsureCVar(Side.Server, typeof(EnsureCVarsTestCVars), nameof(EnsureCVarsTestCVars.Foo), true)]
public sealed class EnsureCVarTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly IConfigurationManager _sCfg = default!;

    [Test]
    public void FooIsSet()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sCfg.GetCVar(EnsureCVarsTestCVars.Foo), Is.True);
            Assert.That(_sCfg.GetCVar(EnsureCVarsTestCVars.Bar),
                Is.EqualTo(EnsureCVarsTestCVars.Bar.DefaultValue));
        }
    }

    [Test]
    [EnsureCVar(Side.Server, typeof(EnsureCVarsTestCVars), nameof(EnsureCVarsTestCVars.Bar), 42)]
    public void BarIsSet()
    {
        Assert.That(_sCfg.GetCVar(EnsureCVarsTestCVars.Bar),
            Is.EqualTo(42));
    }
}
