using Content.Shared.Actions;
using Content.Shared.Changeling.Devour;
using Content.Shared.DoAfter;
using Content.Shared.Speech.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Changeling.Transform;

public abstract partial class SharedChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingTransformComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformRadialMessage>(OnRadialmessage);
        SubscribeLocalEvent<ChangelingIdentityComponent, GetChangelingIdentitiesRadialEvent>(OnGetIdentities);
    }

    private void OnInit(EntityUid uid, ChangelingTransformComponent component, MapInitEvent init)
    {
        _actionsSystem.AddAction(uid, ref component.ChangelingTransformActionEntity, component.ChangelingTransformAction);


        component.ChangelingIdentities = EnsureComp<ChangelingIdentityComponent>(uid);
        if (component.ChangelingIdentities.OriginalIdentityComponent == null &&
            TryComp<AppearanceComponent>(uid, out var appearance) &&
            TryComp<VocalComponent>(uid, out var vocals))
        {
            StoredIdentityComponent ling = new()
            {
                IdentityAppearance = appearance,
                IdentityVocals = vocals
            };
            component.ChangelingIdentities.Identities?.Add(ling);
            component.ChangelingIdentities.OriginalIdentityComponent = ling;
        }
    }

    private void OnTransformAction(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformActionEvent args)
    {
        var user = args.Performer;
        if (TryComp<UserInterfaceComponent>(component.ChangelingTransformActionEntity, out var userInterface))
        {
            var isOpen = _uiSystem.IsUiOpen(userInterface.Owner, TransformUi.Key, user);
            if (isOpen)
            {
                _uiSystem.CloseUi(userInterface.Owner, TransformUi.Key, user);
            }
            else
            {
                _uiSystem.OpenUi(userInterface.Owner, TransformUi.Key, user);
            }
        }
    }

    private void OnRadialmessage(ChangelingTransformRadialMessage ev)
    {
        if (!TryGetEntity(ev.Entity, out var target))
            return;

        ev.Event.User = ev.Actor;
        RaiseLocalEvent(target.Value, (object) ev.Event);
    }

    private void OnGetIdentities(Entity<ChangelingTransformComponent> component, ref GetChangelingIdentitiesRadialEvent ev)
    {
        if (component.Comp.ChangelingIdentities?.Identities != null)
        {
            foreach (var identity in component.Comp.ChangelingIdentities.Identities)
            {
                ev.Actions.Add(new()
                    {
                        Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/changeling.rsi"), "transform"),
                        Tooltip = identity.IdentityDna?.DNA,
                    }
                );
            }
        }
    }
}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed class ChangelingTransformRadialMessage : BoundUserInterfaceMessage
{
    public ChangelingTransformTargetAction Event = default!;
}
public sealed class ChangelingTransformRadialOption : ChangelingTransformTargetAction
{
    public SpriteSpecifier? Sprite;

    public string? Tooltip;

    //public ChangelingTransformTargetAction Event = default!;
}

[Serializable, NetSerializable]
public abstract class ChangelingTransformTargetAction
{
    [field: NonSerialized]
    public EntityUid User { get; set; }
}

[ByRefEvent]
public record struct GetChangelingIdentitiesRadialEvent()
{
    public List<ChangelingTransformRadialOption> Actions = new();
}

[Serializable, NetSerializable]
public sealed partial class ChangelingTransformWindupDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public enum TransformUi : byte
{
    Key,
}
