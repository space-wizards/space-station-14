using Content.Server.Access.Systems;

namespace Content.Server.Access.Components;

[RegisterComponent, Access(typeof(IdExaminableSystem))]
public sealed class IdExaminableComponent : Component
{
}
