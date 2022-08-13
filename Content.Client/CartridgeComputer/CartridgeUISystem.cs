using System.Diagnostics.CodeAnalysis;
using Content.Shared.CartridgeComputer;

namespace Content.Client.CartridgeComputer;

public sealed class CartridgeUISystem : EntitySystem
{
    public bool TryGetUIComponent(EntityUid cartridgeUid, [NotNullWhen(true)] out CartridgeUIComponent? component)
    {
        component = null;
        return Resolve(cartridgeUid, ref component);
    }

    public bool TryGetCartridgeComponent(EntityUid cartridgeUid, [NotNullWhen(true)] out CartridgeComponent? component)
    {
        component = null;
        return Resolve(cartridgeUid, ref component);
    }
}
