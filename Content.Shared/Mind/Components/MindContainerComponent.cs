using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;

namespace Content.Shared.Mind.Components
{
    /// <summary>
    /// This component indicates that this entity may have mind, which is simply an entity with a <see cref="MindComponent"/>.
    /// The mind entity is not actually stored in a "container", but is simply stored in nullspace.
    /// </summary>
    [RegisterComponent, Access(typeof(SharedMindSystem)), NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class MindContainerComponent : Component
    {
        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [DataField, AutoNetworkedField]
        [Access(typeof(SharedMindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public EntityUid? Mind { get; set; }

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Mind))]
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Whether examining should show information about the mind or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showExamineInfo"), AutoNetworkedField]
        public bool ShowExamineInfo { get; set; }

        /// <summary>
        ///     Whether the mind will be put on a ghost after this component is shutdown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ghostOnShutdown")]
        [Access(typeof(SharedMindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public bool GhostOnShutdown { get; set; } = true;
    }

    public sealed class MindRemovedMessage : EntityEventArgs
    {
        public readonly Entity<MindComponent> Mind;

        public MindRemovedMessage(Entity<MindComponent> mind)
        {
            Mind = mind;
        }
    }

    public sealed class MindAddedMessage : EntityEventArgs
    {
        public readonly Entity<MindComponent> Mind;

        public MindAddedMessage(Entity<MindComponent> mind)
        {
            Mind = mind;
        }
    }
}
