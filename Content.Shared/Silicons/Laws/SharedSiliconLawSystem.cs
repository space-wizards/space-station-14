using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract partial class SharedSiliconLawSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnLawBoundInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentShutdown>(OnLawBoundShutdown);

        SubscribeLocalEvent<BorgChassisComponent, GetSiliconLawsEvent>(OnChassisGetLaws);

        InitializeUpdater();
        InitializeProvider();
        InitializeEmag();
    }

    #region Events

    private void OnLawBoundShutdown(Entity<SiliconLawBoundComponent> ent, ref ComponentShutdown args)
    {
        UnlinkFromProvider(ent.AsNullable());
    }

    private void OnLawBoundInit(Entity<SiliconLawBoundComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.FetchOnInit)
            FetchLawset(ent.AsNullable());
    }

    private void OnToggleLawsScreen(Entity<SiliconLawBoundComponent> ent, ref ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;

        _userInterface.TryToggleUi(ent.Owner, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnChassisGetLaws(Entity<BorgChassisComponent> ent, ref GetSiliconLawsEvent args)
    {
        // Chassis specific laws take priority over brain laws.
        // We want this for things like Syndieborgs or Xenoborgs, who depend on chassis specific laws.
        if (TryComp<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            args.Laws = chassisProvider.Lawset.Clone();
            args.Handled = true;
        }
        else if (TryComp<SiliconLawProviderComponent>(ent.Comp.BrainEntity, out var brainProvider))
        {
            args.Laws = brainProvider.Lawset.Clone();
            args.LinkedEntity = ent.Comp.BrainEntity;
            args.Handled = true;
        }
    }
    #endregion Events
}

/// <summary>
/// An event used to get the laws of silicons roundstart, linking them to potential LawProviders.
/// </summary>
/// <param name="Entity">The entity we are gathering the laws for.</param>
/// <param name="LinkedEntity">The entity to link to, if null, uses the entity this event was raised on.</param>
[ByRefEvent]
public record struct GetSiliconLawsEvent(EntityUid Entity, EntityUid? LinkedEntity = null)
{
    public SiliconLawset Laws = new();

    public bool Handled = false;
}

/// <summary>
/// Raised on an entity with <see cref="SiliconLawBoundComponent"/> when their provider is changed.
/// </summary>
/// <param name="NewProvider">The new provider whose laws are being taken.</param>
/// <param name="OldProvider">The old provider, can be null if there wasn't one.</param>
[ByRefEvent]
public record struct SiliconLawProviderChanged(EntityUid NewProvider, EntityUid? OldProvider = null);

/// <summary>
/// Raised on an entity with <see cref="SiliconLawBoundComponent"/> when they are unlinked from a provider.
/// </summary>
/// <param name="Provider">The provider the entity has been unlinked from..</param>
[ByRefEvent]
public record struct SiliconLawProviderUnlinked(EntityUid Provider);

public sealed partial class ToggleLawsScreenEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SiliconLawsUiKey : byte
{
    Key
}
