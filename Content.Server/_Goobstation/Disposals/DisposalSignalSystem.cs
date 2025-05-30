// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Disposal.Unit;
using Content.Shared.Disposal.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Disposals;

public sealed class DisposalSignalSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposalUnitSystem _disposal = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public static readonly ProtoId<SinkPortPrototype> FlushPort = "DisposalFlush";
    public static readonly ProtoId<SinkPortPrototype> EjectPort = "DisposalEject";
    public static readonly ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(Entity<DisposalUnitComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == FlushPort)
            _disposal.ToggleEngage(ent, ent);
        else if (args.Port == EjectPort)
            _disposal.TryEjectContents(ent, ent);
        else if (args.Port == TogglePort)
            _power.TogglePower(ent);
    }
}
