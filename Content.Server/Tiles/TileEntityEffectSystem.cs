using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;

namespace Content.Server.Tiles;

public sealed class TileEntityEffectSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileEntityEffectComponent, StepTriggeredOffEvent>(OnTileStepTriggered);
        SubscribeLocalEvent<TileEntityEffectComponent, StepTriggerAttemptEvent>(OnTileStepTriggerAttempt);
    }
    private void OnTileStepTriggerAttempt(Entity<TileEntityEffectComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnTileStepTriggered(Entity<TileEntityEffectComponent> ent, ref StepTriggeredOffEvent args)
    {
        var otherUid = args.Tripper;

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(new EntityEffectBaseArgs(otherUid, EntityManager));
        }
    }
}
