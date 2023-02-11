using Robust.Shared.Audio;

namespace Content.Server.Ranged.Weapons.Components
{
    /// <summary>
    ///     Entities with this component have a chance to reflect hitscan shots
    /// </summary>
    [RegisterComponent]
    public sealed class ReflectHitScanComponent : Component 
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
