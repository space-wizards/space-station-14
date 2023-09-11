using Content.Client.Computer;
using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class EmergencyConsoleClientBoundUserInterface : ComputerClientBoundUserInterface<EmergencyConsoleWindow, EmergencyConsoleBoundUserInterfaceState>
{
    public EmergencyConsoleClientBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
