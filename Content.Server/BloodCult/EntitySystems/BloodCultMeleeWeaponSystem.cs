using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Server.Hands.Systems;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;

namespace Content.Server.BloodCult.EntitySystems;

public sealed class BloodCultMeleeWeaponSystem : EntitySystem
{
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly PopupSystem _popupSystem = default!;
	[Dependency] private readonly HandsSystem _hands = default!;
	[Dependency] private readonly SharedAudioSystem _audioSystem = default!;

	public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<BloodCultMeleeWeaponComponent, BloodCultMeleeAllyBlockedAttemptEvent>(OnAllyBlocked);
		SubscribeLocalEvent<BloodCultMeleeWeaponComponent, BloodCultMeleeChaplainBlockedAttemptEvent>(OnChaplainBlocked);
	}

	private void OnAllyBlocked(Entity<BloodCultMeleeWeaponComponent> ent, ref BloodCultMeleeAllyBlockedAttemptEvent args)
	{
		if (args.Cancelled)
			return;

		_popupSystem.PopupEntity(
			Loc.GetString("cult-attack-teamhit"),
			args.User, args.User, PopupType.MediumCaution);
	}

	private void OnChaplainBlocked(Entity<BloodCultMeleeWeaponComponent> ent, ref BloodCultMeleeChaplainBlockedAttemptEvent args)
	{
		if (args.Cancelled)
			return;

		_popupSystem.PopupEntity(
			Loc.GetString("cult-attack-repelled"),
			args.User, args.User, PopupType.MediumCaution);
		var coordinates = Transform(args.User).Coordinates;
		_audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Effects/holy.ogg"), coordinates);
		var offsetRandomCoordinates = coordinates.Offset(_random.NextVector2(1f, 1.5f));
		_hands.ThrowHeldItem(args.User, offsetRandomCoordinates);
	}
}
