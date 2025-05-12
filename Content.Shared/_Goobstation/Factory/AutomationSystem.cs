// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Factory.Slots;

namespace Content.Shared._Goobstation.Factory;

public sealed class AutomationSystem : EntitySystem
{
    private EntityQuery<AutomationSlotsComponent> _slotsQuery;
    private EntityQuery<AutomatedComponent> _automatedQuery;

    public override void Initialize()
    {
        base.Initialize();

        _slotsQuery = GetEntityQuery<AutomationSlotsComponent>();
        _automatedQuery = GetEntityQuery<AutomatedComponent>();

        SubscribeLocalEvent<AutomationSlotsComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<AutomatedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutomatedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<AutomationSlotsComponent> ent, ref ComponentInit args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            slot.Owner = ent;
            slot.Initialize();
        }
    }

    private void OnMapInit(Entity<AutomatedComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<AutomationSlotsComponent>(ent, out var comp))
            return;

        foreach (var slot in comp.Slots)
        {
            slot.AddPorts();
        }
    }

    private void OnShutdown(Entity<AutomatedComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<AutomationSlotsComponent>(ent, out var comp))
            return;

        foreach (var slot in comp.Slots)
        {
            slot.RemovePorts();
        }
    }

    #region Public API

    public AutomationSlot? GetSlot(Entity<AutomationSlotsComponent?> ent, string port, bool input)
    {
        // entity has no automation slots to begin with
        if (!_slotsQuery.Resolve(ent, ref ent.Comp, false))
            return null;

        // automation isn't enabled
        if (!_automatedQuery.HasComp(ent))
            return null;

        foreach (var slot in ent.Comp.Slots)
        {
            string? id = input ? slot.Input : slot.Output;
            if (id == port)
                return slot;
        }

        return null;
    }

    public bool HasSlot(Entity<AutomationSlotsComponent?> ent, string port, bool input)
    {
        return GetSlot(ent, port, input) != null;
    }

    #endregion
}
