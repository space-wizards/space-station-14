using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAETelepathicSystem : BaseXAESystem<XAETelepathicComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAETelepathicComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var component = ent.Comp;
        // try to find victims nearby
        var victims = _lookup.GetEntitiesInRange(ent, component.Range);
        foreach (var victimUid in victims)
        {
            if (!EntityManager.HasComponent<ActorComponent>(victimUid))
                continue;

            // roll if msg should be usual or drastic
            List<string> msgArr;
            if (_random.NextFloat() <= component.DrasticMessageProb && component.DrasticMessages != null)
            {
                msgArr = component.DrasticMessages;
            }
            else
            {
                msgArr = component.Messages;
            }

            // pick a random message
            var msgId = _random.Pick(msgArr);
            var msg = Loc.GetString(msgId);

            // show it as a popup, but only for the victim
            _popupSystem.PopupEntity(msg, victimUid, victimUid);
        }
    }
}
