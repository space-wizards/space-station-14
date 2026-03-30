#nullable enable
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests.GameTestTests;

[CVarDefs]
public sealed class EnsureCVarsTestCVars
{
    public static readonly CVarDef<bool> Foo =
        CVarDef.Create("tests.ensure_cvars.foo", false, CVar.SERVER);

    public static readonly CVarDef<int> Bar =
        CVarDef.Create("tests.ensure_cvars.bar", 3, CVar.SERVER);
}
