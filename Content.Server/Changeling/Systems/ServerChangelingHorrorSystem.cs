using System.Linq;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Server.Changeling.Systems;

public sealed partial class ServerChangelingHorrorSystem : ChangelingHorrorSystem
{
    [Dependency] private ChangelingIdentitySystem _identitySystem = default!;
    [Dependency] private ChangelingTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stuns = default!;
    [Dependency] private SharedPopupSystem _popups = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ChangelingHorrorComponent, ChangelingIdentityComponent>();
        var curtime = Timing.CurTime;

        while (enumerator.MoveNext(out var uid, out var comp, out var identities))
        {
            // calculate the timeout
            if (curtime > comp.EndTime)
            {
                if (comp.LastIdentity != null && identities.ConsumedIdentities.Any(k => k.Identity == comp.LastIdentity.Value))
                {
                    // we force the transformation, this will call all cleanup code in OnBeforeTransform
                    var tComp = EnsureComp<ChangelingTransformComponent>(uid);
                    _transform.TransformIntoNow((uid, tComp), comp.LastIdentity.Value);
                }
                else
                {
                    // we try to find a non-horror identity
                    var id = identities.ConsumedIdentities.Where(k => k.Identity != null && _identitySystem.IsSafe(k.Identity.Value));

                    if (!id.Any())
                        continue;

                    var identity = id.First();

                    if (!identity.Identity.HasValue)
                        continue;

                    // we force the transformation, this will call all cleanup code in OnBeforeTransform
                    var tComp = EnsureComp<ChangelingTransformComponent>(uid);
                    _transform.TransformIntoNow((uid, tComp), identity.Identity.Value);
                }
                var selfMessage = Loc.GetString("changeling-horror-force-transform-self", ("user", Identity.Name(uid, EntityManager)));
                var othersMessage = Loc.GetString("changeling-horror-force-transform-others", ("user", Identity.Name(uid, EntityManager)));
                _popups.PopupEntity(
                selfMessage,
                othersMessage,
                uid,
                uid,
                PopupType.MediumCaution);

                // we apply a stun penality, you should transform back yourself!
                _stuns.TryAddStunDuration(uid, comp.StunTime);
                _stuns.TryKnockdown(uid, comp.StunTime);
            }
        }
    }
}
