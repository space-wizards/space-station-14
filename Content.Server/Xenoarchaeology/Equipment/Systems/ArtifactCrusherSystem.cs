using Content.Server.Body.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Equipment;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <inheritdoc/>
public sealed class ArtifactCrusherSystem : SharedArtifactCrusherSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactCrusherComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ArtifactCrusherComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnGetVerbs(Entity<ArtifactCrusherComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.Crushing)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var entityStorageComp) || entityStorageComp.Contents.ContainedEntities.Count == 0)
            return;

        if (!this.IsPowered(ent, EntityManager))
            return;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("artifact-crusher-verb-start-crushing"),
            Priority = 2,
            Act = () => StartCrushing((ent, ent.Comp, entityStorageComp))
        };
        args.Verbs.Add(verb);
    }

    private void OnPowerChanged(Entity<ArtifactCrusherComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopCrushing(ent);
    }

    public void StartCrushing(Entity<ArtifactCrusherComponent, EntityStorageComponent> ent)
    {
        var (_, crusher, _) = ent;

        if (crusher.Crushing)
            return;

        crusher.Crushing = true;
        crusher.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
        crusher.CrushEndTime = _timing.CurTime + crusher.CrushDuration;
        crusher.CrushingSoundEntity = AudioSystem.PlayPvs(crusher.CrushingSound, ent);
        Appearance.SetData(ent, ArtifactCrusherVisuals.Crushing, true);
        Dirty(ent, ent.Comp1);
    }

    public void FinishCrushing(Entity<ArtifactCrusherComponent, EntityStorageComponent> ent)
    {
        var (_, crusher, storage) = ent;
        StopCrushing((ent, ent.Comp1), false);
        AudioSystem.PlayPvs(crusher.CrushingCompleteSound, ent);
        crusher.CrushingSoundEntity = null;
        Dirty(ent, ent.Comp1);

        var contents = new ValueList<EntityUid>(storage.Contents.ContainedEntities);
        var coords = Transform(ent).Coordinates;
        foreach (var contained in contents)
        {
            if (crusher.CrushingWhitelist.IsValid(contained, EntityManager))
            {
                var amount = _random.Next(crusher.MinFragments, crusher.MaxFragments);
                var stacks = _stack.SpawnMultiple(crusher.FragmentStackProtoId, amount, coords);
                foreach (var stack in stacks)
                {
                    ContainerSystem.Insert((stack, null, null, null), crusher.OutputContainer);
                }
                _artifact.ForceActivateArtifact(contained);
            }

            if (!TryComp<BodyComponent>(contained, out var body))
                Del(contained);

            var gibs = _body.GibBody(contained, body: body, gibOrgans: true, deleteBrain: true);
            foreach (var gib in gibs)
            {
                ContainerSystem.Insert((gib, null, null, null), crusher.OutputContainer);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArtifactCrusherComponent, EntityStorageComponent>();
        while (query.MoveNext(out var uid, out var crusher, out var storage))
        {
            if (!crusher.Crushing)
                continue;

            if (crusher.NextSecond < _timing.CurTime)
            {
                var contents = new ValueList<EntityUid>(storage.Contents.ContainedEntities);
                foreach (var contained in contents)
                {
                    _damageable.TryChangeDamage(contained, crusher.CrushingDamage);
                }
                crusher.NextSecond += TimeSpan.FromSeconds(1);
                Dirty(uid, crusher);
            }

            if (crusher.CrushEndTime < _timing.CurTime)
            {
                FinishCrushing((uid, crusher, storage));
            }
        }
    }
}
