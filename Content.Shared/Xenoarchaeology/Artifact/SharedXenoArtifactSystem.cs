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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoArtifactComponent, ComponentStartup>(OnStartup);

        InitializeNode();
        InitializeUnlock();
        InitializeXAT();
        InitializeXAE();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateUnlock(frameTime);
    }

    /// <summary> As all artifacts have to contain nodes - we ensure that they are containers. </summary>
    private void OnStartup(Entity<XenoArtifactComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NodeContainer = _container.EnsureContainer<Container>(ent, XenoArtifactComponent.NodeContainerId);
    }

    public void SetSuppressed(Entity<XenoArtifactComponent> ent, bool val)
    {
        if (ent.Comp.Suppressed == val)
            return;

        ent.Comp.Suppressed = val;
        Dirty(ent);
    }
}
