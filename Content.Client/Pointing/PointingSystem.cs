using Content.Shared.Input;
using Content.Shared.Pointing;
using Content.Shared.Pointing.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Input.Binding;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing;

public sealed partial class PointingSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PointingArrowComponent, ComponentStartup>(OnArrowStartup);
        SubscribeLocalEvent<RoguePointingArrowComponent, ComponentStartup>(OnRogueArrowStartup);
        InitializeVisualizer();
    }

    private void OnArrowStartup(EntityUid uid, PointingArrowComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        // Hide the pointer if it's serverside and ours.
        // TODO: Maybe a dedicated system for this due to gun prediction maybe?
        if (!GameTiming.IsFirstTimePredicted && component.Owner == PlayerManager.LocalEntity)
        {
            _sprite.SetVisible((uid, sprite), false);
            return;
        }

        BeginPointAnimation(uid, component.StartPosition);
    }

    private void OnRogueArrowStartup(EntityUid uid, RoguePointingArrowComponent arrow, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            _sprite.SetDrawDepth((uid, sprite), (int)DrawDepth.Overlays);
            sprite.NoRotation = false;
        }
    }

}
