using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class ExaminableStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExaminableStatusEffectComponent, StatusEffectRelayedEvent<ExaminedEvent>>(OnExamined);
    }

    private void OnExamined(Entity<ExaminableStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ExaminedEvent> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        args.Args.PushMarkup(Loc.GetString(ent.Comp.Message, ("target", Identity.Entity(target, EntityManager))));
    }
}
