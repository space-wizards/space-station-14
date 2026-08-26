using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicLapseSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private PopupSystem _popup = default!;

    private static readonly ProtoId<PolymorphPrototype> HumanLapse = "CosmicLapseMobHuman";

    [SubscribeLocalEvent]
    private void OnCosmicLapse(Entity<CosmicCultistComponent> uid, ref EventCosmicLapse action)
    {
        if (action.Handled || HasComp<CosmicShuntedOriginComponent>(action.Target) || HasComp<CosmicCultistComponent>(action.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-generic-fail"), uid, uid);
            return;
        }
        action.Handled = true;
        var tgtpos = Transform(action.Target).Coordinates;
        Spawn(uid.Comp.LapseVfx, tgtpos);
        _popup.PopupEntity(Loc.GetString("cosmicability-lapse-success", ("target", Identity.Entity(action.Target, EntityManager))), uid, uid);
        var species = Comp<HumanoidProfileComponent>(action.Target).Species;
        var polymorphId = "CosmicLapseMob" + species;

        if (_prototype.HasIndex<PolymorphPrototype>(polymorphId))
            _polymorph.PolymorphEntity(action.Target, polymorphId);
        else
            _polymorph.PolymorphEntity(action.Target, HumanLapse);
    }
}
