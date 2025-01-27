using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Changeling.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Devour;
using Content.Shared.DoAfter;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Materials;
using Content.Server.Stack;
using Content.Shared.Damage;
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Materials;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Devour;

public sealed class ChangelingDevourSystem : SharedChangelingDevourSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    protected override void StartSound(Entity<ChangelingDevourComponent> ent, SoundSpecifier? sound)
    {
        if(sound is not null)
            ent.Comp.CurrentDevourSound = _audioSystem.PlayPvs(sound, ent)!.Value.Entity;
    }

    protected override void StopSound(Entity<ChangelingDevourComponent> ent)
    {
        if (ent.Comp.CurrentDevourSound is not null)
            _audioSystem.Stop(ent.Comp.CurrentDevourSound);
        ent.Comp.CurrentDevourSound = null;
    }

    protected override void RipClothing(EntityUid uid, EntityUid item,  ButcherableComponent butcher)
    {
        var spawnEntities = EntitySpawnCollection.GetSpawns(butcher.SpawnedEntities, _robustRandom);
        var coords = _transform.GetMapCoordinates(uid);
        foreach (var proto in spawnEntities)
        {
            Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
        }
        QueueDel(item);
    }
}
