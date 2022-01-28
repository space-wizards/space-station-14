using Content.Server.Explosion.EntitySystems;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Whenever a <see cref="TriggerEvent"/> is run play a sound in PVS range.
    /// </summary>
    [RegisterComponent]
    public sealed class SoundOnTriggerComponent : Component
    {
        public override string Name => "SoundOnTrigger";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier? Sound { get; set; }
    }
}
