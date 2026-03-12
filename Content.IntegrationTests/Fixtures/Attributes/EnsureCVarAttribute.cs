using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Ensures the given CVar, on the given side (or both), is the given value.
///     Attribute version of <see cref="GameTest.OverrideCVar{T}"/>, and stores the old value the same way.
/// </summary>
/// <remarks>This only works with <see cref="GameTest"/> fixtures.</remarks>
/// <param name="side">The side to set the CVar on, or both.</param>
/// <param name="definitionType">The type the CVar is defined on.</param>
/// <param name="fieldName">The name of the static field defining the CVar.</param>
/// <param name="value">The value to set the CVar to.</param>
/// <example>
/// <code>
///     [Test]
///     [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.FlavorText), true)]
///     public async Task MyTest()
///     {
///         // CVar is set for you inside the test, and automatically un-set on teardown.
///     }
/// </code>
/// </example>
/// <seealso cref="GameTest"/>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class EnsureCVarAttribute(Side side, Type definitionType, string fieldName, object value) : Attribute, IGameTestModifier, IApplyToTest
{
    private const string ClientEnsuredCVarsProperty = "ClientEnsuredCVars";
    private const string ServerEnsuredCVarsProperty = "ServerEnsuredCVars";

    Task IGameTestModifier.ApplyToTest(GameTest test)
    {
        var cvar = LookupCVar();

        test.PreTestAddOverride(side, cvar!.Name, value);

        return Task.CompletedTask;
    }

    private CVarDef LookupCVar()
    {
        var field = definitionType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
        if (field is null)
            throw new ArgumentException($"Couldn't find a public, static field named {fieldName} on {definitionType}");

        var obj = field.GetValue(field);

        if (obj is not CVarDef cvar)
        {
            throw new ArgumentException(
                $"Expected a CVar definition on {definitionType}.{fieldName}, but it was a {obj?.GetType().FullName ?? "null"}");
        }

        if (value.GetType() != cvar!.DefaultValue.GetType())
            throw new NotSupportedException($"Cannot set {cvar.Name} to {value}, it's the wrong type.");

        return cvar;
    }

    void IApplyToTest.ApplyToTest(Test test)
    {
        var cvar = LookupCVar();

        if ((side & Side.Client) != 0)
            test.Properties.Add(ClientEnsuredCVarsProperty, $"{cvar} = {value}");

        if ((side & Side.Server) != 0)
            test.Properties.Add(ServerEnsuredCVarsProperty, $"{cvar} = {value}");
    }
}
