using Content.Shared.Body.Components;
using Content.Shared.Body.Systems.Part;
using System.Linq;

namespace Content.Server.Body.Systems;

public sealed class BodyPartSystem : SharedBodyPartSystem
{
    protected override void OnComponentInit(EntityUid uid, SharedBodyPartComponent component, ComponentInit args)
    {
        base.OnComponentInit(uid, component, args);

        foreach (var mechanismId in component.InitialMechanisms)
        {
            var entity = Spawn(mechanismId, Transform(uid).Coordinates);

            if (!TryComp<MechanismComponent>(entity, out var mechanism))
            {
                Logger.Error($"Entity {mechanismId} does not have a {nameof(MechanismComponent)} component.");
                continue;
            }

            TryAddMechanism(uid, mechanism, true, component);
        }
    }

    /// <summary>
    ///     Gibs the body part.
    /// </summary>
    public HashSet<EntityUid> Gib(EntityUid uid, SharedBodyPartComponent? part = null)
    {
        var gibs = new HashSet<EntityUid>();
        if (!Resolve(uid, ref part))
            return gibs;

        foreach (var mechanism in GetAllMechanisms(uid, part).ToArray())
        {
            if (TryRemoveMechanism(uid, mechanism, part))
                gibs.Add(mechanism.Owner);
        }

        return gibs;
    }
}
