using Content.Server.Pinpointer;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class NotifyOnNonFunctionalSystem : SharedNotifyOnNonFunctionalSystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private NavMapSystem _navMap = default!;

    protected override void AlertRadio(Entity<NotifyOnNonFunctionalComponent> ent, string locString)
    {
        //if (!ent.Comp.IsOn || !ent.Comp.IsPowered)
        //    return;

        var message = Loc.GetString(
            locString,
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent.Owner)))
        );
        _radio.SendRadioMessage(ent.Owner, message, ent.Comp.RadioChannel, ent.Owner);
    }
}
