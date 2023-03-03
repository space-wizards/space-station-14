
using Content.Server.MachineLinking.Events;
using Content.Shared.Doors;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Server.SCP
{
    public sealed class SCPGeneralSystem : EntitySystem
    {
        private Dictionary<string,List<EntityUid>> _containmentDoors = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ScpContainmentDoorComponent, ComponentInit>(OnContainmentComponentInit);
            SubscribeLocalEvent<ScpContainmentDoorComponent, ComponentRemove>(OnContainmentComponentRemove);
            SubscribeLocalEvent<ScpContainmentDoorComponent, BeforeDoorClosedEvent>(OnDoorCloseTryRemove);
        }

        private void OnContainmentComponentInit(EntityUid uid, ScpContainmentDoorComponent comp, ComponentInit args)
        {
            var group = comp.DoorGroup;
            List<EntityUid> groupList;
            if (_containmentDoors.ContainsKey(group))
                groupList = _containmentDoors[group];
            else
            {
                groupList = new();
                _containmentDoors[group] = groupList;
            }
            groupList.Add(uid);
        }
        private void OnContainmentComponentRemove(EntityUid uid, ScpContainmentDoorComponent comp, ComponentRemove args)
        {
            var group = comp.DoorGroup;
            if (!_containmentDoors.ContainsKey(group))
                return;
            var groupList = _containmentDoors[group];
            groupList.Remove(uid);
            if (groupList.Count == 0)
                _containmentDoors.Remove(group);
        }
        private void OnDoorCloseTryRemove(EntityUid uid, ScpContainmentDoorComponent comp, BeforeDoorClosedEvent args)
        {
            if (comp.DoorBlocked)
                args.Cancel();
        }

        public List<EntityUid> GetGroupedDoors(string group)
        {
            return _containmentDoors[group];
        }
        public List<EntityUid>? GetGroupedDoorsOrNull(string group)
        {
            if (_containmentDoors.TryGetValue(group, out var groupList))
                return groupList;
            return null;
        }
        public List<string> GetGroupedDoorsGroups()
        {
            var res = new List<string>();
            foreach (var pair in _containmentDoors)
                res.Add(pair.Key);
            return res;
        }
    }
}
