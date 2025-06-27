using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiia;

public abstract partial class SharedKodepiiaScramblerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaScramblerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<KodepiiaScramblerComponent, ComponentShutdown>(OnShutdown);
    }

    public sealed partial class KodepiiaScramblerEvent : InstantActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaScramblerDoAfterEvent : SimpleDoAfterEvent;

    public void OnStartup(Entity<KodepiiaScramblerComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ScramblerAction, ent.Comp.ScramblerActionId);
    }

    public void OnShutdown(Entity<KodepiiaScramblerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ScramblerAction);
    }

    public void PlaySound(EntityUid uid,KodepiiaScramblerComponent comp)
    {
        _audio.PlayPredicted(comp.ScramblerSound, uid, uid);
    }
}

