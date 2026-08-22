using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.Roles;
using Content.Shared.Administration.Components;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Madden;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Madden;

/// <summary>
/// This handles items that give the Maddened antag status such as Thronglers, giving the status when picked up and lost when dropped.
/// </summary>
public sealed partial class MaddenSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    #region Subscriptions

    [SubscribeLocalEvent]
    private void OnEquip(Entity<MaddeningComponent> ent, ref GotEquippedHandEvent args)
    {
        HandleMaddeningItemMove(ent);
    }

    [SubscribeLocalEvent]
    private void OnUnequip(Entity<MaddeningComponent> ent, ref DroppedEvent args)
    {
        HandleMaddeningItemMove(ent);
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(Entity<MaddeningComponent> ent, ref ComponentRemove args)
    {
        Unmadden(ent.Comp.MaddenedEntity);
        ent.Comp.MaddenedEntity = null;
    }

    [SubscribeLocalEvent]
    private void OnBriefing(Entity<MaddenedRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Briefing = (Loc.GetString("maddened-role-greeting"));
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MaddeningComponent> ent, ref MapInitEvent args)
    {
        var containers = _container.GetContainingContainers((ent, Transform(ent.Owner)));

        foreach (var container in containers)
        {
            if (_mind.TryGetMind(container.Owner, out var mind, out _))
            {
                Enmadden(container.Owner, ent);
                break;
            }
        }
    }

    #endregion

    private void HandleMaddeningItemMove(Entity<MaddeningComponent> ent)
    {
        var newVictim = EntityUid.Invalid;

        if (_container.TryGetOuterContainer(ent.Owner, Transform(ent), out var container) &&
            _mind.TryGetMind(container.Owner, out _, out _))
            newVictim = container.Owner;

        if (newVictim != ent.Comp.MaddenedEntity)
        {
            Unmadden(ent.Comp.MaddenedEntity);
            ent.Comp.MaddenedEntity = null;
            Enmadden(newVictim, ent);
        }
    }

    public void Enmadden(EntityUid victim, Entity<MaddeningComponent> maddeningEnt)
    {
        var comp = maddeningEnt.Comp;
        Enmadden(victim, comp.Stinger, comp.AnnouncementText, comp.AnnouncementSender, maddeningEnt);
    }

    public void Enmadden(EntityUid victim,
        SoundSpecifier? stinger = null,
        string? announcementText = null,
        string? announcementSender = null,
        Entity<MaddeningComponent>? maddeningEnt = null)
    {
        if (!_mind.TryGetMind(victim, out var mind, out _) || !TryComp<ActorComponent>(victim, out var actor))
            return;

        if (_admin.IsAdmin(actor.PlayerSession))
            return;

        if (HasComp<MaddenedComponent>(mind))
        {
            maddeningEnt?.Comp.MaddenedEntity = victim;
            return;
        }

        EnsureComp<KillSignComponent>(victim);

        EnsureComp<MaddenedComponent>(mind);
        _role.MindAddRole(mind, "MindRoleMaddened");
        _antag.SendBriefing(victim, Loc.GetString("maddened-role-greeting"), Color.Silver, stinger);
        _adminLog.Add(LogType.AntagSelection,
            LogImpact.Extreme,
            $"{ToPrettyString(victim):player} was maddened by {ToPrettyString(maddeningEnt):entity}");

        maddeningEnt?.Comp.MaddenedEntity = victim;

        if (announcementText is null || announcementSender is null)
            return;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(announcementText),
            Loc.GetString(announcementSender),
            false,
            colorOverride: Color.DarkGray);
    }

    public void Unmadden(EntityUid? maddenedEnt)
    {
        if (maddenedEnt is null || !_mind.TryGetMind(maddenedEnt.Value, out var mind, out _))
            return;

        RemCompDeferred<KillSignComponent>(maddenedEnt.Value);
        RemCompDeferred<MaddenedComponent>(mind);
        _role.MindRemoveRole(mind, "MindRoleMaddened");
        _adminLog.Add(LogType.AntagSelection,
            LogImpact.High,
            $"{ToPrettyString(maddenedEnt):player} lost their maddened status.");
    }
}
