// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.GameObjects;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;

namespace Content.Server.BloodCult.EntitySystems;

public sealed partial class CultAnchorableSystem : EntitySystem
{
	[Dependency] private readonly SharedAppearanceSystem _appearance = default!;

	public override void Initialize()
	{
		base.Initialize();

		SubscribeLocalEvent<CultAnchorableComponent, AnchorStateChangedEvent>(OnAnchorChanged);
	}

	private void OnAnchorChanged(EntityUid uid, CultAnchorableComponent component, AnchorStateChangedEvent args)
	{
		if (args.Anchored)
			_appearance.SetData(uid, CultStructureVisuals.Anchored, true);
		else
			_appearance.SetData(uid, CultStructureVisuals.Anchored, false);
	}
}
