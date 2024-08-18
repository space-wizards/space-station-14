using Content.Shared.Eye;
using Content.Shared.Movement.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public sealed class IncorporealSystem : EntitySystem
{
    // Somewhat placeholder for holopads

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedVisibilitySystem _vis = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IncorporealComponent, MapInitEvent>(OnIncorporealMapInit);
        SubscribeLocalEvent<IncorporealComponent, RefreshMovementSpeedModifiersEvent>(OnIncorporealSpeed);
    }

    private void OnIncorporealSpeed(Entity<IncorporealComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Visible)
            args.ModifySpeed(ent.Comp.VisibleSpeedModifier);
    }

    private void OnIncorporealMapInit(Entity<IncorporealComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<IncorporealComponent> ent)
    {
        _appearance.SetData(ent.Owner, IncorporealState.Base, ent.Comp.Visible);
    }

    public bool SetVisible(Entity<IncorporealComponent> entity, bool value)
    {
        if (entity.Comp.Visible == value)
            return false;

        entity.Comp.Visible = value;
        Dirty(entity);

        if (value)
        {
            _vis.AddLayer(entity.Owner, (ushort) VisibilityFlags.Normal);
        }
        else
        {
            _vis.RemoveLayer(entity.Owner, (ushort) VisibilityFlags.Normal);
        }

        UpdateAppearance(entity);
        _speed. RefreshMovementSpeedModifiers(entity);
        return true;
    }
}

[Serializable, NetSerializable]
public enum IncorporealState : byte
{
    Base,
}
