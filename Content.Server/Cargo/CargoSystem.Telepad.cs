using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Shared.Cargo;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Cargo;

public sealed partial class CargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        SubscribeLocalEvent<CargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
    }

    private void UpdateTelepad(float frameTime)
    {
        foreach (var comp in EntityManager.EntityQuery<CargoTelepadComponent>())
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(comp.Owner, out var appearance);

            if (comp.CurrentState == CargoTelepadState.Unpowered || comp.TeleportQueue.Count <= 0)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                appearance?.SetData(CargoTelepadVisuals.State, CargoTelepadState.Idle);
                comp.Accumulator = comp.Delay;
                continue;
            }

            comp.Accumulator -= frameTime;

            // Uhh listen teleporting takes time and I just want the 1 float.
            if (comp.Accumulator > 0f)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                appearance?.SetData(CargoTelepadVisuals.State, CargoTelepadState.Idle);
                continue;
            }

            var product = comp.TeleportQueue.Pop();

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.TeleportSound.GetSound(), comp.Owner, AudioParams.Default.WithVolume(-8f));
            SpawnProduct(comp, product);

            comp.CurrentState = CargoTelepadState.Teleporting;
            appearance?.SetData(CargoTelepadVisuals.State, CargoTelepadState.Teleporting);
            comp.Accumulator = comp.Delay;
        }
    }

    private void SetEnabled(CargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(component.Owner, ref receiver, ref xform, false)) return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Setting idle state should be handled by Update();
        if (disabled) return;

        TryComp<AppearanceComponent>(component.Owner, out var appearance);
        component.CurrentState = CargoTelepadState.Unpowered;
        appearance?.SetData(CargoTelepadVisuals.State, CargoTelepadState.Unpowered);
    }

    private void OnTelepadPowerChange(EntityUid uid, CargoTelepadComponent component, PowerChangedEvent args)
    {
        SetEnabled(component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, CargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(component);
    }

    public void QueueTeleport(CargoTelepadComponent component, CargoOrderData order)
    {
        for (var i = 0; i < order.Amount; i++)
        {
            component.TeleportQueue.Push(order);
        }
    }

    /// <summary>
    ///     Spawn the product and a piece of paper. Attempt to attach the paper to the product.
    /// </summary>
    private void SpawnProduct(CargoTelepadComponent component, CargoOrderData data)
    {
        // spawn the order
        if (!_protoMan.TryIndex(data.ProductId, out CargoProductPrototype? prototype))
            return;

        var xform = Transform(component.Owner);

        var product = EntityManager.SpawnEntity(prototype.Product, xform.Coordinates);

        Transform(product).Anchored = false;

        // spawn a piece of paper.
        var printed = EntityManager.SpawnEntity(component.PrinterOutput, xform.Coordinates);

        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        // fill in the order data
        var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", data.OrderNumber));

        MetaData(printed).EntityName = val;

        paper.SetContent(Loc.GetString(
            "cargo-console-paper-print-text",
            ("orderNumber", data.OrderNumber),
            ("requester", data.Requester),
            ("reason", data.Reason),
            ("approver", data.Approver)));

        // attempt to attach the label
        if (TryComp<PaperLabelComponent>(product, out var label))
        {
            _slots.TryInsert(component.Owner, label.LabelSlot, printed, null);
        }
    }
}
