using Content.Client.Actions;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed class ActionUIController : UIController
{
    [UISystemDependency] private readonly ActionsSystem _handsSystem = default!;
    [UISystemDependency] private readonly
}
