using System;
using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Interfaces.GameObjects;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Prototypes;
using Content.Server.GameObjects.Components.Mobs.Body.Organs;
using Robust.Shared.IoC;
using System.Linq;
using Robust.Shared.Utility;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Reflection;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Limb is not just like <see cref="Organ"/>, it has BONES, and holds organs (and child limbs), 
    ///     it receive damage first, then through resistances and such it transfers the damage to organs,
    ///     also the limb is visible, and it can be targeted
    /// </summary>
    [Prototype("limb")]
    public class Limb : IPrototype, IIndexedPrototype
    {
#pragma warning disable CS0649
        [Dependency]
        protected IPrototypeManager PrototypeManager;
        protected IReflectionManager reflectionManager;
#pragma warning restore

        public string Name;
        public string Id;
        public string TexturePath = "";
        public List<Organ> Organs = new List<Organ>();

        public string Parent;
        public List<Limb> Children = new List<Limb>();

        Random _seed;
        string TargetKey;
        AttackTargetDef AttackTarget;
        LimbState State;
        int MaxHealth;
        int CurrentHealth;
        IEntity Owner;
        BodyTemplate BodyOwner;
        float BloodChange = 0f;
        string PrototypeEntity = "";
        bool childOrganDamage; //limbs that are used for deattaching, they'll get deleted if parent is dropped. (arms, legs)
        bool directOrganDamage;
        List<string> _organProt = new List<string>();
        /// <summary>
        /// Additional targets like Mouth and Eyes.
        /// </summary>
        Dictionary<string, string> _addTargetKeys = new Dictionary<string, string>();
        List<AdditionalTarget> _addTargets = new List<AdditionalTarget>();
        List<LimbStatus> Statuses = new List<LimbStatus>();


        string IIndexedPrototype.ID => Id;

        void IPrototype.LoadFrom(YamlMappingNode mapping)
        {
            var obj = YamlObjectSerializer.NewReader(mapping);
            obj.DataField(ref Id, "id", "");
            obj.DataField(ref Name, "name", "");
            obj.DataField(ref MaxHealth, "health", 0);
            obj.DataField(ref TargetKey, "target", "");
            obj.DataField(ref TexturePath, "dollIcon", "");
            obj.DataField(ref PrototypeEntity, "prototype", "");
            obj.DataField(ref Parent, "parent", "");
            obj.DataField(ref childOrganDamage, "childOrganDamage", false);
            obj.DataField(ref directOrganDamage, "directOrganDamage", false);

            if (mapping.TryGetNode<YamlSequenceNode>("additionalTargets", out var targetNodes))
            {
                foreach (var targetMap in targetNodes.Cast<YamlMappingNode>())
                {
                    ReadAddTargetPrototype(targetMap).ToList().ForEach(x => _addTargetKeys.Add(x.Key, x.Value));
                }
            }
            if (mapping.TryGetNode<YamlSequenceNode>("organs", out var organNodes))
            {
                _organProt = new List<string>();
                foreach (var prot in organNodes.Cast<YamlMappingNode>())
                {
                    _organProt.Add(prot.GetNode("map").AsString());
                }
            }
        }

        public void Initialize(IEntity owner, BodyTemplate body)
        {
            PrototypeManager = IoCManager.Resolve<IPrototypeManager>();
            reflectionManager = IoCManager.Resolve<IReflectionManager>();
            CurrentHealth = MaxHealth;
            Statuses = new List<LimbStatus>();
            Owner = owner;
            BodyOwner = body;
            _seed = new Random(DateTime.Now.GetHashCode());
            Organs = new List<Organ>();
            foreach (var key in _organProt)
            {
                var prot = PrototypeManager.Index<OrganPrototype>(key);
                var organ = prot.Create();
                organ.Initialize(owner, body);
                Organs.Add(organ);
            }
            if (reflectionManager.TryParseEnumReference(TargetKey, out var @enum))
            {
                AttackTarget = (AttackTargetDef)@enum;
            }
            foreach(var _addTargetKey in _addTargetKeys)
            {
                if (reflectionManager.TryParseEnumReference(_addTargetKey.Key, out var @anotherEnum))
                {
                    _addTargets.Add(new AdditionalTarget((AttackTargetDef)@anotherEnum, _addTargetKey.Value));
                }
            }
        }

        public LimbRender Render() //TODO
        {
            var color = new Color(128, 128, 128);
            switch (State)
                {
                case LimbState.Healthy:
                    color = new Color(0, 255, 0);
                    break;
                case LimbState.InjuredLightly:
                    color = new Color(255, 255, 0);
                    break;
                case LimbState.Injured:
                    color = new Color(255, 165, 0);
                    break;
                case LimbState.InjuredSeverely:
                    color = new Color(255, 0, 0);
                    break;
                case LimbState.Missing:
                    color = new Color(128, 128, 128);
                    break;
                }
            return new LimbRender(TexturePath, color);

        }

        public void HandleGib()
        {
            if(directOrganDamage) //don't spawn both arm and hand, leg and foot
            {
                return;
            }
            if (_seed.Prob(0.7f))
            {
                foreach (var organ in Organs)
                {
                    organ.HandleGib();
                }
            } 
            else 
            {
                SpawnPrototypeEntity();
            }
        }

        public void HandleDecapitation(bool spawn)
        {
            State = LimbState.Missing;
            foreach (var organ in Organs)
            {
                organ.State = OrganState.Dead;
            }
            foreach (var child in Children)
            {
                child.HandleDecapitation(childOrganDamage);
            }
            if (spawn)
            {
                SpawnPrototypeEntity();
            }
        }

        private void Dispose()
        {
            Children = null;
            Organs = null;
        }

        public void SpawnPrototypeEntity()
        {
            //TODO
            if (!string.IsNullOrWhiteSpace(PrototypeEntity))
            {
                Owner.EntityManager.TrySpawnEntityAt(PrototypeEntity, Owner.Transform.GridPosition, out var entity);
            }
        }

        public void HandleDamage(int damage) //TODO: test prob numbers, and add targetting!
        {
            if (State == LimbState.Missing)
            {
                return; //It doesn't exist, so it's unaffected by damage/heal
            }
            //if (_addTargets.Contains(target)) //handle the damage to additional snowflake targets (mouth, eyes, and whatever else you add)
            var state = ChangeHealthValue(damage);
            if(Organs.Count == 0)
            {
                return;
            }
            switch (state)
            {
                case LimbState.InjuredLightly:
                    if(_seed.Prob(0.1f))
                    {
                        _seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case LimbState.Injured:
                    //Organs[0].HandleDamage(damage); //testing brain damage
                    if (_seed.Prob(0.4f))
                    {
                        _seed.Pick(Organs).HandleDamage(damage);
                    }
                    break;
                case LimbState.InjuredSeverely:
                    _seed.Pick(Organs).HandleDamage(damage);
                    break;
                case LimbState.Missing:
                    if (State != state)
                    {
                        HandleDecapitation(true);
                    }
                    break;
            }
            State = state;
            Logger.DebugS("Limb", "Limb {0} received {1} damage!", Name, damage);

        }

        public Blood CirculateBlood(Blood blood)
        {
            blood.ChangeCurrentVolume(BloodChange);
            return blood;
        }

        private LimbState ChangeHealthValue(int value)
        {
            CurrentHealth -= value;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }

            switch ((float)CurrentHealth)
            {
                case float n when (n > MaxHealth / 0.75f):
                    return LimbState.Healthy;
                case float n when (n <= MaxHealth / 0.75f && n > MaxHealth / 3f):
                    return LimbState.InjuredLightly;
                case float n when (n <= MaxHealth / 2f && n > MaxHealth / 4f):
                    return LimbState.Injured;
                case float n when (n <= MaxHealth / 4f && Math.Abs(n) > float.Epsilon):
                    return LimbState.InjuredSeverely;
                case float n when (Math.Abs(n) < float.Epsilon):
                    return LimbState.Missing;
            }
            return State;
        }

        private Dictionary<string, string> ReadAddTargetPrototype(YamlMappingNode map)
        {

            if (map.TryGetNode("map", out var target))
            {
                var addTargetKey = target.AsString();
                if (map.TryGetNode("targetOrgan", out var targetOrgan))
                {
                    var addTargetOrgan = targetOrgan.AsString();
                    var dict = new Dictionary<string, string>();
                    dict.Add(addTargetKey, addTargetOrgan);
                    return dict;
                }

            }
            throw new InvalidOperationException("Not enough data specified to determine additional target.");
        }

    }

    public class AdditionalTarget
    {
        AttackTargetDef Target;
        string OrganTag;

        public AdditionalTarget(AttackTargetDef target, string tag)
        {
            Target = target;
            OrganTag = tag;
        }
    }

    public enum LimbState
    {
        Healthy,
        InjuredLightly,
        Injured,
        InjuredSeverely,
        Missing
    }

    public enum LimbStatus
    { 
        Bleeding,
        Broken
    }
}
