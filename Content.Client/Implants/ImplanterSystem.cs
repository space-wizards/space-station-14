using Content.Client.Implants.UI;
using Content.Client.Items;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImplanterComponent, AfterAutoHandleStateEvent>(OnHandleImplanterState);
        SubscribeNetworkEvent<DrawImplantAttemptEvent>(ChangeSprite);
        Subs.ItemStatus<ImplanterComponent>(ent => new ImplanterStatusControl(ent));
    }

    private void ChangeSprite(DrawImplantAttemptEvent args)
    {
        var implant = GetEntity(args.Implant);
        var implanter = GetEntity(args.Implanter);

        if (!TryComp(implant, out SubdermalImplantComponent? implantComp))
            return;

        if (TryComp<SpriteComponent>(implanter, out var sprite))
        {
            sprite.LayerSetColor("implantFull", implantComp.Color);
        }
    }

    protected override void OnImplanterInit(EntityUid uid, ImplanterComponent component, ComponentInit args)
    {
        SubdermalImplantComponent? subdermal = null;
        base.OnImplanterInit(uid, component, args);

        var implant = component.ImplanterSlot.ContainerSlot?.ContainedEntity;
        Log.Info($"Implanter {ToPrettyString(uid)} has implant {implant}");
        if (TryComp(implant, out SubdermalImplantComponent? implantComp))
        {
            subdermal = implantComp;
        }
        else if (component.Implant != null)
        {
            if (_proto.TryIndex<EntityPrototype>(component.Implant.Value.Id, out var proto) &&
                proto.TryGetComponent<SubdermalImplantComponent>(out var comp))
            {
                subdermal = comp;
            }
        }

        if (subdermal != null)
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                sprite.LayerSetColor("implantFull", subdermal.Color);
            }
        }
    }

    private void OnHandleImplanterState(EntityUid uid, ImplanterComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_uiSystem.TryGetOpenUi<DeimplantBoundUserInterface>(uid, DeimplantUiKey.Key, out var bui))
        {
            Dictionary<string, string> implants = new();
            foreach (var implant in component.DeimplantWhitelist)
            {
                if (_proto.TryIndex(implant, out var proto))
                    implants.Add(proto.ID, proto.Name);
            }

            bui.UpdateState(implants, component.DeimplantChosen);
        }

        component.UiUpdateNeeded = true;
    }
}
