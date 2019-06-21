using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Limb is not just like <see cref="Organ"/>, it has BONES, and holds organs (and child limbs), 
    ///     it receive damage first, then through resistances and such it transfers the damage to organs,
    ///     also the limb is visible, and it can be targeted
    /// </summary>
    public class Limb
    {
        public string Name;
        public List<Organ> Organs;
        System.Enum Id;
        public List<Limb> Children;
        int Health;
        public void ExposeData(ObjectSerializer obj)
        {
            obj.DataField(ref Name, "name", "");
            obj.DataField(ref Id, "limb", null);
            obj.DataField(ref Organs, "organs", null);
            obj.DataField(ref Health, "health", 0);
        }
        public Limb(string name, System.Enum id, List<Organ> organs, List<Limb> children, int health)
        {
            Name = name;
            Id = id;
            Organs = organs;
            Children = children;
            Health = health;
        }
    }
}
