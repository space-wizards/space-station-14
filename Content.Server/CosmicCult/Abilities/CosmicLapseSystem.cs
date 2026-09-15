using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicLapseSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnLapseAbility(Entity<CosmicActionLapseComponent> ent, ref EventCosmicLapse args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        args.Handled = true;

        var species = Comp<HumanoidProfileComponent>(args.Target).Species;
        var lapseEnt = Spawn(ent.Comp.SpawnLapse, Transform(args.Target).Coordinates);
        var doAfterArgs = new DoAfterArgs(EntityManager, lapseEnt, ent.Comp.Duration, new CosmicLapseDoAfter(), lapseEnt, args.Target)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            BreakOnMove = false,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };

        if (_container.TryGetContainer(lapseEnt, SharedEntityStorageSystem.ContainerName, out var container))
            _container.Insert(args.Target, container);

        _doAfter.TryStartDoAfter(doAfterArgs);
        _appearance.SetData(lapseEnt, CosmicLapseVisuals.Visuals, species);
        _audio.PlayPvs(action.Sfx, lapseEnt, AudioParams.Default.WithVariation(0.075f));
        Spawn(action.Vfx, Transform(lapseEnt).Coordinates);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CosmicLapsedComponent> ent, ref CosmicLapseDoAfter args)
    {
        var vfx = Spawn(ent.Comp.Vfx, Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.Sfx, vfx, AudioParams.Default.WithVariation(0.075f));

        if (_container.TryGetContainer(ent, SharedEntityStorageSystem.ContainerName, out var container))
            _container.EmptyContainer(container, true);

        QueueDel(ent);
    }
}
