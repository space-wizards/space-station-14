using Content.Shared.Gravity;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.Gravity;

public sealed partial class GravitySystem : SharedGravitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedGravityGeneratorComponent, AppearanceChangeEvent>(OnAppearanceChange);
        InitializeShake();
    }

    /// <summary>
    /// Ensures that the visible state of gravity generators are synced with their sprites.
    /// </summary>
    private void OnAppearanceChange(EntityUid uid, SharedGravityGeneratorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearanceSystem.TryGetData<PowerChargeStatus>(uid, PowerChargeVisuals.State, out var state, args.Component))
        {
            if (comp.SpriteMap.TryGetValue(state, out var spriteState))
            {
                var layer = _sprite.LayerMapGet((uid, args.Sprite), GravityGeneratorVisualLayers.Base);
                _sprite.LayerSetRsiState((uid, args.Sprite), layer, spriteState);
            }
        }

        if (_appearanceSystem.TryGetData<float>(uid, PowerChargeVisuals.Charge, out var charge, args.Component))
        {
            var layer = _sprite.LayerMapGet((uid, args.Sprite), GravityGeneratorVisualLayers.Core);
            switch (charge)
            {
                case < 0.2f:
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
                    break;
                case >= 0.2f and < 0.4f:
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                    _sprite.LayerSetRsiState((uid, args.Sprite), layer, comp.CoreStartupState);
                    break;
                case >= 0.4f and < 0.6f:
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                    _sprite.LayerSetRsiState((uid, args.Sprite), layer, comp.CoreIdleState);
                    break;
                case >= 0.6f and < 0.8f:
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                    _sprite.LayerSetRsiState((uid, args.Sprite), layer, comp.CoreActivatingState);
                    break;
                default:
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                    _sprite.LayerSetRsiState((uid, args.Sprite), layer, comp.CoreActivatedState);
                    break;
            }
        }
    }
}

public enum GravityGeneratorVisualLayers : byte
{
    Base,
    Core
}
