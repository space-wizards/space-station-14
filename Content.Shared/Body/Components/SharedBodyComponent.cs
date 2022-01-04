using System;
using System.Collections.Generic;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Components;

[NetworkedComponent]
[Friend(typeof(SharedBodySystem))]
public abstract class SharedBodyComponent : Component
{
    public override string Name => "Body";

    [DataField("template", required: true)]
    public string TemplateId = default!;

    [DataField("preset", required: true)]
    public string PresetId = default!;

    [ViewVariables]
    public Dictionary<string, BodyPartSlot> SlotIds = new();

    [ViewVariables]
    public Dictionary<SharedBodyPartComponent, BodyPartSlot> Parts = new();

    [ViewVariables]
    public Container PartContainer = default!;
}

[Serializable]
[NetSerializable]
public class BodyComponentState : ComponentState
{
    public Dictionary<string, SharedBodyPartComponent> Parts;

    public BodyComponentState(Dictionary<string, SharedBodyPartComponent> parts)
    {
        Parts = parts;
    }
}
