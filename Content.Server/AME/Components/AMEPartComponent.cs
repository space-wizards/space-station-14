using System.Linq;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Tool;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.AME.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IInteractUsing))]
    public class AMEPartComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        public override string Name => "AMEPart";
        [DataField("unwrapSound")] private SoundSpecifier _unwrapSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (!args.User.TryGetComponent<IHandsComponent>(out var hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("ame-part-component-interact-using-no-hands"));
                return true;
            }

            if (!args.Using.TryGetComponent<ToolComponent>(out var multitool) || multitool.Qualities != ToolQuality.Multitool)
                return true;

            if (!_mapManager.TryGetGrid(args.ClickLocation.GetGridId(_serverEntityManager), out var mapGrid))
                return false; // No AME in space.

            var snapPos = mapGrid.TileIndicesFor(args.ClickLocation);
            if (mapGrid.GetAnchoredEntities(snapPos).Any(sc => _serverEntityManager.HasComponent<AMEShieldComponent>(sc)))
            {
                Owner.PopupMessage(args.User, Loc.GetString("ame-part-component-shielding-already-present"));
                return true;
            }

            var ent = _serverEntityManager.SpawnEntity("AMEShielding", mapGrid.GridTileToLocal(snapPos));
            ent.Transform.LocalRotation = Owner.Transform.LocalRotation;

            SoundSystem.Play(Filter.Pvs(Owner), _unwrapSound.GetSound(), Owner);

            Owner.Delete();

            return true;
        }
    }
}
