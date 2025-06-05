using Content.Server.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NotekeeperCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not NotekeeperUiMessageEvent message)
            return;

        if (message.Action == NotekeeperUiAction.Add)
        {
            component.Notes.Add(message.Note);
            _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                $"{ToPrettyString(args.Actor)} added a note to PDA: '{message.Note}' contained on: {ToPrettyString(uid)}");
        }
        else
        {
            component.Notes.Remove(message.Note);
            _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                $"{ToPrettyString(args.Actor)} removed a note from PDA: '{message.Note}' was contained on: {ToPrettyString(uid)}");
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, NotekeeperCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new NotekeeperUiState(component.Notes);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
