using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Changeling.Systems;

public abstract partial class SharedChangelingLastResortSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;

    [SubscribeLocalEvent]
    private void OnTakeOverMapInit(Entity<ChangelingSlugComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnTakeOverShutdown(Entity<ChangelingSlugComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }
}
