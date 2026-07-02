namespace Content.Server.Arcade.SpaceVillain;

public sealed partial class SpaceVillainGame
{
    /// <summary>
    /// A state holder for the fighters in the SpaceVillain game.
    /// </summary>
    public sealed class Fighter
    {
        /// <summary>
        /// The current hit point total of the fighter.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Hp
        {
            get => _hp;
            set
            {
                _hp = value;
                if (!Uncapped)
                {
                    _hp = MathHelper.Clamp(_hp, 0, HpMax);
                }
            }
        }
        private int _hp;

        /// <summary>
        /// The maximum hit point total of the fighter.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int HpMax
        {
            get => _hpMax;
            set
            {
                _hpMax = Math.Max(value, 0);
                Hp = _hp;  // Re-clamp the HP value
            }
        }
        private int _hpMax;

        /// <summary>
        /// The current mana total of the fighter.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Mp
        {
            get => _mp;
            set
            {
                _mp = value;
                if (!Uncapped)
                {
                    _mp = MathHelper.Clamp(_mp, 0, MpMax);
                }
            }
        }
        private int _mp;

        /// <summary>
        /// The maximum mana total of the fighter.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MpMax
        {
            get => _mpMax;
            set
            {
                _mpMax = Math.Max(value, 0);
                Mp = _mp;  // Re-clamp the MP value
            }
        }
        private int _mpMax;

        /// <summary>
        /// Whether the given fighter can take damage/lose mana.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Invincible = false;

        /// <summary>
        /// Whether the given fighter's HP and MP values are capped between 0 and their respective Max values.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Uncapped = false;
    }
}
