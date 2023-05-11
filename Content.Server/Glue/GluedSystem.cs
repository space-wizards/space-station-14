using Content.Server.Ghost.Roles.Components;
using Content.Server.Instruments;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Dummy;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Content.Shared.Hands.Components;
using Content.Server.Speech.Muting;
using Content.Shared.Item;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Glue;
using Content.Server.Abilities.Mime;
using Robust.Shared.Audio;
using Content.Server.Magic.Events;
using Content.Server.Chat.Systems;
using Content.Shared.Stealth.Components;
using Content.Shared.Ghost.Roles;
using Content.Server.Nutrition.Components;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition;
using Content.Shared.Interaction;
using Content.Shared.Humanoid;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Tag;
using Robust.Shared.Network;
using Content.Shared.Glue;
using Robust.Shared.Timing;
using System.Runtime.InteropServices;
using Content.Shared.Charges.Systems;
using Content.Shared.Charges.Components;
using System.Xml.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Server.Nutrition;

namespace Content.Server.Glue;

public sealed class GluedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GluedComponent, ComponentStartup>(OnGlued);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GluedComponent>();
        while (query.MoveNext(out var uid, out var glued))

            foreach (var glue in EntityQuery<GluedComponent>())
            {
                if (glue.Glued)
                    continue;

                if (_timing.CurTime < glue.GlueCooldown)
                    continue;

                glue.Glued = true;

                RemoveGlue(uid, glued);
            }
    }

    private void OnGlued(EntityUid uid, GluedComponent component, ComponentStartup args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;
        component.BeforeGluedEntityName = meta.EntityName;
        _audio.PlayPvs(component.Squeeze, uid);
        meta.EntityName = Loc.GetString("glued-name-prefix", ("target", name));
        component.Glued = false;
    }

    private void RemoveGlue(EntityUid uid, GluedComponent component)
    {
        if (component.Glued == true)
        {
            MetaData(uid).EntityName = component.BeforeGluedEntityName;
            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
            component.GlueCooldown = TimeSpan.FromSeconds(30);
        }
    }
}
