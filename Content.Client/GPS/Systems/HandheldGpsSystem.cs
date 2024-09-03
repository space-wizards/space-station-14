using Content.Client.GPS.UI;
using Content.Client.Items;
using Content.Shared.Examine;
using Content.Shared.GPS.Components;

namespace Content.Client.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<HandheldGPSComponent>(ent => new HandheldGpsStatusControl(ent));
    }
}
