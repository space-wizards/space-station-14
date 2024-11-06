using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components
{
    /// <summary>
    /// This object will speed up the movement speed of entities
    /// when collided with
    /// 
    /// Used for mucin
    /// 
    /// This partially replicates SpeedModifierContactsComponent because that
    /// component is already heavily coupled with existing puddle code.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class PropulsionComponent : Component
    {
        [DataField, ViewVariables]
        [AutoNetworkedField]
        public float WalkSpeedModifier = 1.0f;

        [AutoNetworkedField]
        [DataField, ViewVariables]
        public float SprintSpeedModifier = 1.0f;

        /// <summary>
        /// If an entity passes this, apply the speed modifier.
        /// Passes all entities if not defined.
        /// </summary>
        [AutoNetworkedField]
        [DataField, ViewVariables]
        public EntityWhitelist? Whitelist;
    }
}
