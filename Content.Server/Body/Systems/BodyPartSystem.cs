using Content.Shared.Body.Components;
using Content.Shared.Body.Systems.Part;
using Content.Shared.Random.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.Body.Systems;

public class BodyPartSystem : SharedBodyPartSystem
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

    protected override void OnRemoveMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent part)
    {
        base.OnRemoveMechanism(uid, mechanism, part);

        mechanism.Owner.RandomOffset(0.25f);
    }

    /// <summary>
    ///     Gibs the body part.
    /// </summary>
    public void Gib(EntityUid uid,
        SharedBodyPartComponent? part=null)
    {
        if (!Resolve(uid, ref part))
            return;

        foreach (var mechanism in part.Mechanisms)
        {
            RemoveMechanism(uid, mechanism, part);
        }
    }
}
