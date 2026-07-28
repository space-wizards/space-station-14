using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Robust.Shared.Containers;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for adding or removing components with a mindshield implant
/// as well as checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RevolutionaryRuleSystem _revolutionary = default!; // DS14

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(OnMindShieldMapInit); // DS14
        SubscribeLocalEvent<MindShieldImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<MindShieldImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    // DS14-start
    private void OnMindShieldMapInit(
        EntityUid uid,
        MindShieldComponent component,
        MapInitEvent args)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemCompDeferred<MindShieldComponent>(uid);
            return;
        }

        if (HasComp<RevolutionaryComponent>(uid))
        {
            _revolutionary.Deconvert(
                uid,
                stun: true,
                showPopup: true,
                showEui: false,
                "Mindshield component added");
        }
    }
    // DS14-end

    private void OnImplantImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent ev)
    {
        EnsureComp<MindShieldComponent>(ev.Implanted);
        var unitologyEvent = new UnitologyMindShieldAddedEvent();
        RaiseLocalEvent(ev.Implanted, ref unitologyEvent);
        MindShieldRemovalCheck(ev.Implanted, ev.Implant);
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    private void MindShieldRemovalCheck(EntityUid implanted, EntityUid implant)
    {
        if (HasComp<HeadRevolutionaryComponent>(implanted))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), implanted);
            RemComp<MindShieldComponent>(implanted); // DS14
            QueueDel(implant);
            return;
        }

        // DS14-start
        _revolutionary.Deconvert(
            implanted,
            stun: true,
            showPopup: true,
            showEui: false,
            "implanted with a Mindshield");
        // DS14-end
    }

    private void OnImplantRemoved(Entity<MindShieldImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<MindShieldComponent>(args.Implanted);
    }
}

