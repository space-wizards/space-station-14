using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DragDrop;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Toggleable;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blocking;

public sealed class SharedBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBlockingComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<SharedBlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<SharedBlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);

        SubscribeLocalEvent<SharedBlockingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SharedBlockingComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnUserDamageModified(EntityUid uid, SharedBlockingUserComponent component, DamageModifyEvent args)
    {
        if (TryComp(component.BlockingItem, out SharedBlockingComponent? blockingComponent) && blockingComponent.IsBlocking)
        {
            if (_proto.TryIndex("Metallic", out DamageModifierSetPrototype? blockModifier))
            {
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, blockModifier);
            }
        }
    }

    private void OnPickupAttempt(EntityUid uid, SharedBlockingComponent component, GettingPickedUpAttemptEvent args)
    {
        component.User = args.User;
        var userComp = EnsureComp<SharedBlockingUserComponent>(args.User);
        userComp.BlockingItem = uid;

        //To make sure that this bodytype doesn't get set as anything but the original
        if (TryComp(args.User, out PhysicsComponent? physicsComponent) && physicsComponent.BodyType != BodyType.Static)
        {
            userComp.OriginalBodyType = physicsComponent.BodyType;
        }
    }

    private void OnDrop(EntityUid uid, SharedBlockingComponent component, DroppedEvent args)
    {
        RemComp<SharedBlockingComponent>(args.User);
        component.User = null;
        if (component.IsBlocking)
        {
            StopBlocking(uid, component, args.User);
        }
    }


    private void OnGetActions(EntityUid uid, SharedBlockingComponent component, GetItemActionsEvent args)
    {
        if (component.BlockingToggleAction == null
            && _proto.TryIndex(component.BlockingToggleActionId, out InstantActionPrototype? act))
        {
            component.BlockingToggleAction = new(act);
        }

        if (component.BlockingToggleAction != null)
        {
            args.Actions.Add(component.BlockingToggleAction);
        }

    }

    private void OnToggleAction(EntityUid uid, SharedBlockingComponent component, ToggleActionEvent args)
    {
        if(args.Handled)
            return;

        if (component.IsBlocking)
            StopBlocking(uid, component, args.Performer);
        else
            StartBlocking(uid, component, args.Performer);

        args.Handled = true;
    }

    private bool StartBlocking(EntityUid uid, SharedBlockingComponent component, EntityUid user)
    {
        if (component.IsBlocking) return false;

        var xform = Transform(user);

        component.IsBlocking = true;

        var shape = new PhysShapeCircle();
        shape.Radius = 1f;

        if (TryComp(user, out PhysicsComponent? physicsComponent))
        {
            var fixture = new Fixture(physicsComponent, shape)
            {
                ID = "test",
                Hard = true,
                CollisionLayer = (int) CollisionGroup.WallLayer
            };

            _fixtureSystem.TryCreateFixture(physicsComponent, fixture);
        }

        if (component.BlockingToggleAction != null)
        {
            _actionsSystem.SetToggled(component.BlockingToggleAction, true);
            _transformSystem.AnchorEntity(xform);
        }

        return true;
    }

    private bool StopBlocking(EntityUid uid, SharedBlockingComponent component, EntityUid user)
    {
        if (!component.IsBlocking) return false;

        component.IsBlocking = false;

        var xform = Transform(user);

        //Storing component to be used to get the original bodytype
        //It won't let me just make a blank component here so I need to initialize
        //to use it outside of the if statement.
        var blockingUserComponent = new SharedBlockingUserComponent();

        if (TryComp(user, out SharedBlockingUserComponent? sharedBlockingUserComponent))
        {
            blockingUserComponent = sharedBlockingUserComponent;
        }

        //Used to store the physics component so the users bodytype can be set back to Kinematic Controller
        //It won't let me just make a blank component here so I need to initialize
        //to use it outside of the if statement.
        var bodyType = new PhysicsComponent();

        if (TryComp(user, out PhysicsComponent? physicsComponent))
        {
            _fixtureSystem.DestroyFixture(physicsComponent, "test");
            bodyType = physicsComponent;
        }

        if (component.BlockingToggleAction != null)
        {
            _actionsSystem.SetToggled(component.BlockingToggleAction, false);
            _transformSystem.Unanchor(xform);
            bodyType.BodyType = blockingUserComponent.OriginalBodyType;
        }

        return true;
    }

}
