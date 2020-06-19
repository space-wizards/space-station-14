using System;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Utensil
{
    [Flags]
    public enum UtensilType : byte
    {
        None = 0,
        Fork = 1,
        Spoon = 1 << 1,
        Knife = 1 << 2
    }

    public class SharedUtensilComponent : Component
    {
        public override string Name => "Utensil";

        public virtual UtensilType Types { get; set; }
    }
}
