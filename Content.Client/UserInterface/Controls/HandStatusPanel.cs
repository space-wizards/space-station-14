using System.Globalization;
using Content.Client.Stylesheets;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public abstract class HandDataPanel : BoxContainer
{
    [Dependency] protected readonly IEntityManager EntityManager = default!;

    protected EntityUid? HeldEntity;
    public HandDataPanel()
    {
        IoCManager.InjectDependencies(this);
    }
    protected abstract void UpdateData();
    protected abstract void ClearData();
    public void UpdateData(HandControl? hand)
    {
        if (hand == null)
        {
            UpdateDataForEntity(null);
            return;
        }
        UpdateDataForEntity(hand.HeldItem);
    }
    public void UpdateDataForEntity(EntityUid? heldEntity)
    {
        if (!EntityManager.TryGetComponent<MetaDataComponent>(heldEntity, out var meta))
        {
            ClearData();
            return;
        }
        HeldEntity = heldEntity;
        UpdateData();
    }
}
