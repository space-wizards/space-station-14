using Content.Shared.GPS;
using Content.Server.UserInterface;

using Robust.Server.GameObjects;

namespace Content.Server.GPS
{
    [RegisterComponent]
    public sealed class HandheldGPSComponent : Component
    {
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(GPSUiKey.Key);
    }
}
