using Content.Server.Administration;
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
    public EntityUid? Add([PipedArgument] EntityUid input, EntProtoId status, float time, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TryAddStatusEffectDuration(
            input,
            status,
            TimeSpan.FromSeconds(time),
            delay == null ? null : TimeSpan.FromSeconds(delay.Value));

        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add([PipedArgument] IEnumerable<EntityUid> input, EntProtoId status, float time, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TryAddStatusEffectDuration(
                ent,
                status,
                TimeSpan.FromSeconds(time),
                delay == null ? null : TimeSpan.FromSeconds(delay.Value));

            yield return ent;
        }
    }

    [CommandImplementation("update")]
    public EntityUid? Update([PipedArgument] EntityUid input, EntProtoId status, float? time, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TryUpdateStatusEffectDuration(
            input,
            status,
            time == null ? null : TimeSpan.FromSeconds(time.Value),
            delay == null ? null : TimeSpan.FromSeconds(delay.Value));

        return input;
    }

    [CommandImplementation("update")]
    public IEnumerable<EntityUid> Update([PipedArgument] IEnumerable<EntityUid> input, EntProtoId status, float? time = null, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TryUpdateStatusEffectDuration(
                ent,
                status,
                time == null ? null : TimeSpan.FromSeconds(time.Value),
                delay == null ? null : TimeSpan.FromSeconds(delay.Value));

            yield return ent;
        }
    }

    [CommandImplementation("set")]
    public EntityUid? Set([PipedArgument] EntityUid input, EntProtoId status, float? time, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TrySetStatusEffectDuration(
            input,
            status,
            time == null ? null : TimeSpan.FromSeconds(time.Value),
            delay == null ? null : TimeSpan.FromSeconds(delay.Value));

        return input;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set([PipedArgument] IEnumerable<EntityUid> input, EntProtoId status, float? time = null, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TrySetStatusEffectDuration(
                ent,
                status,
                time == null ? null : TimeSpan.FromSeconds(time.Value),
                delay == null ? null : TimeSpan.FromSeconds(delay.Value));

            yield return ent;
        }
    }

    [CommandImplementation("remove")]
    public EntityUid? Remove([PipedArgument] EntityUid input, EntProtoId status, float? time, float? delay = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        _statusEffectsSystem.TryRemoveTime(
            input,
            status,
            time == null ? null : TimeSpan.FromSeconds(time.Value));

        return input;
    }

    [CommandImplementation("remove")]
    public IEnumerable<EntityUid> Remove([PipedArgument] IEnumerable<EntityUid> input, EntProtoId status, float? time = null)
    {
        _statusEffectsSystem ??= GetSys<StatusEffectsSystem>();

        foreach (var ent in input)
        {
            _statusEffectsSystem.TryRemoveTime(
                ent,
                status,
                time == null ? null : TimeSpan.FromSeconds(time.Value));

            yield return ent;
        }
    }
}
