using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Mindshield.Components;
/// <summary>
/// If a player has a Mindshield they will get this component to prevent conversion (Until I find a solution, only works on round start and when a player joins.)
/// </summary>
[RegisterComponent, Access(typeof(MindShieldSystem))]
public sealed class MindShieldComponent : Component
{
}
