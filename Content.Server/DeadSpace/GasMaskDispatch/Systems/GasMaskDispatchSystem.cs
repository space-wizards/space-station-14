// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.DeadSpace.GasMaskDispatch.Components;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.GasMaskDispatch.Systems;

public sealed class GasMaskDispatchSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly Dictionary<GasMaskDispatchCode, string> CodeLocKeys = new()
    {
        [GasMaskDispatchCode.Code0] = "gas-mask-dispatch-message-code-0",
        [GasMaskDispatchCode.Code1] = "gas-mask-dispatch-message-code-1",
        [GasMaskDispatchCode.Code2] = "gas-mask-dispatch-message-code-2",
        [GasMaskDispatchCode.Code3] = "gas-mask-dispatch-message-code-3",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMaskDispatchComponent, OpenGasMaskDispatchMenuEvent>(OnOpenMenu);
        SubscribeNetworkEvent<GasMaskDispatchSelectMessage>(OnSelect);
    }

    private void OnOpenMenu(Entity<GasMaskDispatchComponent> ent, ref OpenGasMaskDispatchMenuEvent args)
    {
        // Открытие меню целиком обрабатывается на клиенте, здесь достаточно подтвердить действие,
        // чтобы у него корректно сработала перезарядка (useDelay) и звук нажатия.
        args.Handled = true;
    }

    private void OnSelect(GasMaskDispatchSelectMessage msg, EntitySessionEventArgs args)
    {
        var wearer = args.SenderSession.AttachedEntity;
        if (wearer == null)
            return;

        var mask = GetEntity(msg.Mask);
        if (!TryComp<GasMaskDispatchComponent>(mask, out var comp))
            return;

        // Убеждаемся, что противогаз действительно надет отправителем запроса.
        if (!_inventory.TryGetSlotEntity(wearer.Value, "mask", out var equippedMask) || equippedMask != mask)
            return;

        if (!_proto.TryIndex(comp.Channel, out var channel))
            return;

        if (!CodeLocKeys.TryGetValue(msg.Code, out var locKey))
            return;

        var location = _navMap.GetNearestBeaconString(wearer.Value, onlyName: true);
        var message = Loc.GetString(locKey, ("location", location));

        _radio.SendRadioMessage(wearer.Value, message, channel, wearer.Value);
        PlayDispatchAlert(wearer.Value, channel, comp.Sound);
    }

    /// <summary>
    /// Проигрывает звук оповещения всем, кто слушает указанный радиоканал, а также самому отправителю.
    /// </summary>
    private void PlayDispatchAlert(EntityUid sender, RadioChannelPrototype channel, SoundSpecifier sound)
    {
        var senderMap = Transform(sender).MapID;
        var notified = new HashSet<EntityUid>();

        var audioParams = sound.Params.WithVolume(-8f);

        var query = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (query.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels && !radio.Channels.Contains(channel.ID))
                continue;

            if (!channel.LongRange && transform.MapID != senderMap)
                continue;

            var player = receiver;
            while (player.IsValid() && !HasComp<ActorComponent>(player))
            {
                var parent = Transform(player).ParentUid;
                if (!parent.IsValid() || parent == player)
                    break;
                player = parent;
            }

            if (notified.Add(player))
                _audio.PlayGlobal(sound, player, audioParams);
        }

        if (notified.Add(sender))
            _audio.PlayGlobal(sound, sender, audioParams);
    }
}
