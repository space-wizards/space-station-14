using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.SubFloor;

[RegisterComponent]
[NetworkedComponent]
public class TrayScannerComponent : Component
{
    [ViewVariables]
    public bool Toggled { get; set; }

    // this should always be rounded
    [ViewVariables]
    public Vector2 LastLocation { get; set; }

    // range of the scanner itself
    [DataField("range")]
    public float Range { get; set; } = 0.5f;

    // exclude entities that are not the set
    // of entities in range & entities already revealed
    [ViewVariables]
    public HashSet<EntityUid> RevealedSubfloors = new();
}

[Serializable, NetSerializable]
public sealed class TrayScannerState : ComponentState
{
    public bool Toggled { get; }

    public TrayScannerState(bool toggle)
    {
        Toggled = toggle;
    }
}
