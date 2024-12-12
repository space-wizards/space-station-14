using Content.Shared._DeltaV.CartridgeLoader.Cartridges;
using Content.Shared._DeltaV.NanoChat;
using Content.Shared.Audio;
using Content.Shared.CartridgeLoader;
using Content.Shared._DeltaV.CartridgeLoader.Cartridges;
using Content.Shared._DeltaV.NanoChat;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem
{
    private void InitializeNanoChat()
    {
        SubscribeLocalEvent<NanoChatRecipientUpdatedEvent>(OnRecipientUpdated);
        SubscribeLocalEvent<NanoChatMessageReceivedEvent>(OnMessageReceived);
    }

    private void OnRecipientUpdated(ref NanoChatRecipientUpdatedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                new Dictionary<uint, NanoChatRecipient>(card.Recipients),
                probe.ScannedNanoChatData.Value.Messages,
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    private void OnMessageReceived(ref NanoChatMessageReceivedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                probe.ScannedNanoChatData.Value.Recipients,
                new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    private void ScanNanoChatCard(Entity<LogProbeCartridgeComponent> ent,
        CartridgeAfterInteractEvent args,
        EntityUid target,
        NanoChatCardComponent card)
    {
        _audioSystem.PlayEntity(ent.Comp.SoundScan,
            args.InteractEvent.User,
            target,
            AudioHelpers.WithVariation(0.25f, _random));
        _popupSystem.PopupCursor(Loc.GetString("log-probe-scan-nanochat", ("card", target)), args.InteractEvent.User);

        ent.Comp.PulledAccessLogs.Clear();

        ent.Comp.ScannedNanoChatData = new NanoChatData(
            new Dictionary<uint, NanoChatRecipient>(card.Recipients),
            new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
            card.Number,
            GetNetEntity(target)
        );

        UpdateUiState(ent, args.Loader);
    }
}
