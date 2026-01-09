using Content.Server.Administration;
using Content.Server.Toolshed.TypeParsers.StatusEffects;
using Content.Shared.Administration;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Toolshed.Commands.StatusEffects;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class StatusEffectCommand : ToolshedCommand
{
    private StatusEffectsSystem? _statusEffectsSystem;

    [CommandImplementation("add")]
    public EntityUid? Add([PipedArgument] EntityUid input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time, float delay = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TryAddStatusEffectDuration(
            input,
            status,
            TimeSpan.FromSeconds(time),
            ZeroAsNull(delay));

        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time, float delay = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TryAddStatusEffectDuration(
                ent,
                status,
                TimeSpan.FromSeconds(time),
                ZeroAsNull(delay));

            yield return ent;
        }
    }

    [CommandImplementation("update")]
    public EntityUid? Update([PipedArgument] EntityUid input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time, float delay = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TryUpdateStatusEffectDuration(
            input,
            status,
            ZeroAsNull(time),
            ZeroAsNull(delay));

        return input;
    }

    [CommandImplementation("update")]
    public IEnumerable<EntityUid> Update([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time, float delay = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TryUpdateStatusEffectDuration(
                ent,
                status,
                ZeroAsNull(time),
                ZeroAsNull(delay));

            yield return ent;
        }
    }

    [CommandImplementation("set")]
    public EntityUid? Set([PipedArgument] EntityUid input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time = 0, float delay = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TrySetStatusEffectDuration(
            input,
            status,
            ZeroAsNull(time),
            ZeroAsNull(delay));

        return input;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time = 0, float delay = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TrySetStatusEffectDuration(
                ent,
                status,
                ZeroAsNull(time),
                ZeroAsNull(delay));

            yield return ent;
        }
    }

    [CommandImplementation("remove")]
    public EntityUid? Remove([PipedArgument] EntityUid input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TryRemoveTime(
            input,
            status,
            ZeroAsNull(time));

        return input;
    }

    [CommandImplementation("remove")]
    public IEnumerable<EntityUid> Remove([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument(typeof(StatusEffectCompletionParser))] EntProtoId status, float time = 0)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TryRemoveTime(
                ent,
                status,
                ZeroAsNull(time));

            yield return ent;
        }
    }

    private static TimeSpan? ZeroAsNull(float delay)
    {
        return delay == 0 ? null : TimeSpan.FromSeconds(delay);
    }
}
