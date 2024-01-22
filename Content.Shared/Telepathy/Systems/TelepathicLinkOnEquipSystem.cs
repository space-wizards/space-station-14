using Content.Shared.Inventory.Events;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Telepathy.Components;

namespace Content.Shared.Telepathy.Systems;

public sealed class TelepathicLinkOnEquipSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelepathicLinkOnEquipComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<TelepathicLinkOnEquipComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<TelepathicLinkOnEquipComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<SpeechComponent>(ent).SpeechVerb = "Ghost";
        EnsureComp<TargetedTelepathyComponent>(ent).Target = args.Equipee;
    }

    private void OnGotUnequipped(Entity<TelepathicLinkOnEquipComponent> ent, ref GotUnequippedEvent args)
    {
        RemComp<SpeechComponent>(ent);
        RemComp<TargetedTelepathyComponent>(ent);
    }
}
