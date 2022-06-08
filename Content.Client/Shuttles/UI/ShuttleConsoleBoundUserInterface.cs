using Content.Client.Computer;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles.UI;

[UsedImplicitly]
public sealed class ShuttleConsoleBoundUserInterface : ComputerBoundUserInterface<ShuttleConsoleWindow, ShuttleConsoleBoundInterfaceState>
{
    public ShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
}
