using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Server.Station.Systems;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research;

public sealed partial class ResearchSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnServerStartup);
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
    }

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
        UnregisterServer(component);
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        RegisterServer(component);
    }

    private bool CanRun(ResearchServerComponent component)
    {
        return this.IsPowered(component.Owner, EntityManager);
    }

    private void UpdateServer(ResearchServerComponent component, int time)
    {
        if (!CanRun(component)) return;
        component.Points += PointsPerSecond(component) * time;
    }

    public bool RegisterServerClient(ResearchServerComponent component, ResearchClientComponent clientComponent)
    {
        // TODO: This is shit but I'm just trying to fix RND for now until it gets bulldozed
        if (TryComp<ResearchPointSourceComponent>(clientComponent.Owner, out var source))
        {
            if (component.PointSources.Contains(source)) return false;
            component.PointSources.Add(source);
            source.Server = component;
        }

        if (component.Clients.Contains(clientComponent)) return false;
        component.Clients.Add(clientComponent);
        clientComponent.Server = component;
        return true;
    }

    public void UnregisterServerClient(ResearchServerComponent component, ResearchClientComponent clientComponent)
    {
        if (TryComp<ResearchPointSourceComponent>(clientComponent.Owner, out var source))
        {
            component.PointSources.Remove(source);
        }

        component.Clients.Remove(clientComponent);
        clientComponent.Server = null;
    }

    public bool IsTechnologyUnlocked(ResearchServerComponent component, TechnologyPrototype prototype,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false)) return false;
        return databaseComponent.IsTechnologyUnlocked(prototype.ID);
    }

    public bool CanUnlockTechnology(ResearchServerComponent component, TechnologyPrototype technology, TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false))
            return false;

        if (!databaseComponent.CanUnlockTechnology(technology) ||
            component.Points < technology.RequiredPoints ||
            IsTechnologyUnlocked(component, technology, databaseComponent))
            return false;

        return true;
    }

    public bool UnlockTechnology(ResearchServerComponent component, TechnologyPrototype prototype,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(component.Owner, ref databaseComponent, false)) return false;

        if (!CanUnlockTechnology(component, prototype, databaseComponent)) return false;
        var result = UnlockTechnology(databaseComponent, prototype);
        if (result)
            component.Points -= prototype.RequiredPoints;
        return result;
    }

    public int PointsPerSecond(ResearchServerComponent component)
    {
        var points = 0;

        if (CanRun(component))
        {
            foreach (var source in component.PointSources)
            {
                if (CanProduce(source)) points += source.PointsPerSecond;
            }
        }

        return points;
    }
}
