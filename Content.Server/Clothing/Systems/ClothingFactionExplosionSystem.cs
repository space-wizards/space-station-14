using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Content.Server.Chat.Managers;
using Content.Server.Administration.Managers;
using Content.Server.Clothing.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Clothing.Systems;

public sealed class ClothingFactionExplosionSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingFactionExplosionComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingFactionExplosionComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, ClothingFactionExplosionComponent component, GotEquippedEvent args)
    {
        SoundSystem.Play("/Audio/Effects/ame_overloading_admin_alert.ogg", Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), AudioParams.Default.WithVolume(-4f));
        DoExplode(uid);
        // component.InSlot = args.Slot;
        // if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, "HidesHair"))
        //     _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Hair, false);
    }

    private void OnGotUnequipped(EntityUid uid, ClothingFactionExplosionComponent component, GotUnequippedEvent args)
    {
        SoundSystem.Play("/Audio/Effects/ame_overloading_admin_alert.ogg", Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), AudioParams.Default.WithVolume(-4f));
        // component.InSlot = null;
        // if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, "HidesHair"))
        //     _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Hair, true);
    }

    private void DoExplode(EntityUid coords)
    {
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        sysMan.GetEntitySystem<ExplosionSystem>().QueueExplosion(coords, "Default", 5, 5, 5);
        QueueDel(coords);
    }
}
