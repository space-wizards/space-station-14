using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact;

/// <summary>
/// Handles all logic for generating and facilitating interactions with XenoArtifacts
/// </summary>
public abstract partial class SharedXenoArtifactSystem : EntitySystem
{
    private static readonly EntProtoId ArtifactEffectBaseProtoId = "BaseXenoArtifactEffect";

    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public HashSet<string> EffectPrototypes = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<XenoArtifactComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<XenoArtifactComponent, ArtifactSelfActivateEvent>(OnSelfActivate);

        InitializeNode();
        InitializeUnlock();
        InitializeXAT();
        InitializeXAE();

        ReloadEffectCache();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        ReloadEffectCache();
    }

    private void ReloadEffectCache()
    {
        EffectPrototypes.Clear();

        foreach (var entityPrototype in PrototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (entityPrototype is { Abstract: false, Parents: not null }
                && Array.IndexOf(entityPrototype.Parents, ArtifactEffectBaseProtoId.Id) != -1)
                EffectPrototypes.Add(entityPrototype.ID);
        }
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateUnlock(frameTime);
    }

    /// <summary> As all artifacts have to contain nodes - we ensure that they are containers. </summary>
    private void OnStartup(Entity<XenoArtifactComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ent.Comp.SelfActivateAction);
        ent.Comp.NodeContainer = _container.EnsureContainer<Container>(ent, XenoArtifactComponent.NodeContainerId);
    }

    private void OnSelfActivate(Entity<XenoArtifactComponent> ent, ref ArtifactSelfActivateEvent args)
    {
        args.Handled = TryActivateXenoArtifact(ent, ent, null, Transform(ent).Coordinates, false);
    }

    public void SetSuppressed(Entity<XenoArtifactComponent> ent, bool val)
    {
        if (ent.Comp.Suppressed == val)
            return;

        ent.Comp.Suppressed = val;
        Dirty(ent);
    }
}
