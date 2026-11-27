using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.NameIdentifier;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This handles logic relating to <see cref="ArtifactCrusherComponent"/>
/// </summary>
public abstract partial class SharedArtifactCrusherSystem : EntitySystem
{
    [Dependency] private SharedXenoArtifactSystem _artifact = default!;
    [Dependency] protected SharedAudioSystem AudioSystem = default!;
    [Dependency] protected SharedContainerSystem ContainerSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [Dependency] protected EntityQuery<XenoArtifactComponent> ArtifactQuery;
    [Dependency] private EntityQuery<XenoArtifactNodeComponent> _nodeQuery;
    [Dependency] private EntityQuery<NameIdentifierComponent> _nameQuery;

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
            _popup.PopupEntity(Loc.GetString("artifact-crusher-autolocks-enable"), uid);

        crusher.Crushing = true;
        crusher.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
        crusher.CrushEndTime = _timing.CurTime + crusher.CrushDuration;
        crusher.CrushingSoundEntity = AudioSystem.PlayPredicted(crusher.CrushingSound, ent, user)?.Entity ?? crusher.CrushingSoundEntity;
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
            ent.Comp.CrushingSoundEntity = AudioSystem.Stop(ent.Comp.CrushingSoundEntity);

        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Create Anomalous Shards each containing one of the nodes of an artifact
    /// </summary>
    public int MakeShards(EntityUid artifact, EntityCoordinates coords, ArtifactCrusherComponent crusher)
    {
        if (!ArtifactQuery.TryComp(artifact, out var artifactComponent) || crusher.ShardProtoId == null)
            return 0;

        var nodes = _artifact.GetAllNodes((artifact, artifactComponent));
        var lockedNodes = 0;
        foreach (var node in nodes)
        {
            if (!_nodeQuery.TryComp(node, out var nodeComp))
                continue;

            if (nodeComp.Locked == true && crusher.GetLockedNodes == false)
            {
                lockedNodes += 1;
                continue; // only get shards that have been discovered and used
            }

            var shard = PredictedSpawnAtPosition(crusher.ShardProtoId, coords);
            ContainerSystem.Insert((shard, null, null, null), crusher.OutputContainer); //spawn and place inside the crusher

            if (!ArtifactQuery.TryComp(shard, out var artifactShardComponent))
                continue;

            nodeComp.Depth = 0; //forcibly set to depth 0 otherwise the node will appear in strange places in the GUI.
            _artifact.AddNode((shard, artifactShardComponent), (node, nodeComp)); //give it the node

            if (_nameQuery.TryComp(node, out var nameComp)) // get the name identifier of the node and put it on the shard for QoL of identifying what each shard does.
            {
                if (!_nameQuery.TryComp(shard, out var shardNameComp))
                {
                    var cloneNameComp = Factory.GetComponent<NameIdentifierComponent>(); //make a clone of that node name identifier
                    cloneNameComp.Group = "XenoArtifactShard";
                    cloneNameComp.Identifier = nameComp.Identifier;
                    AddComp(shard, cloneNameComp);
                }
            }
        }

        return lockedNodes;
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
