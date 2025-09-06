using Content.Shared.Damage;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This handles logic relating to <see cref="ArtifactCrusherComponent"/>
/// </summary>
public abstract class SharedArtifactCrusherSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactCrusherComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ArtifactCrusherComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
        SubscribeLocalEvent<ArtifactCrusherComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<ArtifactCrusherComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ArtifactCrusherComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<ArtifactCrusherComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ArtifactCrusherComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnInit(Entity<ArtifactCrusherComponent> ent, ref ComponentInit args)
    {
        ent.Comp.OutputContainer = ContainerSystem.EnsureContainer<Container>(ent, ent.Comp.OutputContainerName);
    }

    private void OnStorageAfterOpen(Entity<ArtifactCrusherComponent> ent, ref StorageAfterOpenEvent args)
    {
        StopCrushing(ent);
        ContainerSystem.EmptyContainer(ent.Comp.OutputContainer);
    }

    private void OnEmagged(Entity<ArtifactCrusherComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (ent.Comp.AutoLock)
            return;

        ent.Comp.AutoLock = true;
        args.Handled = true;
        Dirty(ent);
    }

    private void OnStorageOpenAttempt(Entity<ArtifactCrusherComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (ent.Comp.AutoLock && ent.Comp.Crushing)
            args.Cancelled = true;
    }

    private void OnExamine(Entity<ArtifactCrusherComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.AutoLock ? Loc.GetString("artifact-crusher-examine-autolocks") : Loc.GetString("artifact-crusher-examine-no-autolocks"));
    }

    private void OnGetVerbs(Entity<ArtifactCrusherComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.Crushing)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var entityStorageComp) ||
            entityStorageComp.Contents.ContainedEntities.Count == 0)
            return;

        if (!_power.IsPowered(ent.Owner))
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("artifact-crusher-verb-start-crushing"),
            Priority = 2,
            Act = () => StartCrushing((ent, ent.Comp, entityStorageComp), user)
        };
        args.Verbs.Add(verb);
    }

    private void OnPowerChanged(Entity<ArtifactCrusherComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopCrushing(ent);
    }

    public void StartCrushing(Entity<ArtifactCrusherComponent, EntityStorageComponent> ent, EntityUid? user = null)
    {
        var (uid, crusher, _) = ent;

        if (crusher.Crushing)
            return;

        if (crusher.AutoLock)
            _popup.PopupPredicted(Loc.GetString("artifact-crusher-autolocks-enable"), uid, user);

        crusher.Crushing = true;
        crusher.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
        crusher.CrushEndTime = _timing.CurTime + crusher.CrushDuration;
        crusher.CrushingSoundEntity = AudioSystem.PlayPvs(crusher.CrushingSound, ent)?.Entity;
        _appearance.SetData(ent, ArtifactCrusherVisuals.Crushing, true);
        Dirty(ent, ent.Comp1);
    }

    public void StopCrushing(Entity<ArtifactCrusherComponent> ent, bool early = true)
    {
        if (!ent.Comp.Crushing)
            return;

        ent.Comp.Crushing = false;
        _appearance.SetData(ent, ArtifactCrusherVisuals.Crushing, false);

        if (early)
        {
            AudioSystem.Stop(ent.Comp.CrushingSoundEntity);
            ent.Comp.CrushingSoundEntity = null;
        }

        Dirty(ent, ent.Comp);
    }

    public virtual void FinishCrushing(Entity<ArtifactCrusherComponent, EntityStorageComponent> ent) { }

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
                FinishCrushing((uid, crusher, storage));
        }
    }
}
