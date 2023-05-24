namespace Content.Server.Arcade.SpaceVillain;

public sealed partial class SpaceVillainGame
{
    /// <summary>
    /// 
    /// </summary>
    private sealed class Fighter
    {
        /// <summary>
        /// 
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Hp
        {
            get => _hp;
            set => _hp = MathHelper.Clamp(value, 0, HpMax);
        }
        private int _hp;

        /// <summary>
        /// 
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int HpMax
        {
            get => _hpMax;
            set
            {
                _hpMax = Math.Max(value, 0);
                Hp = MathHelper.Clamp(Hp, 0, HpMax);
            }
        }
        private int _hpMax;

        /// <summary>
        /// 
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Mp
        {
            get => _mp;
            set => _mp = MathHelper.Clamp(value, 0, MpMax);
        }
        private int _mp;

        /// <summary>
        /// 
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MpMax
        {
            get => _mpMax;
            set
            {
                _mpMax = Math.Max(value, 0);
                Mp = MathHelper.Clamp(Mp, 0, MpMax);
            }
        }
        private int _mpMax;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Invincible = false;
    }
}
