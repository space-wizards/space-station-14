using Content.Shared._Offbrand.Input;
using Robust.Client.GameObjects;
using Robust.Shared.Input;

namespace Content.Client._Offbrand.Input;

public static class InputSystemExtensions
{
    extension(InputSystem input)
    {
        public OffbrandTargetZone TargetZone()
        {
            if (input.CmdStates.GetState(OffbrandKeyFunctions.AimHigh) == BoundKeyState.Down)
                return OffbrandTargetZone.High;
            if (input.CmdStates.GetState(OffbrandKeyFunctions.AimLow) == BoundKeyState.Down)
                return OffbrandTargetZone.Low;

            return OffbrandTargetZone.Mid;
        }
    }
}
