using Content.Shared.Arcade;
using Content.Shared.Bed.Sleep;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.StatusEffect;
using Robust.Server.GameObjects;

namespace Content.Server.Arcade.ParadiseGame;

public sealed partial class ParadiseSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKey = "ForcedSleep";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadiseArcadeComponent, ParadiesMessages.ParadiseArcadeConnectButtonPressedEvent>(OnPress);
        SubscribeLocalEvent<InParadiseComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SleepingComponent, ComponentRemove>(OnRemove);

        // This whole thing is a mess, but good enough for april fools. I will never touch UI ever again.
    }

    private void OnRemove(EntityUid uid, SleepingComponent component, ComponentRemove args)
    {
        if (HasComp<InParadiseComponent>(uid))
        {
            RemComp<InParadiseComponent>(uid);
        }
    }

    private void OnExamine(EntityUid uid, InParadiseComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("paradise-arcade-on-examine", ("target", Identity.Entity(uid, EntityManager))));
    }

    private void OnPress(EntityUid uid, ParadiseArcadeComponent component, ParadiesMessages.ParadiseArcadeConnectButtonPressedEvent args)
    {
        if (!args.Session.AttachedEntity.HasValue)
        {
            Log.Warning("ParadiseArcadeConnectButtonPressedEvent sent without attached entity.");
            return;
        }

        _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(args.Session.AttachedEntity.Value, StatusEffectKey, TimeSpan.FromSeconds(60), true);
        AddComp<InParadiseComponent>(args.Session.AttachedEntity.Value);
        if (!_userInterface.TryGetUi(uid, args.UiKey, out var userInterface))
        {
            Log.Warning("UserInterface not found.");
            return;
        }

        var didSend = _userInterface.TrySendUiMessage(
            userInterface,
            new ParadiesMessages.ParadiseArcadeConnectEvent(component.Destination),
            args.Session);

        if (!didSend)
        {
            Log.Error("Failed to send message.");
        }
    }
}
