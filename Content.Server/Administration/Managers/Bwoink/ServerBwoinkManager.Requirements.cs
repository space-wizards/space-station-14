using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Requirements;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager
{
    /// <summary>
    /// Dictionary of channels with a list for whitelisting.
    /// </summary>
    /// <seealso cref="ListRequirement"/>
    [ViewVariables]
    private Dictionary<ProtoId<BwoinkChannelPrototype>, List<NetUserId>> _channelAllowList = new();

    private void SyncChannelsRequest(MsgBwoinkSyncChannelsRequest message)
    {
        SyncChannels(PlayerManager.GetSessionByChannel(message.MsgChannel));
    }

    /// <summary>
    /// Event that gets raised when CanManageChannel is called.
    /// This is to be used for conditions that do not exactly fit into this file to avoid making a mega class.
    /// </summary>
    /// <remarks>
    /// Called *after* the other access checks are done.
    /// </remarks>
    [PublicAPI]
    public event EventHandler<BwoinkRequirementCheckEventArgs>? OnManageCheck;
    /// <summary>
    /// Event that gets raised when CanReadChannel is called.
    /// This is to be used for conditions that do not exactly fit into this file to avoid making a mega class.
    /// </summary>
    /// <remarks>
    /// Called after the other access checks are done.
    /// </remarks>
    [PublicAPI]
    public event EventHandler<BwoinkRequirementCheckEventArgs>? OnReadCheck;
    /// <summary>
    /// Event that gets raised when CanWriteChannel is called.
    /// This is to be used for conditions that do not exactly fit into this file to avoid making a mega class.
    /// </summary>
    /// <remarks>
    /// Called after the other access checks are done.
    /// </remarks>
    [PublicAPI]
    public event EventHandler<BwoinkRequirementCheckEventArgs>? OnWriteCheck;

    /// <summary>
    /// Sends over the up-to-date list of channel condition flags to the session.
    /// </summary>
    public void SyncChannels(ICommonSession session)
    {
        var dict = new Dictionary<ProtoId<BwoinkChannelPrototype>, BwoinkChannelConditionFlags>();

        foreach (var (id, proto) in ProtoCache)
        {
            dict.Add(id, GetFlagsForChannel(proto, session));
        }

        _netManager.ServerSendMessage(new MsgBwoinkSyncChannels()
        {
            Channels = dict,
        }, session.Channel);
    }

    protected override void UpdatedChannels()
    {
        foreach (var session in PlayerManager.Sessions)
        {
            SyncChannels(session);
        }
    }

    /// <summary>
    /// Checks if a given session is able to manage a given bwoink channel.
    /// </summary>
    public override bool CanManageChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        var prototype = PrototypeManager.Index(proto);
        return CanManageChannel(prototype, session);
    }

    /// <inheritdoc cref="CanManageChannel(Robust.Shared.Prototypes.ProtoId{Content.Shared.Administration.Managers.Bwoink.BwoinkChannelPrototype},Robust.Shared.Player.ICommonSession)"/>
    public override bool CanManageChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        var result = channel.ManageRequirement == null
               || CheckConditions(channel, channel.ManageRequirement.Requirements, channel.ManageRequirement.OperationMode, session);

        var eventCall = new BwoinkRequirementCheckEventArgs(channel, session, result);
        OnManageCheck?.Invoke(this, eventCall);
        return eventCall.CanAccess;
    }


    /// <summary>
    /// Checks if a given session is able to read in a channel.
    /// </summary>
    public override bool CanReadChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        var prototype = PrototypeManager.Index(proto);
        return CanReadChannel(prototype, session);
    }

    /// <inheritdoc cref="CanReadChannel(Robust.Shared.Prototypes.ProtoId{Content.Shared.Administration.Managers.Bwoink.BwoinkChannelPrototype},Robust.Shared.Player.ICommonSession)"/>
    public override bool CanReadChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        if (CanManageChannel(channel, session))
            return true;

        var result = channel.ReadRequirement == null
               || CheckConditions(channel, channel.ReadRequirement.Requirements, channel.ReadRequirement.OperationMode, session);

        var eventCall = new BwoinkRequirementCheckEventArgs(channel, session, result);
        OnReadCheck?.Invoke(this, eventCall);
        return eventCall.CanAccess;
    }
    /// <summary>
    /// Checks if a given session is able to write to a channel.
    /// </summary>
    public override bool CanWriteChannel(ProtoId<BwoinkChannelPrototype> proto, ICommonSession session)
    {
        var prototype = PrototypeManager.Index(proto);
        return CanWriteChannel(prototype, session);
    }

    /// <inheritdoc cref="CanWriteChannel(Robust.Shared.Prototypes.ProtoId{Content.Shared.Administration.Managers.Bwoink.BwoinkChannelPrototype},Robust.Shared.Player.ICommonSession)"/>
    public override bool CanWriteChannel(BwoinkChannelPrototype channel, ICommonSession session)
    {
        if (CanManageChannel(channel, session))
            return true;

        var result = channel.WriteRequirement == null
               || CheckConditions(channel, channel.WriteRequirement.Requirements, channel.WriteRequirement.OperationMode, session);

        var eventCall = new BwoinkRequirementCheckEventArgs(channel, session, result);
        OnWriteCheck?.Invoke(this, eventCall);
        return eventCall.CanAccess;
    }

    private bool CheckConditions(BwoinkChannelPrototype prototype, List<BwoinkChannelCondition> requirements, RequirementOperationMode mode, ICommonSession session)
    {
        if (mode == RequirementOperationMode.None)
            return false;

        var matchedAll = true;
        foreach (var requirement in requirements)
        {
            bool matched;
            switch (requirement)
            {
                case AdminFlagRequirement flagRequirement:
                    matched = CheckFlag(flagRequirement, session);
                    break;
                case ListRequirement:
                    matched = CheckList(prototype, session);
                    break;

                default:
                    throw new InvalidOperationException("Unknown requirement: " + requirement);
            }

            switch (mode)
            {
                case RequirementOperationMode.Any:
                    if (matched)
                        return true;
                    break;
                case RequirementOperationMode.InvertedAny:
                    if (matched)
                        return false;
                    break;
            }

            if (!matched)
                matchedAll = false;
        }

        // This assumes default of "All"
        return matchedAll;
    }

    #region Conditions

    private bool CheckFlag(AdminFlagRequirement req, ICommonSession ses)
    {
        return AdminManager.HasAdminFlag(ses, req.Flags);
    }

    private bool CheckList(BwoinkChannelPrototype channel, ICommonSession amongus)
    {
        if (!_channelAllowList.TryGetValue(channel, out var users))
            return false;

        return users.Contains(amongus.UserId);
    }

    #endregion
}

/// <summary>
/// Event args for when an access check for a bwoink channel is performed.
/// </summary>
public sealed class BwoinkRequirementCheckEventArgs(BwoinkChannelPrototype proto, ICommonSession session, bool canAccess) : EventArgs
{
    public BwoinkChannelPrototype Prototype { get; } = proto;
    public ICommonSession Session { get; } = session;
    public bool CanAccess { get; set; } = canAccess;
}
