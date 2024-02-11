using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Standing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(StandingStateSystem))]
    public sealed partial class StandingStateComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public SoundSpecifier DownSound { get; private set; } = new SoundCollectionSpecifier("BodyFall");

        [DataField, AutoNetworkedField]
        public bool Standing { get; set; } = true;

        /// <summary>
        ///     List of fixtures that had their collision mask changed when the entity was downed.
        ///     Required for re-adding the collision mask.
        /// </summary>
        [DataField, AutoNetworkedField]
        public List<string> MaskChangedFixtures = new();

        /// <summary>
        ///     List of fixtures that had their collision layer changed when the entity was downed.
        ///     Required for re-adding the collision layer.
        /// </summary>
        [DataField, AutoNetworkedField]
        public List<string> LayerChangedFixtures = new();

        /// <summary>
        ///     The CollisionLayer that will be subtracted from the entity's CollisionLayer when it falls down.
        /// </summary>
        [DataField]
        public CollisionGroup StandingLayer = CollisionGroup.MobLayer;

        /// <summary>
        ///     The CollisionLayer that will be added to the entity's CollisionLayer when it falls down.
        /// </summary>
        [DataField]
        public CollisionGroup LayingDownLayer = CollisionGroup.Opaque;
    }
}
