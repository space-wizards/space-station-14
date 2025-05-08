﻿using Content.Shared.Station;

namespace Content.Client.Station;

/// <summary>
/// This handles letting the client know stations are a thing. Only really used by an admin menu.
/// </summary>
public sealed partial class StationSystem : SharedStationSystem
{
    private readonly List<(string Name, NetEntity Entity)> _stations = new();

    /// <summary>
    /// All stations that currently exist.
    /// </summary>
    /// <remarks>
    /// I'd have this just invoke an entity query, but we're on the client and the client barely knows about stations.
    /// </remarks>
    // TODO: Stations have a global PVS override now, this can probably be changed into a query.
    public IReadOnlyList<(string Name, NetEntity Entity)> Stations => _stations;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StationsUpdatedEvent>(StationsUpdated);
    }

    private void StationsUpdated(StationsUpdatedEvent ev)
    {
        _stations.Clear();
        // TODO this needs to be done in component states and with the Ensure() methods
        _stations.AddRange(ev.Stations);
    }
}
