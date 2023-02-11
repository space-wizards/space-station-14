using Robust.Shared.Audio;

namespace Content.Server.Projectiles
{
    /// <summary>
    ///     Entities with this component have a chance to reflect projectiles
    /// </summary>
    [RegisterComponent]
    public sealed class ReflectProjectileComponent : Component 
    {
        /// <summary>
        ///     Can only reflect when enabled
        /// </summary>
        [DataField("enabled")]
        public bool Enabled;

        [DataField("reflectChance")]
        public float ReflectChance;

        [DataField("onReflect")]
        public SoundSpecifier? OnReflect { get; set; }
    }
}