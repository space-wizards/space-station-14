using Content.Server.Clothing.Systems;
using Content.Server.GhostKick;
using Content.Server.Mind;
using Content.Server.Movement.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Silicons.Laws;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tabletop;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Slippery;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Administration.Verbs.Operations;

/// <summary>
/// Executes data-defined admin operations by raising their local events on the target.
/// </summary>
public sealed partial class AdminOperationSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ContentEyeSystem _contentEye = default!;
    [Dependency] private SharedCreamPieSystem _creamPie = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private GhostKickManager _ghostKick = default!;
    [Dependency] private SharedGodmodeSystem _godmode = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private OutfitSystem _outfit = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private SiliconLawSystem _siliconLaws = default!;
    [Dependency] private SlipperySystem _slippery = default!;
    [Dependency] private TabletopSystem _tabletop = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    /// <summary>
    /// Raises the strongly typed local event used by an operation's handler.
    /// </summary>
    public void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>
    {
        var operationEvent = new AdminOperationEvent<T>(operation, user);
        RaiseLocalEvent(target, ref operationEvent);
    }

    /// <summary>
    /// Executes every operation synchronously in list order.
    /// </summary>
    public void Execute(EntityUid target, EntityUid user, IReadOnlyList<AdminOperation> operations)
    {
        foreach (var operation in operations)
        {
            operation.RaiseEvent(target, user, this);
        }
    }
}
