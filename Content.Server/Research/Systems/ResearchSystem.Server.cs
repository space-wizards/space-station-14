using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private bool CanRun(ResearchServerComponent component)
    {
        return this.IsPowered(component.Owner, EntityManager);
    }

    private void UpdateServer(ResearchServerComponent component, int time)
    {
        if (!CanRun(component))
            return;
        ChangePointsOnServer(component.Owner, PointsPerSecond(component) * time, component);
    }

    public bool RegisterServerClient(ResearchServerComponent component, EntityUid client, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(client, ref clientComponent))
            return false;

        if (component.Clients.Contains(client))
            return false;
        component.Clients.Add(client);
        clientComponent.Server = component;
        return true;
    }

    public void UnregisterServerClient(ResearchServerComponent component, EntityUid client, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(client, ref clientComponent))
            return;

        component.Clients.Remove(client);
        clientComponent.Server = null;
    }

    public bool IsTechnologyUnlocked(ResearchServerComponent component, TechnologyPrototype prototype,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false))
            return false;
        return IsTechnologyUnlocked(databaseComponent.Owner, prototype.ID, databaseComponent);
    }

    public bool CanUnlockTechnology(ResearchServerComponent component, TechnologyPrototype technology, TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false))
            return false;

        if (!CanUnlockTechnology(databaseComponent.Owner, technology, databaseComponent) ||
            component.Points < technology.RequiredPoints ||
            IsTechnologyUnlocked(component, technology, databaseComponent))
            return false;

        return true;
    }

    public bool UnlockTechnology(ResearchServerComponent component, TechnologyPrototype prototype,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false))
            return false;

        if (!CanUnlockTechnology(component, prototype, databaseComponent))
            return false;
        var result = UnlockTechnology(databaseComponent, prototype);
        if (result)
            ChangePointsOnServer(component.Owner, -prototype.RequiredPoints, component);
        return result;
    }

    public int PointsPerSecond(ResearchServerComponent component)
    {
        var points = 0;

        if (!CanRun(component))
            return points;
        var ev = new ResearchServerGetPointsPerSecondEvent(component.Owner, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }

        return ev.Points;
    }

    public void ChangePointsOnServer(EntityUid uid, int points, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.Points += points;
        var ev = new ResearchServerPointsChangedEvent(uid, component.Points, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
    }
}
