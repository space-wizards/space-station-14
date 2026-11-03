using Content.Shared.Pointing;
using Content.Shared.Pointing.Components;
using Robust.Client.GameObjects;

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

    public void TryPointAtEntity(EntityUid uid)
    {
        RaisePredictiveEvent(new NetworkPointAttemptEvent()
        {
            Target = GetNetEntity(uid),
        });
    }

    private void OnArrowStartup(EntityUid uid, PointingArrowComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        // Hide the pointer if it's serverside and ours.
        // TODO: Maybe a dedicated system for this due to gun prediction maybe?
        if (!GameTiming.IsFirstTimePredicted && component.User == PlayerManager.LocalEntity)
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
            sprite.NoRotation = false;
        }
    }
}
