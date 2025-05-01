using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components
{
    /// <summary>
    ///     Logic for using tools (Or verbs) to open / close something on an entity.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ToolOpenableComponent : Component
    {
        /// <summary>
        ///     Is the openable part open or closed?
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool IsOpen = false;

        /// <summary>
        ///     If a tool is needed to open the entity, this time will be used.
        /// </summary>
        [DataField]
        public float OpenTime = 1f;

        /// <summary>
        ///     If a tool is needed to close the entity, this time will be used.
        /// </summary>
        [DataField]
        public float CloseTime = 1f;

        /// <summary>
        ///     What type of tool quality is needed to open this?
        ///     If null, the it will only be openable by a verb.
        /// </summary>
        [DataField]
        public ProtoId<ToolQualityPrototype>? OpenToolQualityNeeded;

        /// <summary>
        ///     What type of tool quality is needed to close this.
        ///     If null, this will only be closable by a verb.
        /// </summary>
        [DataField]
        public ProtoId<ToolQualityPrototype>? CloseToolQualityNeeded;

        /// <summary>
        ///     If true, verbs will appear to help interact with opening / closing.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool HasVerbs = true;

        /// <summary>
        /// If true, the only way to interact is with verbs. Clicking on the entity will not do anything.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool VerbOnly;

        /// <summary>
        ///     The name of what is being open and closed.
        ///     E.g toilet lid, pannel, compartment.
        /// </summary>
        [DataField, AutoNetworkedField]
        public string? Name;

    }

    /// <summary>
    ///     Simple do after event for opening or closing.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class ToolOpenableDoAfterEventToggleOpen : SimpleDoAfterEvent
    {
    }

    /// <summary>
    ///     Visualizers for handling stash open closed state if stash has door.
    /// </summary>
    [Serializable, NetSerializable]
    public enum ToolOpenableVisuals : byte
    {
        ToolOpenableVisualState,
    }

    [Serializable, NetSerializable]
    public enum ToolOpenableVisualState : byte
    {
        Open,
        Closed
    }
}
