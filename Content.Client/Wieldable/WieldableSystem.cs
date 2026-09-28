using System.Numerics;
using Content.Client.Movement.Components;
using Content.Client.Movement.Systems;
using Content.Shared.AttachedVisuals;
using Content.Shared.Camera;
using Content.Shared.Hands;
using Content.Shared.Movement.Components;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Client.Timing;

namespace Content.Client.Wieldable;

public sealed partial class WieldableSystem : SharedWieldableSystem
{
    [Dependency] private EyeCursorOffsetSystem _eyeOffset = default!;
    [Dependency] private IClientGameTiming _gameTiming = default!;
    [Dependency] private SpriteSystem _sprite = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursorOffsetRequiresWieldComponent, ItemUnwieldedEvent>(OnEyeOffsetUnwielded);
        SubscribeLocalEvent<CursorOffsetRequiresWieldComponent, HeldRelayedEvent<GetEyeOffsetRelayedEvent>>(OnGetEyeOffset);
    }

    [SubscribeLocalEvent]
    private void OnAttachedVisuals(Entity<WieldableComponent> ent, ref AttachedVisualsUpdatedEvent args)
    {
        if (!ent.Comp.Wielded)
            return;

        foreach (var index in args.Layers)
        {
            if (!_sprite.TryGetLayer(args.AttachedTo, index, out var layer, false))
                continue;

            if (layer.RSI == null)
                continue;

            var newState = $"{ent.Comp.WieldedInhandPrefix}-{layer.State}";

            if (!layer.RSI.TryGetState(newState, out var _))
                continue;

            _sprite.LayerSetRsiState(args.AttachedTo, index, newState);
        }
    }

    public void OnEyeOffsetUnwielded(Entity<CursorOffsetRequiresWieldComponent> entity, ref ItemUnwieldedEvent args)
    {
        if (!TryComp(entity.Owner, out EyeCursorOffsetComponent? cursorOffsetComp))
            return;

        if (_gameTiming.IsFirstTimePredicted)
        {
            cursorOffsetComp.CurrentPosition = Vector2.Zero;
            cursorOffsetComp.TargetPosition = Vector2.Zero;
        }
    }

    public void OnGetEyeOffset(Entity<CursorOffsetRequiresWieldComponent> entity, ref HeldRelayedEvent<GetEyeOffsetRelayedEvent> args)
    {
        if (!TryComp(entity.Owner, out WieldableComponent? wieldableComp))
            return;

        if (!wieldableComp.Wielded)
            return;

        var offset = _eyeOffset.OffsetAfterMouse(entity.Owner, null);
        if (offset == null)
            return;

        args.Args.Offset += offset.Value;
    }
}
