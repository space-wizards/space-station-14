using Content.Client.Items;
using Content.Client.Light.Controls;
using Content.Shared.Light.Components;

namespace Content.Client.Light.EntitySystems;

/// <summary>
/// Handles the label on the light replacer
/// </summary>
public sealed class LightReplacerStatusControlSystem : EntitySystem
{
    public override void Initialize()
    {
        Subs.ItemStatus<LightReplacerComponent>(replacer => new LightReplacerStatusControl(replacer));
    }
}
