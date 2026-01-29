using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Overlays;
using Content.Shared.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    private static readonly ProtoId<SiliconLawsetPrototype> DefaultCrewLawset = "Crewsimov";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(Entity<SiliconLawBoundComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.FromHex("#5ed7aa"));

        if (!ent.Comp.Subverted)
            return;

        EnsureSubvertedSiliconRole(args.Mind);

        var modifedLawMsg = Loc.GetString("laws-notify-subverted");
        var modifiedLawWrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", modifedLawMsg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, modifedLawMsg, modifiedLawWrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);
    }

    private void OnMindRemoved(Entity<SiliconLawBoundComponent> ent, ref MindRemovedMessage args)
    {
        if (!ent.Comp.Subverted)
            return;

        RemoveSubvertedSiliconRole(args.Mind);
    }

    public override void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(uid, cue);

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // TODO: Prediction dump this
        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out var provider))
            return;

        var lawset = provider.Lawset;

        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
        {
            if (TryComp<ShowCrewIconsComponent>(update, out var crewIconComp))
            {
                crewIconComp.UncertainCrewBorder = DefaultCrewLawset != provider.Laws;
                Dirty(update, crewIconComp);
            }

            SetProviderLaws(update, lawset.Laws, false, provider.LawUploadSound);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawProviderComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetBoundLaws(lawbound).Laws)
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
