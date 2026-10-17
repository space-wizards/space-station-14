using Content.Shared.FixedPoint;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Random;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Server.Silicons.Laws;

public sealed partial class IonStormSystem : EntitySystem
{
    [Dependency] private SiliconLawSystem _siliconLaw = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IonLawSystem _ionLaw = default!;

    /// <summary>
    /// Triggers the ion storm event and subsequent handlers.
    /// Borg ion storm logic moved to <see cref="SiliconLawSystem"/>
    /// </summary>
    public void IonStormTarget(Entity<IonStormTargetComponent> ent)
    {
        var ev = new IonStormEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}
