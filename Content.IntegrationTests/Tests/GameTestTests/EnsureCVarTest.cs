#nullable enable
using System.Linq;
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
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _cCfg = default!;

    [Test]
    [Description("Ensure Foo is set and Bar is not.")]
    public void FooIsSet()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sCfg.GetCVar(EnsureCVarsTestCVars.Foo), Is.True);
            Assert.That(_cCfg.GetCVar(EnsureCVarsTestCVars.Foo),
                Is.EqualTo(EnsureCVarsTestCVars.Foo.DefaultValue),
                "Foo is not replicated and should not be set on the client.");

            Assert.That(_sCfg.GetCVar(EnsureCVarsTestCVars.Bar),
                Is.EqualTo(EnsureCVarsTestCVars.Bar.DefaultValue));
        }
    }

    [Test]
    [EnsureCVar(Side.Server, typeof(EnsureCVarsTestCVars), nameof(EnsureCVarsTestCVars.Bar), 42)]
    [Description("Ensure Foo and Bar are set.")]
    public void BarIsSet()
    {
        var props = TestContext.CurrentContext.Test.Properties;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sCfg.GetCVar(EnsureCVarsTestCVars.Bar),
                Is.EqualTo(42));
            Assert.That(_cCfg.GetCVar(EnsureCVarsTestCVars.Bar),
                Is.EqualTo(EnsureCVarsTestCVars.Bar.DefaultValue),
                "Bar is not replicated and should not be set on the client.");

            Assert.That(props[EnsureCVarAttribute.ServerEnsuredCVarsProperty].Count(),
                Is.EqualTo(1),
                "Expected EnsureCVar to appropriately mark its target test.");
        }
    }
}
