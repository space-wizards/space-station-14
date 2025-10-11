using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class LungFunctionModifierStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LungFunctionModifierStatusEffectComponent, StatusEffectRelayedEvent<ModifiedLungFunctionEvent>>(OnModifiedVascularTone);
    }

    private void OnModifiedVascularTone(Entity<LungFunctionModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ModifiedLungFunctionEvent> args)
    {
        args.Args = args.Args with { Function = MathF.Max(ent.Comp.Function, args.Args.Function) };
    }
}
