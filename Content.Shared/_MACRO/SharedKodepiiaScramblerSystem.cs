using Content.Shared._MACRO.Species.Kodepiia.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.Species.Kodepiia;
/// <summary>
/// Handles kodepiia appearance scrambling, which is essentially randomizing their appearance.
/// </summary>
public abstract partial class SharedKodepiiaScramblerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actionsSystem = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<KodepiiaScramblerComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ScramblerAction, ent.Comp.ScramblerActionId);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<KodepiiaScramblerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ScramblerAction);
    }
}

/// <summary>
/// Event that is triggered when the scrambler's action is used.
/// </summary>
public sealed partial class KodepiiaScramblerEvent : InstantActionEvent;

/// <summary>
/// This sure is a scrambler doafter event!
/// </summary>
[Serializable, NetSerializable]
public sealed partial class KodepiiaScramblerDoAfterEvent : SimpleDoAfterEvent;
