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
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] protected IRobustRandom RobustRandom = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    // TODO: This solution is kind of a band-aid to slip between two problems:
    // * iterating over EntityPrototypes is slow and bad idea at runtime
    // * having pre-cached collections of EntityPrototypes with every filter we would possibly need seems like clogging memory
    // We should invent other way of doing this. Giving more generic and simpler API for such caches
    // could lead to wider usage and indirectly will clog memory with overly-specific caches, so it is not advised.
    /// <summary> Cached EntProtoIds of all XenoArtifactEffect prototypes. Used for text hints. </summary>
    public readonly HashSet<string> EffectPrototypeIds = [];

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
        EffectPrototypeIds.Clear();

        foreach (var entityPrototype in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            // all effect prototypes are marked as nodes, as nodes are born from those prototypes
            if (entityPrototype.HasComp<XenoArtifactNodeComponent>(Factory) && !entityPrototype.Abstract)
                EffectPrototypeIds.Add(entityPrototype.ID);
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
