using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;

namespace Content.Client.StatusEffectNew;

/// <inheritdoc/>
public sealed partial class ClientStatusEffectsSystem : SharedStatusEffectsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<StatusEffectContainerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not StatusEffectContainerComponentState state)
            return;

        var toRemove = new ValueList<EntityUid>();
        foreach (var effect in ent.Comp.ActiveStatusEffects)
        {
            if (state.ActiveStatusEffects.Contains(GetNetEntity(effect)))
                continue;

            toRemove.Add(effect);
        }

        foreach (var effect in toRemove)
        {
            ent.Comp.ActiveStatusEffects.Remove(effect);
            var ev = new StatusEffectRemovedEvent(ent);
            RaiseLocalEvent(effect, ref ev);
        }

        foreach (var effect in state.ActiveStatusEffects)
        {
            var effectUid = GetEntity(effect);
            if (ent.Comp.ActiveStatusEffects.Contains(effectUid))
                continue;

            ent.Comp.ActiveStatusEffects.Add(effectUid);
            var ev = new StatusEffectAppliedEvent(ent);
            RaiseLocalEvent(effectUid, ref ev);
        }
    }
}
