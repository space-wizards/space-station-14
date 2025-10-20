using Content.Shared.Administration.Managers.Bwoink.Requirements;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Managers.Bwoink;

public abstract partial class SharedBwoinkManager
{
    /// <summary>
    /// Checks if a given session is able to manage a given bwoink channel.
    /// </summary>
    public abstract bool CanManageChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session);

    /// <inheritdoc cref="CanManageChannel(Robust.Shared.Prototypes.ProtoId{Content.Shared.Administration.Managers.Bwoink.BwoinkChannelPrototype},Robust.Shared.Player.ICommonSession)"/>
    public abstract bool CanManageChannel(BwoinkChannelPrototype channel, ICommonSession session);


    /// <summary>
    /// Checks if a given session is able to read in a channel.
    /// </summary>
    public abstract bool CanReadChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session);

    /// <inheritdoc cref="CanReadChannel(Robust.Shared.Prototypes.ProtoId{Content.Shared.Administration.Managers.Bwoink.BwoinkChannelPrototype},Robust.Shared.Player.ICommonSession)"/>
    public abstract bool CanReadChannel(BwoinkChannelPrototype channel, ICommonSession session);

    /// <summary>
    /// Checks if a given session is able to write to a channel.
    /// </summary>
    public abstract bool CanWriteChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session);

    /// <inheritdoc cref="CanWriteChannel(Robust.Shared.Prototypes.ProtoId{Content.Shared.Administration.Managers.Bwoink.BwoinkChannelPrototype},Robust.Shared.Player.ICommonSession)"/>
    public abstract bool CanWriteChannel(BwoinkChannelPrototype channel, ICommonSession session);

    /// <summary>
    /// Determines the effective <see cref="BwoinkChannelConditionFlags"/> for a given channel and session.
    /// </summary>
    /// <returns>
    /// A combination of <see cref="BwoinkChannelConditionFlags"/> indicating the user's permissions.
    /// </returns>
    /// <remarks>
    /// If the user can manage the channel, they automatically receive both <see cref="BwoinkChannelConditionFlags.Read"/>
    /// and <see cref="BwoinkChannelConditionFlags.Write"/> in addition to <see cref="BwoinkChannelConditionFlags.Manager"/>.
    /// </remarks>
    public BwoinkChannelConditionFlags GetFlagsForChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        var flags = BwoinkChannelConditionFlags.None;

        if (CanManageChannel(proto, session))
        {
            flags |= BwoinkChannelConditionFlags.Manager
                     | BwoinkChannelConditionFlags.Read
                     | BwoinkChannelConditionFlags.Write;
        }
        else
        {
            if (CanReadChannel(proto, session))
                flags |= BwoinkChannelConditionFlags.Read;

            if (CanWriteChannel(proto, session))
                flags |= BwoinkChannelConditionFlags.Write;
        }

        return flags;
    }
}

/// <summary>
/// Represents the combined output of <see cref="SharedBwoinkManager"/>'s <see cref="SharedBwoinkManager.CanWriteChannel(ProtoId{BwoinkChannelPrototype},ICommonSession)"/>, <see cref="SharedBwoinkManager.CanManageChannel(ProtoId{BwoinkChannelPrototype},ICommonSession)"/> and <see cref="SharedBwoinkManager.CanWriteChannel(ProtoId{BwoinkChannelPrototype},ICommonSession)"/> methods.
/// </summary>
[Flags]
public enum BwoinkChannelConditionFlags : byte
{
    None = 0,

    Manager = 1,
    Read = 2,
    Write = 4,
}
