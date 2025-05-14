// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking.Events;
using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Construction;

public sealed class FlatpackSignalSystem : EntitySystem
{
    public static readonly ProtoId<SinkPortPrototype> OnPort = "On";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlatpackCreatorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(Entity<FlatpackCreatorComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != OnPort)
            return;

        // supercode has no API so we have to do this
        var ev = new FlatpackCreatorStartPackBuiMessage();
        RaiseLocalEvent(ent, ev);
    }
}
