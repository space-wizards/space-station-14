using Content.Client.Items;
using Content.Client.Light.Controls;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light.EntitySystems;

/// <summary>
/// Handles the label on the light replacer
/// </summary>
public sealed class LightReplacerStatusControlSystem : EntitySystem
{
    public static readonly ProtoId<ItemStatusPrototype> LightReplacerItemStatus = "LightReplacer";

    public override void Initialize()
    {
        Subs.ItemStatus<LightReplacerComponent>(replacer => new LightReplacerStatusControl(replacer), LightReplacerItemStatus);
    }
}
