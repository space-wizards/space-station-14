using Content.Shared.Arcade.Components;

namespace Content.Shared.Arcade.Systems;

/// <summary>
///
/// </summary>
public sealed partial class SharedSpaceVillainArcadeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    #region Events

    #endregion

    #region BUI

    #endregion

    #region API

    /// <summary>
    ///
    /// </summary>
    public void ToggleOverflow(Entity<SpaceVillainArcadeComponent?> ent)
    {
        if (!RemComp<SpaceVillainArcadeOverflowComponent>(ent))
            EnsureComp<SpaceVillainArcadeOverflowComponent>(ent);
    }

    /// <summary>
    ///
    /// </summary>
    public bool IsOverflowAllowed(Entity<SpaceVillainArcadeComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp) && HasComp<SpaceVillainArcadeOverflowComponent>(ent);
    }

    /// <summary>
    ///
    /// </summary>
    public void AddPlayerHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PlayerHP = (byte)Math.Clamp(ent.Comp.PlayerHP + value, byte.MinValue, IsOverflowAllowed(ent) ? byte.MaxValue : ent.Comp.PlayerMaxHP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.PlayerHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PlayerHP = byte.Min(value, ent.Comp.PlayerMaxHP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.PlayerHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerMaxHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PlayerMaxHP = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.PlayerMaxHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void AddPlayerMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PlayerMP = (byte)Math.Clamp(ent.Comp.PlayerMP + value, byte.MinValue, IsOverflowAllowed(ent) ? byte.MaxValue : ent.Comp.PlayerMaxMP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.PlayerMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PlayerMP = byte.Min(value, ent.Comp.PlayerMaxMP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.PlayerMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerMaxMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PlayerMaxMP = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.PlayerMaxMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void AddVillainHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.VillainHP = (byte)Math.Clamp(ent.Comp.VillainHP + value, byte.MinValue, IsOverflowAllowed(ent) ? byte.MaxValue : ent.Comp.VillainMaxHP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.VillainHP = byte.Min(value, ent.Comp.VillainMaxHP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainMaxHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.VillainMaxHP = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainMaxHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void AddVillainMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.VillainMP = (byte)Math.Clamp(ent.Comp.VillainMP + value, byte.MinValue, IsOverflowAllowed(ent) ? byte.MaxValue : ent.Comp.VillainMaxMP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.VillainMP = byte.Min(value, ent.Comp.VillainMaxMP);
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainMaxMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.VillainMaxMP = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainMaxMP));
    }

    #endregion
}
