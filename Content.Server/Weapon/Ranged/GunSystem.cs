using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EffectSystem _effects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmmoComponent, ExaminedEvent>(OnAmmoExamine);

        SubscribeLocalEvent<AmmoBoxComponent, ComponentInit>(OnAmmoBoxInit);
        SubscribeLocalEvent<AmmoBoxComponent, MapInitEvent>(OnAmmoBoxMapInit);
        SubscribeLocalEvent<AmmoBoxComponent, ExaminedEvent>(OnAmmoBoxExamine);

        SubscribeLocalEvent<AmmoBoxComponent, InteractUsingEvent>(OnAmmoBoxInteractUsing);
        SubscribeLocalEvent<AmmoBoxComponent, UseInHandEvent>(OnAmmoBoxUse);
        SubscribeLocalEvent<AmmoBoxComponent, InteractHandEvent>(OnAmmoBoxInteractHand);
        SubscribeLocalEvent<AmmoBoxComponent, GetAlternativeVerbsEvent>(OnAmmoBoxAltVerbs);
    }
}
