namespace Content.Shared.EntityEffects.NewEffects;

public abstract partial class SharedMakeSentientEntityEffectSystem : EntityEffectSystem<MetaDataComponent, MakeSentient>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<MakeSentient> args)
    {
        // Serverside effect
    }
}

public sealed class MakeSentient : EntityEffectBase<MakeSentient>
{
    /// <summary>
    /// Description for the ghost role created by this effect.
    /// </summary>
    [DataField]
    public LocId RoleDescription = "ghost-role-information-cognizine-description";

    /// <summary>
    /// Whether we give the target the ability to speak coherently.
    /// </summary>
    [DataField]
    public bool AllowSpeech = true;
}
