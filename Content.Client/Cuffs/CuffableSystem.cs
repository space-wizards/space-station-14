using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Cuffs;

public sealed class CuffableSystem : SharedCuffableSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffableComponent, ComponentShutdown>(OnCuffableShutdown);
    }

    private void OnCuffableShutdown(Entity<CuffableComponent> entity, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(entity, out var sprite))
            _sprite.LayerSetVisible((entity, sprite), HumanoidVisualLayers.Handcuffs, false);
    }

    protected override void UpdateCuffState(Entity<CuffableComponent> entity)
    {
        base.UpdateCuffState(entity);

        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        _sprite.LayerSetVisible((entity, sprite), HumanoidVisualLayers.Handcuffs, entity.Comp.Cuffed);

        // if they are not cuffed, that means that we didn't get a valid color,
        // iconstate, or RSI. that also means we don't need to update the sprites.
        if (GetLastCuffOrNull(entity.AsNullable()) is not { } cuff)
            return;

        _sprite.LayerSetColor((entity, sprite), HumanoidVisualLayers.Handcuffs, cuff.Comp.Color);

        if (Equals(entity.Comp.CurrentRSI, cuff.Comp.CuffedRSI) || cuff.Comp.CuffedRSI == null) // we don't want to keep loading the same RSI
            return;

        entity.Comp.CurrentRSI = cuff.Comp.CuffedRSI;
        var state = entity.Comp.State != null && cuff.Comp.ValidStates.Contains(entity.Comp.State) ? entity.Comp.State : CuffableComponent.DefaultState;
        _sprite.LayerSetRsi((entity, sprite), _sprite.LayerMapGet((entity, sprite), HumanoidVisualLayers.Handcuffs), new ResPath(entity.Comp.CurrentRSI), state);
    }
}

