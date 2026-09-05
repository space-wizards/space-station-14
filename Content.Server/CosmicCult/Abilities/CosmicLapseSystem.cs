using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicLapseSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly ProtoId<PolymorphPrototype> HumanLapse = "CosmicLapseMobHuman";

    [SubscribeLocalEvent]
    private void OnCosmicLapse(Entity<CosmicCultistComponent> ent, ref EventCosmicLapse args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        args.Handled = true;
        var tgtpos = Transform(args.Target).Coordinates;
        var species = Comp<HumanoidProfileComponent>(args.Target).Species;
        var polymorphId = "CosmicLapseMob" + species;

        Spawn(action.Vfx, tgtpos);
        _audio.PlayPvs(action.Sfx, ent, AudioParams.Default.WithVariation(0.1f));
        _popup.PopupEntity(Loc.GetString("cosmicability-lapse-success", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);

        if (_prototype.HasIndex<PolymorphPrototype>(polymorphId))
            _polymorph.PolymorphEntity(args.Target, polymorphId);
        else
            _polymorph.PolymorphEntity(args.Target, HumanLapse);
    }
}
