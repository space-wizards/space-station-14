using Content.Shared.Roles;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles;

namespace Content.Server.Roles;

public sealed class RoleSystem : SharedRoleSystem
{
    public override void Initialize()
    {
        // TODO make roles entities
        base.Initialize();

        SubscribeAntagEvents<DragonRoleComponent>();
        SubscribeAntagEvents<InitialInfectedRoleComponent>();
        SubscribeAntagEvents<NinjaRoleComponent>();
        SubscribeAntagEvents<NukeopsRoleComponent>();
        SubscribeAntagEvents<RevolutionaryRoleComponent>();
        SubscribeAntagEvents<SubvertedSiliconRoleComponent>();
        SubscribeAntagEvents<TraitorRoleComponent>();
        SubscribeAntagEvents<ZombieRoleComponent>();
        SubscribeAntagEvents<ThiefRoleComponent>();

        // TODO: I am in fact coding mind role entities, but until we get there, we have to live with... *this*
        SubscribeMindRoleEvents<DragonRoleComponent>();
        SubscribeMindRoleEvents<InitialInfectedRoleComponent>();
        SubscribeMindRoleEvents<NinjaRoleComponent>();
        SubscribeMindRoleEvents<NukeopsRoleComponent>();
        SubscribeMindRoleEvents<RevolutionaryRoleComponent>();
        SubscribeMindRoleEvents<SubvertedSiliconRoleComponent>();
        SubscribeMindRoleEvents<TraitorRoleComponent>();
        SubscribeMindRoleEvents<ZombieRoleComponent>();
        SubscribeMindRoleEvents<ThiefRoleComponent>();
        SubscribeMindRoleEvents<GhostRoleMarkerRoleComponent>();
        SubscribeMindRoleEvents<ObserverRoleComponent>();
    }

    public string? MindGetBriefing(EntityUid? mindId)
    {
        if (mindId == null)
            return null;

        var ev = new GetBriefingEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        return ev.Briefing;
    }
}

/// <summary>
/// Event raised on the mind to get its briefing.
/// Handlers can either replace or append to the briefing, whichever is more appropriate.
/// </summary>
[ByRefEvent]
public sealed class GetBriefingEvent
{
    public string? Briefing;

    public GetBriefingEvent(string? briefing = null)
    {
        Briefing = briefing;
    }

    /// <summary>
    /// If there is no briefing, sets it to the string.
    /// If there is a briefing, adds a new line to separate it from the appended string.
    /// </summary>
    public void Append(string text)
    {
        if (Briefing == null)
        {
            Briefing = text;
        }
        else
        {
            Briefing += "\n" + text;
        }
    }
}
