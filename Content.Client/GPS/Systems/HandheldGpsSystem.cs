using Content.Client.GPS.UI;
using Content.Client.Items;
using Content.Shared.GPS.Components;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Client.GPS.Systems;

public sealed partial class HandheldGpsSystem : EntitySystem
{
    public static readonly ProtoId<ItemStatusPrototype> GpsItemStatus = "GPS";

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<HandheldGPSComponent>(ent => new HandheldGpsStatusControl(ent), GpsItemStatus);
    }
}
