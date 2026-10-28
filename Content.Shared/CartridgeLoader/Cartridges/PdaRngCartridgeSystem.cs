using Content.Shared.Administration.Logs;
using Content.Shared.CartridgeLoader.Cartridges.PdaRng;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.CartridgeLoader.Cartridges;

public sealed partial class PdaRngCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PdaRngCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<PdaRngCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    private void OnUiReady(Entity<PdaRngCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void OnMessage(Entity<PdaRngCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not PdaRngPrintMessage)
            return;

        TryPrintTicket(ent, args.User);
        UpdateUiState(ent, GetEntity(args.LoaderUid));
    }

    private void TryPrintTicket(Entity<PdaRngCartridgeComponent> ent, EntityUid user)
    {
        if (ent.Comp.HasRolled)
        {
            _popup.PopupClient(Loc.GetString("pda-rng-already-rolled"), user, user);
            return;
        }

        // Consume the roll immediately so prediction cannot double-print.
        ent.Comp.HasRolled = true;
        Dirty(ent);

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner), GetNetEntity(user));
        var number = random.Next(PdaRngAnalyzer.MinNumber, PdaRngAnalyzer.MaxNumber + 1);
        var result = PdaRngAnalyzer.Analyze(number);

        var playerName = Identity.Name(user, EntityManager);
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = Loc.GetString("generic-unknown-title");

        var paper = PredictedSpawn(ent.Comp.PaperPrototype, _transform.GetMapCoordinates(user));
        _label.Label(paper, Loc.GetString("pda-rng-printout-label", ("number", result.NumberText)));
        _audio.PlayEntity(ent.Comp.PrintSound, user, paper);
        _hands.PickupOrDrop(user, paper, checkActionBlocker: false);

        var paperComp = Comp<PaperComponent>(paper);
        _paper.SetContent((paper, paperComp), PdaRngAnalyzer.FormatTicket(result, playerName));

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low,
            $"{ToPrettyString(user):user} printed PDA RNG ticket {result.NumberText} ({paper})");
    }

    private void UpdateUiState(Entity<PdaRngCartridgeComponent> ent, EntityUid loaderUid)
    {
        _cartridge.UpdateCartridgeUiState(loaderUid, new PdaRngUiState(ent.Comp.HasRolled));
    }
}

[Serializable, NetSerializable]
public sealed class PdaRngPrintMessage : CartridgeMessageEvent;

[Serializable, NetSerializable]
public sealed class PdaRngUiState : BoundUserInterfaceState
{
    public bool HasRolled { get; }

    public PdaRngUiState(bool hasRolled)
    {
        HasRolled = hasRolled;
    }
}
