using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This handles <see cref="SolutionContainerMixerComponent"/>
/// </summary>
public abstract class SharedSolutionContainerMixerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChemicalReactionSystem _chemicalReaction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionContainerMixerComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<SolutionContainerMixerComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
    }

    private void OnActivateInWorld(Entity<SolutionContainerMixerComponent> entity, ref ActivateInWorldEvent args)
    {
        TryStartMix(entity, args.User);
    }

    private void OnRemoveAttempt(Entity<SolutionContainerMixerComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID == ent.Comp.ContainerId && ent.Comp.Mixing)
            args.Cancel();
    }

    protected virtual bool HasPower(Entity<SolutionContainerMixerComponent> entity)
    {
        return true;
    }

    public void TryStartMix(Entity<SolutionContainerMixerComponent> entity, EntityUid? user)
    {
        var (uid, comp) = entity;
        if (comp.Mixing)
            return;

        if (!HasPower(entity))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-no-power"), entity, user.Value);
            return;
        }

        if (!_container.TryGetContainer(uid, comp.ContainerId, out var container) || container.Count == 0)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-popup-nothing-to-mix"), entity, user.Value);
            return;
        }

        comp.Mixing = true;
        if (_net.IsServer)
            comp.MixingSoundEntity = _audio.PlayPvs(comp.MixingSound, entity, comp.MixingSound?.Params.WithLoop(true));
        comp.MixTimeEnd = _timing.CurTime + comp.MixDuration;
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, true);
        Dirty(uid, comp);
    }

    public void StopMix(Entity<SolutionContainerMixerComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        _audio.Stop(comp.MixingSoundEntity);
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, false);
        comp.Mixing = false;
        comp.MixingSoundEntity = null;
        Dirty(uid, comp);
    }

    public void FinishMix(Entity<SolutionContainerMixerComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        StopMix(entity);

        if (!TryComp<ReactionMixerComponent>(entity, out var reactionMixer)
            || !_container.TryGetContainer(uid, comp.ContainerId, out var container))
            return;

        foreach (var ent in container.ContainedEntities)
        {
            if (!_solution.TryGetFitsInDispenser(ent, out var solution))
                continue;

            _chemicalReaction.FullyReactSolution(solution, ent, solution.MaxVolume, reactionMixer);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionContainerMixerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Mixing)
                continue;

            if (_timing.CurTime < comp.MixTimeEnd)
                continue;

            FinishMix((uid, comp));
        }
    }
}
