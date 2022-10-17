using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildrenOfType(EntityUid? bodyId, BodyPartType type, BodyComponent? body = null)
    {
        foreach (var part in GetBodyChildren(bodyId, body))
        {
            if (part.Component.PartType == type)
                yield return part;
        }
    }

    public bool BodyHasChildOfType(EntityUid? bodyId, BodyPartType type, BodyComponent? body = null)
    {
        return GetBodyChildrenOfType(bodyId, type, body).Any();
    }

    public bool BodyHasChild(
        EntityUid? parentId,
        EntityUid? childId,
        BodyComponent? parent = null,
        BodyPartComponent? child = null)
    {
        if (parentId == null ||
            !Resolve(parentId.Value, ref parent, false) ||
            childId == null ||
            !Resolve(childId.Value, ref child, false))
            return false;

        return child.ParentSlot?.Child == parentId;
    }
}
