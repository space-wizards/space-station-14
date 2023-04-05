using Content.Server.Access.Systems;
using Content.Shared.Access.Components;

namespace Content.Server.Access.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedIdCardConsoleComponent))]
[Access(typeof(IdCardConsoleSystem))]
public sealed class IdCardConsoleComponent : SharedIdCardConsoleComponent
{
}
