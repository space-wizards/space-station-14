using System.Linq;
using Content.Shared.Body;
using Content.Shared.Interaction;
using Content.Shared.RussStation.Surgery;
using Content.Shared.RussStation.Surgery.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.RussStation.Surgery;

public sealed partial class SurgerySystem
{
    private void InitializeOrgans()
    {
        // Organ-specific subscriptions can go here if needed.
    }

    private void TryInsertOrgan(EntityUid surgeon, EntityUid patient, EntityUid organ)
    {
        if (!TryComp<OrganComponent>(organ, out var organComp))
            return;

        if (!TryComp<BodyComponent>(patient, out var body) || body.Organs == null)
            return;

        // Block if the patient already has an organ of the same category
        if (organComp.Category != null)
        {
            foreach (var existing in body.Organs.ContainedEntities)
            {
                if (TryComp<OrganComponent>(existing, out var existingOrgan) &&
                    existingOrgan.Category == organComp.Category)
                {
                    _popup.PopupEntity(
                        Loc.GetString("surgery-organ-already-exists", ("organ", MetaData(organ).EntityName)),
                        patient, surgeon);
                    return;
                }
            }
        }

        _container.Insert(organ, body.Organs, force: true);
        _popup.PopupEntity(
            Loc.GetString("surgery-organ-inserted", ("organ", MetaData(organ).EntityName)),
            patient);
    }

    private void OpenOrganRemovalMenu(EntityUid? surgeon, EntityUid patient)
    {
        if (surgeon == null || !TryComp<ActorComponent>(surgeon, out var actor))
            return;

        if (!TryComp<BodyComponent>(patient, out var body) || body.Organs == null)
            return;

        var organs = new List<(NetEntity, string, string?)>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            // Skip limbs, only show internal organs
            if (!TryComp<OrganComponent>(organ, out var organComp))
                continue;

            if (IsLimbCategory(organComp.Category))
                continue;

            var meta = MetaData(organ);
            organs.Add((GetNetEntity(organ), meta.EntityName, meta.EntityPrototype?.ID));
        }

        if (organs.Count > 0)
            RaiseNetworkEvent(new OpenOrganMenuEvent(GetNetEntity(patient), organs), actor.PlayerSession);
    }

    private void OnOrganSelected(SelectOrganEvent ev, EntitySessionEventArgs args)
    {
        // Validate sender
        if (args.SenderSession.AttachedEntity is not { } surgeon)
            return;

        if (!TryGetEntity(ev.Target, out var patient) || !TryGetEntity(ev.OrganId, out var organ))
            return;

        // Validate: surgeon must be in range and have active surgery on this patient
        if (!_interaction.InRangeUnobstructed(surgeon, patient.Value))
            return;

        if (!TryComp<ActiveSurgeryComponent>(patient.Value, out var active) || active.Surgeon != surgeon)
            return;

        if (!TryComp<BodyComponent>(patient.Value, out var body) || body.Organs == null)
            return;

        if (!body.Organs.ContainedEntities.Contains(organ.Value))
            return;

        _container.Remove(organ.Value, body.Organs);
        _xform.DropNextTo(organ.Value, patient.Value);

        _popup.PopupEntity(
            Loc.GetString("surgery-organ-removed", ("organ", MetaData(organ.Value).EntityName)),
            patient.Value);
    }

    private static bool IsLimbCategory(ProtoId<OrganCategoryPrototype>? category)
    {
        if (category == null)
            return false;

        return category.Value.Id is
            "Torso" or "Head" or
            "ArmLeft" or "ArmRight" or
            "HandLeft" or "HandRight" or
            "LegLeft" or "LegRight" or
            "FootLeft" or "FootRight";
    }
}
