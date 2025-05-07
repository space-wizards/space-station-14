namespace Content.Shared.Throwing;

public sealed class ThrowSpeedModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowSpeedModifierComponent, BeforeThrowEvent>(ApplyModifier);
    }

    private void ApplyModifier(Entity<ThrowSpeedModifierComponent> ent, ref BeforeThrowEvent args)
    {
        //Apply speed modifiers
        args.ThrowSpeed += ent.Comp.FlatModifier;
        args.ThrowSpeed *= ent.Comp.Multiplier;
        //Make sure it's at least 0
        if (args.ThrowSpeed < 0) { args.ThrowSpeed = 0; }
    }
}
