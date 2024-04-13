using Content.Client.GPS.UI;
using Content.Client.Items;
using Content.Shared.Tools.Components;

namespace Content.Client.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<HandheldGpsComponent>(ent => new HandheldGpsStatusControl(ent));
    }
}
