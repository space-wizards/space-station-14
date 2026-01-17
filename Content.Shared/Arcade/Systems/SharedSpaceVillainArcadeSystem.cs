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
    public void SetInvinciblePlayer(Entity<SpaceVillainArcadeComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InvinciblePlayer == value)
            return;

        ent.Comp.InvinciblePlayer = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.InvinciblePlayer));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetInvincibleVillain(Entity<SpaceVillainArcadeComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InvincibleVillain == value)
            return;

        ent.Comp.InvincibleVillain = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.InvincibleVillain));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetOverflow(Entity<SpaceVillainArcadeComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Overflow == value)
            return;

        ent.Comp.Overflow = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.Overflow));
    }

    /// <summary>
    ///
    /// </summary>
    public void AddPlayerHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        SetPlayerHP(ent, (byte)Math.Clamp(ent.Comp.PlayerHP + value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.PlayerMaxHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void TakePlayerHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InvinciblePlayer)
            return;

        SetPlayerHP(ent, (byte)Math.Clamp(ent.Comp.PlayerHP - value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.PlayerMaxHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.PlayerHP == value)
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

        if (ent.Comp.PlayerMaxHP == value)
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

        SetPlayerMP(ent, (byte)Math.Clamp(ent.Comp.PlayerMP + value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.PlayerMaxMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void TakePlayerMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InvinciblePlayer)
            return;

        SetPlayerMP(ent, (byte)Math.Clamp(ent.Comp.PlayerMP - value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.PlayerMaxMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.PlayerMP == value)
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

        if (ent.Comp.PlayerMaxMP == value)
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

        SetVillainHP(ent, (byte)Math.Clamp(ent.Comp.VillainHP + value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.VillainMaxHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void TakeVillainHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InvincibleVillain)
            return;

        SetVillainHP(ent, (byte)Math.Clamp(ent.Comp.VillainHP - value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.VillainMaxHP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainHP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.VillainHP == value)
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

        if (ent.Comp.VillainMaxHP == value)
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

        SetVillainMP(ent, (byte)Math.Clamp(ent.Comp.VillainMP + value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.VillainMaxMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void TakeVillainMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InvincibleVillain)
            return;

        SetVillainMP(ent, (byte)Math.Clamp(ent.Comp.VillainMP - value, byte.MinValue, ent.Comp.Overflow ? byte.MaxValue : ent.Comp.VillainMaxMP));
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainMP(Entity<SpaceVillainArcadeComponent?> ent, byte value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.VillainMP == value)
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

        if (ent.Comp.VillainMaxMP == value)
            return;

        ent.Comp.VillainMaxMP = value;
        DirtyField(ent, nameof(SpaceVillainArcadeComponent.VillainMaxMP));
    }

    #endregion
}
