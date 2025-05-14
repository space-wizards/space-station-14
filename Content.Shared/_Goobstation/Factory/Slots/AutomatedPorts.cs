// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Factory.Slots;

/// <summary>
/// Adds no item I/O, only enables signal ports.
/// </summary>
public sealed partial class AutomatedPorts : AutomationSlot
{
    [DataField]
    public ProtoId<SinkPortPrototype>[] Sinks = [];

    [DataField]
    public ProtoId<SourcePortPrototype>[] Sources = [];

    public override void AddPorts()
    {
        base.AddPorts();

        _device.EnsureSinkPorts(Owner, Sinks);
        _device.EnsureSourcePorts(Owner, Sources);
    }

    public override void RemovePorts()
    {
        base.RemovePorts();

        foreach (var port in Sinks)
        {
            _device.RemoveSinkPort(Owner, port);
        }
        foreach (var port in Sources)
        {
            _device.RemoveSourcePort(Owner, port);
        }
    }
}
