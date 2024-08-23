using Content.Shared.Implants;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Weapons.Misc;

public sealed class COTelekinesisSystem : COSharedTelekinesisSystem
{
    [Dependency] protected readonly COSharedTelekinesisHandSystem _telekinesisHand = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string TelekinesisTag = "ExtraordTelekinesis";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<COTelekinesisComponent, ImplantImplantedEvent>(ImplantGive);
    }

    /// <summary>
    /// Gives telekinesis powers by moving it from implant to the implanted user
    /// </summary>
    public void ImplantGive(EntityUid uid, COTelekinesisComponent comp, ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted != null)
        {
            EnsureComp<COTelekinesisComponent>(ev.Implanted.Value);
            _tagSystem.TryAddTag(ev.Implanted.Value, TelekinesisTag);
        }
    }

    protected override void StartTether(Entity<COTelekinesisComponent?> ent, EntityUid user)
    {
        var coords = Transform(user).Coordinates;
        var entityUid = Spawn(AbilityPowerTelekinesis, coords);

        if (_hands.TryPickup(user, entityUid) == false)
        {
            _popup.PopupEntity(Loc.GetString("telekinesis-no-free-hands"), user);
            EntityManager.DeleteEntity(entityUid);
            return;
        }

        if (Resolve(ent, ref ent.Comp))
            _audioSystem.PlayPredicted(ent.Comp.ActivateSound, user, null);
    }

    protected override void StopTether(EntityUid? telekinesis)
    {
        if (telekinesis is not null && TryComp<COTelekinesisHandComponent>(telekinesis, out var comp))
        {
            _telekinesisHand.StopTether(telekinesis.Value, comp);
            EntityManager.DeleteEntity(telekinesis);
        }
    }
}
