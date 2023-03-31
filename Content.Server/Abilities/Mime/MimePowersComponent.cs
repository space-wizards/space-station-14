using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Server.Abilities.Mime
{
    /// <summary>
    /// Lets its owner entity use mime powers, like placing invisible walls.
    /// </summary>
    [RegisterComponent]
    public sealed class MimePowersComponent : Component
    {
        /// <summary>
        /// Whether this component is active or not.
        /// </summarY>
        [DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        /// The wall prototype to use.
        /// </summary>
        [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string WallPrototype = "WallInvisible";

        [DataField("invisibleWallAction")]
        public InstantAction InvisibleWallAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(30),
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Structures/Walls/solid.rsi/full.png")),
            DisplayName = "mime-invisible-wall",
            Description = "mime-invisible-wall-desc",
            Priority = -1,
            Event = new InvisibleWallActionEvent(),
        };

        // The vow zone lies below
        public bool VowBroken = false;

        /// <summary>
        /// Whether this mime is ready to take the vow again.
        /// Note that if they already have the vow, this is also false.
        /// </summary>
        public bool ReadyToRepent = false;

        /// <summary>
        /// Time when the mime can repent their vow
        /// </summary>
        [DataField("vowRepentTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan VowRepentTime = TimeSpan.Zero;

        /// <summary>
        /// How long it takes the mime to get their powers back
        /// </summary>
        [DataField("vowCooldown", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan VowCooldown = TimeSpan.FromMinutes(5);
    }
}
