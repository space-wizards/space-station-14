using Content.Shared.Unitology.Components;
using Content.Client.Antag;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Unitology;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class UnitologySystem : AntagStatusIconSystem<UnitologyComponent>
{


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyComponent, GetStatusIconsEvent>(GetUniIcon);
    }


    private void GetUniIcon(EntityUid uid, UnitologyComponent comp, ref GetStatusIconsEvent args)
    {
        GetStatusIcon(comp.UniStatusIcon, ref args);
    }
}
