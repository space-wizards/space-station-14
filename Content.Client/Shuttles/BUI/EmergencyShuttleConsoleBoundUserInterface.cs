using Content.Client.Computer;
using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class EmergencyShuttleConsoleBoundUserInterface : ComputerBoundUserInterface<EmergencyShuttleConsoleWindow, EmergencyShuttleConsoleBoundUserInterfaceState>
{
    public EmergencyShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
}
