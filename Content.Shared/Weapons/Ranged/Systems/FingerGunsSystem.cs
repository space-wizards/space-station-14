using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class FingerGunsSystem : EntitySystem
{
    [Dependency] private TransformableItemSystem _transformable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FingerGunsComponent, UseInHandEvent>(OnActivate, before: new[] { typeof(ClothingSystem) });
        SubscribeLocalEvent<FingerGunsGunComponent, GetVerbsEvent<AlternativeVerb>>(OnGunGetVerbs);
    }

    private void OnActivate(Entity<FingerGunsComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true; // prevents using in hand from trying to equip it to hands slot by default

        if (!_transformable.TryGetHiddenItem(ent.Owner, out var gun))
            return;

        _transformable.Swap(ent, gun);
    }

    private void OnGunGetVerbs(Entity<FingerGunsGunComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_transformable.TryGetHiddenItem(ent.Owner, out var glove))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("finger-guns-revert"),
            Act = () => _transformable.Swap(ent, glove),
        });
    }
}
