using Content.Server.Movement.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Movement.Components;

[RegisterComponent, ComponentProtoName("Jetpack"), Friend(typeof(JetpackSystem))]
public sealed class JetpackComponent : Component
{
    [ViewVariables] [DataField("active")] public bool Active { get; set; } = true;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("speed")]
    public float Speed = 5f;


}
