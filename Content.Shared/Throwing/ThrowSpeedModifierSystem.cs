namespace Content.Shared.Throwing;

public sealed class ThrowSpeedModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowSpeedModifierComponent, BeforeBeingThrownEvent>(ApplyModifier);
    }

    private void ApplyModifier(Entity<ThrowSpeedModifierComponent> ent, ref BeforeBeingThrownEvent args)
    {
        args.ThrowSpeed += ent.Comp.FlatModifier;
        args.ThrowSpeed *= ent.Comp.Multiplier;

        if (args.ThrowSpeed < 0) { args.ThrowSpeed = 0; }  //Make sure it's at least 0
    }
}
