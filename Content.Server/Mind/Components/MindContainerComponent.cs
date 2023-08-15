using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Mind.Components
{
    /// <summary>
    ///     Stores a <see cref="Server.Mind.Mind"/> on a mob.
    /// </summary>
    [RegisterComponent, Access(typeof(MindSystem))]
    public sealed class MindContainerComponent : Component
    {
        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [ViewVariables]
        [Access(typeof(MindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public Mind? Mind { get; set; }

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        [ViewVariables]
        [MemberNotNullWhen(true, nameof(Mind))]
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Whether examining should show information about the mind or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showExamineInfo")]
        public bool ShowExamineInfo { get; set; }

        /// <summary>
        ///     Whether the mind will be put on a ghost after this component is shutdown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ghostOnShutdown")]
        [Access(typeof(MindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public bool GhostOnShutdown { get; set; } = true;
    }

    public sealed class MindRemovedMessage : EntityEventArgs
    {
        public Mind OldMind;

        public MindRemovedMessage(Mind oldMind)
        {
            OldMind = oldMind;
        }
    }

    public sealed class MindAddedMessage : EntityEventArgs
    {
    }
}
