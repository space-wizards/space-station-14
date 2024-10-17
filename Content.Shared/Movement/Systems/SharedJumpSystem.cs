using Content.Shared.Actions;
using Content.Shared.Clothing;
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
        SubscribeLocalEvent<JumpComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<JumpComponent, GravityJumpEvent>(JumpAbility);
    }


    public void OnEquip(Entity<JumpComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _actions.AddAction(args.Wearer, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);

        ent.Comp.IsClothing = true;
        ent.Comp.OnClothingEntity = args.Wearer;
    }


    public void JumpAbility(Entity<JumpComponent> entity, ref GravityJumpEvent args)
    {
        if (entity.Comp.IsClothing != null) // checking for wearing clothing with a jump component
        {
            entity.Owner = entity.Comp.OnClothingEntity;
        }

        var xform = Transform(entity.Owner);
        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.JumpPower;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking

        _throwing.TryThrow(entity.Owner, direction);

        _audio.PlayPredicted(entity.Comp.SoundJump, entity.Owner, entity);

        args.Handled = true;
    }
}
