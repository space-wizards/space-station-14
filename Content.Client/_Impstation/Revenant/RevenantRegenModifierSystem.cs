using System.Numerics;
using Content.Client.Alerts;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Revenant;

public sealed class RevenantRegenModifierSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly SpriteSpecifier _witnessIndicator = new SpriteSpecifier.Texture(new ResPath("Interface/Actions/scream.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantRegenModifierComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
        SubscribeNetworkEvent<RevenantHauntWitnessEvent>(OnWitnesses);
    }

    private void OnWitnesses(RevenantHauntWitnessEvent args)
    {
        foreach (var witness in args.Witnesses)
        {
            var ent = GetEntity(witness);
            if (TryComp<SpriteComponent>(ent, out var sprite))
            {
                var layer = sprite.AddLayer(_witnessIndicator);

                sprite.LayerMapSet(RevenantWitnessVisuals.Key, layer);
                sprite.LayerSetOffset(layer, new Vector2(0, 0.8f));
                sprite.LayerSetScale(layer, new Vector2(0.65f, 0.65f));

                Timer.Spawn(TimeSpan.FromSeconds(5), () => sprite.RemoveLayer(RevenantWitnessVisuals.Key));
            }
        }
    }

    private void OnUpdateAlert(Entity<RevenantRegenModifierComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.Alert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var witnesses = Math.Clamp(ent.Comp.Witnesses.Count, 0, 99);
        sprite.LayerSetState(RevenantVisualLayers.Digit1, $"{witnesses / 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit2, $"{witnesses % 10}");
    }
}

public enum RevenantWitnessVisuals : byte
{
    Key
}
