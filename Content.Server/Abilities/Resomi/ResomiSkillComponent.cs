using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities.Resomi
{
    [RegisterComponent]
    public sealed partial class ResomiSkillComponent : Component
    {
        /// <summary>
        /// Whether this component is active or not.
        /// </summarY>
        [DataField("enabled")]
        public bool Enabled = true;

        [DataField("invisibleWallAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? InvisibleWallAction = "ActionMimeInvisibleWall";

        [DataField("invisibleWallActionEntity")] public EntityUid? InvisibleWallActionEntity;

        // The vow zone lies below
        public bool VowBroken = false;

        public bool ReadyToRepent = false;

        /// <summary>
        /// Time when the mime can repent their vow
        /// </summary>
        [DataField("vowRepentTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan VowRepentTime = TimeSpan.Zero;

        /// <summary>
        /// How long it takes the mime to get their powers back
        /// </summary>
        [DataField("vowCooldown")]
        public TimeSpan VowCooldown = TimeSpan.FromMinutes(5);

        [DataField]
        public ProtoId<AlertPrototype> VowAlert = "VowOfSilence";

        [DataField]
        public ProtoId<AlertPrototype> VowBrokenAlert = "VowBroken";

        [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string WallPrototype = "WallInvisible";

    }
}
