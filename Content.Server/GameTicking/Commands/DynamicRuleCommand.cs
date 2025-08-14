using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Rules;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.GameTicking.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class DynamicRuleCommand : ToolshedCommand
{
    private DynamicRuleSystem? _dynamicRuleSystem;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.GetDynamicRules();
    }

    [CommandImplementation("get")]
    public EntityUid Get()
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.GetDynamicRules().FirstOrDefault();
    }

    [CommandImplementation("budget")]
    public IEnumerable<float?> Budget([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Budget);

    [CommandImplementation("budget")]
    public float? Budget([PipedArgument] EntityUid input)
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.GetRuleBudget(input);
    }

    [CommandImplementation("adjust")]
    public IEnumerable<float?> Adjust([PipedArgument] IEnumerable<EntityUid> input, float value)
        => input.Select(i => Adjust(i,value));

    [CommandImplementation("adjust")]
    public float? Adjust([PipedArgument] EntityUid input, float value)
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.AdjustBudget(input, value);
    }

    [CommandImplementation("set")]
    public IEnumerable<float?> Set([PipedArgument] IEnumerable<EntityUid> input, float value)
        => input.Select(i => Set(i,value));

    [CommandImplementation("set")]
    public float? Set([PipedArgument] EntityUid input, float value)
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.SetBudget(input, value);
    }

    [CommandImplementation("dryrun")]
    public IEnumerable<IEnumerable<EntProtoId>> DryRun([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(DryRun);

    [CommandImplementation("dryrun")]
    public IEnumerable<EntProtoId> DryRun([PipedArgument] EntityUid input)
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.DryRun(input);
    }

    [CommandImplementation("executenow")]
    public IEnumerable<IEnumerable<EntityUid>> ExecuteNow([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(ExecuteNow);

    [CommandImplementation("executenow")]
    public IEnumerable<EntityUid> ExecuteNow([PipedArgument] EntityUid input)
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.ExecuteNow(input);
    }

    [CommandImplementation("rules")]
    public IEnumerable<IEnumerable<EntityUid>> Rules([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Rules);

    [CommandImplementation("rules")]
    public IEnumerable<EntityUid> Rules([PipedArgument] EntityUid input)
    {
        _dynamicRuleSystem ??= GetSys<DynamicRuleSystem>();

        return _dynamicRuleSystem.Rules(input);
    }
}

