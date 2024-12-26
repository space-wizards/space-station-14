using Content.Shared.Access.Systems;

namespace Content.Shared.Access.Components;

[RegisterComponent, Access(typeof(IdExaminableSystem))]
public sealed partial class IdExaminableComponent : Component;
