// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Examine;

namespace Content.Server.SS220.AutoEngrave;

public sealed class AutoEngravingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AutoEngravingComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, AutoEngravingComponent component, ExaminedEvent args)
    {
        if (component.AutoEngraveLocKey is null)
            return;

        args.PushMarkup(Loc.GetString(component.AutoEngraveLocKey, ("engraved", component.EngravedText)));
    }
}
