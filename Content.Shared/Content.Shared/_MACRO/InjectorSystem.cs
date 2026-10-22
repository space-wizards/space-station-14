using Content.Shared.Chemisry.Components;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed partial class InjectorSystem
{
    /// <summary>
    /// Tests to see if the given target blocks injections. Returns true when blocked.
    /// </summary>
    /// <param name="user">Entity performing the injection.</param>
    /// <param name="target">Entity being injected into.</param>
    /// <returns></returns>
    public bool TryBlockInjection(EntityUid user, EntityUid target)
    {
        if (!TryComp<BlockInjectionComponent>(target, out var blockInjectionComponent))
            return false;

        _popup.PopupClient(Loc.GetString(blockInjectionComponent.FailurePopup,("name", target)), target, user);
        return true;
    }
}
