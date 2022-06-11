namespace Content.Shared.Friction
{
    [RegisterComponent]

    public sealed class TileFrictionModifierComponent : Component
    {
        /// <summary>
        ///     Multiply the tilefriction cvar by this to get the body's actual tilefriction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("modifier")]
        public float Modifier;
    }
}
