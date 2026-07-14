using System.Collections.Generic;
using Content.Client.Stylesheets;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Analyzers;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using Serilog.Events;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.IntegrationTests.Tests.Stylesheets;

[TestOf(typeof(StylesheetDefinition))]
public sealed class StylesheetDefinitionTest : GameTest
{
    [SidedDependency(Side.Client)] private ILogManager _logManager = default!;

    #region Sheetlet Classes

    private interface ITestConfig : ISheetletConfig;

    private interface IExtraConfig : ISheetletConfig;

    [Virtual]
    private class TestDefinition : StylesheetDefinition, ITestConfig
    {
        public override Dictionary<Type, ResPath[]> Roots => new()
        {
            {
                typeof(TextureResource), [
                    new ResPath("/Textures/Interface/Nano"),
                    new ResPath("/Textures/Interface"),
                ]
            },
        };
    }

    private sealed class TestSpecificDefinition : TestDefinition;

    private sealed class TestMismatchDefinition : TestDefinition;

    [Sheetlet(typeof(TestDefinition))]
    private sealed class TestGenericSheetlet<T> : ISheetlet<T>
        where T : ITestConfig
    {
        public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
        {
            return
            [
                E().Prop("test", 1),
            ];
        }
    }

    [Sheetlet(typeof(TestSpecificDefinition))]
    private sealed class TestNonGenericSheetlet : ISheetlet<ITestConfig>
    {
        public StyleRule[] GetRules(StylesheetDefinition sheet, ITestConfig config)
        {
            return
            [
                E().Prop("test", 2),
            ];
        }
    }

    [Sheetlet(typeof(TestMismatchDefinition))]
    private sealed class TestMissingConstraintsSheetlet<T> : ISheetlet<T>
        where T : IExtraConfig
    {
        public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
        {
            return
            [
                E().Prop("test", true),
            ];
        }
    }

    #endregion

    [Test]
    [Description("Checks that sheetlets and stylesheet definitions work")]
    [RunOnSide(Side.Client)]
    public void TestStylesheetDefinition()
    {
        // Test that it can work at all
        var baseDefinition = new TestDefinition();
        var baseSheet = baseDefinition.Build();
        Assert.That(baseSheet.Rules, Has.Count.EqualTo(1));

        // Test that it can find inherited tests
        var specificDefinition = new TestSpecificDefinition();
        var specificSheet = specificDefinition.Build();
        Assert.That(specificSheet.Rules, Has.Count.EqualTo(2));

        // Verify the inheritance distance ordering is actually overriding correctly (in case RT changes it)
        var control = new Control();
        control.Stylesheet = specificSheet;
        control.ForceRunStyleUpdate();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.TryGetStyleProperty("test", out int testValue), Is.True);
            Assert.That(testValue, Is.EqualTo(2));
        }

        control.Orphan();
    }

    [Test]
    [Description("Checks that it errors when a constraint is missing.")]
    [RunOnSide(Side.Client)]
    public void TestMissingConstraints()
    {
        // It will print an error log, which would otherwise fail the test
        Pair.ClientLogHandler.JudgeLog += JudgeHasStyleError;

        // We can't directly introspect the logs that PoolTestLogHandler has, so we use the LogCatcher that RT provides
        // for a singular other test...
        var catcher = new LogCatcher();
        _logManager.GetSawmill("style").AddHandler(catcher);

        var style = new TestMismatchDefinition();
        var sheet = style.Build();

        using (Assert.EnterMultipleScope())
        {
            // It shouldn't include if it's missing a constraint
            Assert.That(sheet.Rules, Has.Count.EqualTo(1));

            // Make sure it prints an error
            Assert.That(catcher.CaughtLogs, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(catcher.CaughtLogs[0].Level, Is.EqualTo(LogEventLevel.Error));
        }

        _logManager.GetSawmill("style").RemoveHandler(catcher);
        Pair.ClientLogHandler.JudgeLog -= JudgeHasStyleError;
    }

    private static bool JudgeHasStyleError(string name, LogEvent logEvent)
    {
        return name == "style";
    }
}
