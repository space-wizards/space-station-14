using Content.Shared.Actions;
using Content.Shared.Throwing;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;


namespace Content.Shared.Movement.Systems;

public sealed partial class SharedJumpSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JumpAbilityComponent, GravityJumpEvent>(JumpAbility);
    }

    private void JumpAbility(Entity<JumpAbilityComponent> entity, ref GravityJumpEvent args)
    {
        var xform = Transform(args.Performer);
        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.JumpPower;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking

        _throwing.TryThrow(args.Performer, direction);

        _audio.PlayPredicted(entity.Comp.SoundJump, args.Performer, args.Performer);

        args.Handled = true;
    }
}
