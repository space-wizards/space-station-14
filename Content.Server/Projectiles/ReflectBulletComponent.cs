namespace Content.Server.Projectiles
{
    /// <summary>
    /// Entities with this component have a chance to reflect projectiles
    /// </summary>
    [RegisterComponent]
    public sealed class ReflectProjectileComponent : Component 
    {
        [DataField("reflectChance")]
        public float ReflectChance;
    }
}