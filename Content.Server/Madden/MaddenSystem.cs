using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.Roles;
using Content.Shared.Administration.Components;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Madden;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Player;

namespace Content.Server.Madden;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class MaddenSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private ChatSystem _chatSystem = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaddeningComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<MaddeningComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<MaddeningComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<MaddenedRoleComponent, GetBriefingEvent>(OnBriefing);
    }

    private void OnEquip(Entity<MaddeningComponent> ent, ref GotEquippedHandEvent args)
    {
        Enmadden(args.User, ent);
    }

    private void OnUnequip(Entity<MaddeningComponent> ent, ref GotUnequippedHandEvent args)
    {
        Unmadden(args.User);

        ent.Comp.MaddenedEntity = null;
    }

    private void OnComponentRemove(Entity<MaddeningComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.MaddenedEntity is null)
            return;

        Unmadden(ent.Comp.MaddenedEntity.Value);

        ent.Comp.MaddenedEntity = null;
    }

    private void OnBriefing(Entity<MaddenedRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Briefing = (Loc.GetString("maddened-role-greeting"));
    }

    private void Enmadden(EntityUid victim, Entity<MaddeningComponent> maddenEnt)
    {
        if (!_mind.TryGetMind(victim, out var mind, out _) || !TryComp<ActorComponent>(victim, out var actor))
            return;

        if (HasComp<MaddenedComponent>(mind) || _admin.IsAdmin(actor.PlayerSession))
            return;

        EnsureComp<KillSignComponent>(victim);

        EnsureComp<MaddenedComponent>(mind);
        _role.MindAddRole(mind, "MindRoleMaddened");
        _antag.SendBriefing(victim, Loc.GetString("maddened-role-greeting"), Color.Silver, maddenEnt.Comp.Stinger);
        _adminLog.Add(LogType.AntagSelection,
            LogImpact.Extreme,
            $"{ToPrettyString(victim):player} was maddened by {ToPrettyString(maddenEnt):entity}");

        if (maddenEnt.Comp.AnnouncementText is null || maddenEnt.Comp.AnnouncementSender is null)
            return;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(maddenEnt.Comp.AnnouncementText),
            Loc.GetString(maddenEnt.Comp.AnnouncementSender),
            false,
            colorOverride: Color.DarkGray);
    }

    private void Unmadden(EntityUid maddenedEnt)
    {
        if (!_mind.TryGetMind(maddenedEnt, out var mind, out _))
            return;

        RemCompDeferred<KillSignComponent>(maddenedEnt);
        RemCompDeferred<MaddenedComponent>(mind);
        _role.MindRemoveRole(mind, "MindRoleMaddened");
    }
}
