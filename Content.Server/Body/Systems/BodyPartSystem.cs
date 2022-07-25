using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems.Part;
using Content.Shared.Random.Helpers;

namespace Content.Server.Body.Systems;

public sealed class BodyPartSystem : SharedBodyPartSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBodyPartComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, SharedBodyPartComponent component, ComponentStartup args)
    {
        // This is ran in Startup as entities spawned in Initialize
        // are not synced to the client since they are assumed to be
        // identical on it
        foreach (var mechanismId in component.InitialMechanisms)
        {
            var entity = Spawn(mechanismId, Transform(uid).MapPosition);

            if (!TryComp(entity, out MechanismComponent? mechanism))
            {
                Logger.Error($"Entity {mechanismId} does not have a {nameof(MechanismComponent)} component.");
                continue;
            }

            TryAddMechanism(uid, mechanism, true, component);
        }
    }

    #region Overrides

    protected override void OnAddMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent part)
    {
        base.OnRemoveMechanism(uid, mechanism, part);

        part.MechanismContainer.Insert(mechanism.Owner);
    }


    protected override void OnRemoveMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent part)
    {
        base.OnRemoveMechanism(uid, mechanism, part);

        part.MechanismContainer.Remove(mechanism.Owner);
        mechanism.Owner.RandomOffset(0.25f);
    }

    #endregion

    /// <summary>
    ///     Gibs the body part.
    /// </summary>
    public HashSet<EntityUid> Gib(EntityUid uid, SharedBodyPartComponent? part = null)
    {
        var gibs = new HashSet<EntityUid>();
        if (!Resolve(uid, ref part))
            return gibs;

        foreach (var mechanism in part.Mechanisms)
        {
            gibs.Add(part.Owner);
            RemoveMechanism(uid, mechanism, part);
        }

        return gibs;
    }
}
