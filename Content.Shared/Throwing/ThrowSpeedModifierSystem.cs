namespace Content.Shared.Throwing;

public sealed class ThrowSpeedModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrowSpeedModifierComponent, ThrowSpeedModifierEvent>(ApplyModifier);
    }

    private void ApplyModifier(Entity<ThrowSpeedModifierComponent> ent, ref ThrowSpeedModifierEvent args)
    {
        Log.Debug("wawawawawawawawaw");

    }
}
