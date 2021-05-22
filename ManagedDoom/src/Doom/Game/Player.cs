//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom
{
    public sealed class Player
    {
        public static readonly int MaxPlayerCount = 4;

        public static readonly Fixed NormalViewHeight = Fixed.FromInt(41);

        private static readonly string[] defaultPlayerNames = new string[]
        {
            "Green",
            "Indigo",
            "Brown",
            "Red"
        };

        private int number;
        private string name;
        private bool inGame;

        private Mobj mobj;
        private PlayerState playerState;
        private TicCmd cmd;

        // Determine POV, including viewpoint bobbing during movement.
        // Focal origin above mobj.Z.
        private Fixed viewZ;

        // Base height above floor for viewz.
        private Fixed viewHeight;

        // Bob / squat speed.
        private Fixed deltaViewHeight;

        // Bounded / scaled total momentum.
        private Fixed bob;

        // This is only used between levels,
        // mobj.Health is used during levels.
        private int health;
        private int armorPoints;

        // Armor type is 0-2.
        private int armorType;

        // Power ups. invinc and invis are tic counters.
        private int[] powers;
        private bool[] cards;
        private bool backpack;

        // Frags, kills of other players.
        private int[] frags;

        private WeaponType readyWeapon;

        // Is WeanponType.NoChange if not changing.
        private WeaponType pendingWeapon;

        private bool[] weaponOwned;
        private int[] ammo;
        private int[] maxAmmo;

        // True if button down last tic.
        private bool attackDown;
        private bool useDown;

        // Bit flags, for cheats and debug.
        private CheatFlags cheats;

        // Refired shots are less accurate.
        private int refire;

        // For intermission stats.
        private int killCount;
        private int itemCount;
        private int secretCount;

        // Hint messages.
        private string message;
        private int messageTime;

        // For screen flashing (red or bright).
        private int damageCount;
        private int bonusCount;

        // Who did damage (null for floors / ceilings).
        private Mobj attacker;

        // So gun flashes light up areas.
        private int extraLight;

        // Current PLAYPAL, ???
        // can be set to REDCOLORMAP for pain, etc.
        private int fixedColorMap;

        // Player skin colorshift,
        // 0-3 for which color to draw player.
        private int colorMap;

        // Overlay view sprites (gun, etc).
        private PlayerSpriteDef[] playerSprites;

        // True if secret level has been done.
        private bool didSecret;

        public Player(int number)
        {
            this.number = number;

            name = defaultPlayerNames[number];

            cmd = new TicCmd();

            powers = new int[(int)PowerType.Count];
            cards = new bool[(int)CardType.Count];

            frags = new int[MaxPlayerCount];

            weaponOwned = new bool[(int)WeaponType.Count];
            ammo = new int[(int)AmmoType.Count];
            maxAmmo = new int[(int)AmmoType.Count];

            playerSprites = new PlayerSpriteDef[(int)PlayerSprite.Count];
            for (var i = 0; i < playerSprites.Length; i++)
            {
                playerSprites[i] = new PlayerSpriteDef();
            }
        }

        public void Clear()
        {
            mobj = null;
            playerState = 0;
            cmd.Clear();

            viewZ = Fixed.Zero;
            viewHeight = Fixed.Zero;
            deltaViewHeight = Fixed.Zero;
            bob = Fixed.Zero;

            health = 0;
            armorPoints = 0;
            armorType = 0;

            Array.Clear(powers, 0, powers.Length);
            Array.Clear(cards, 0, cards.Length);
            backpack = false;

            Array.Clear(frags, 0, frags.Length);

            readyWeapon = 0;
            pendingWeapon = 0;

            Array.Clear(weaponOwned, 0, weaponOwned.Length);
            Array.Clear(ammo, 0, ammo.Length);
            Array.Clear(maxAmmo, 0, maxAmmo.Length);

            useDown = false;
            attackDown = false;

            cheats = 0;

            refire = 0;

            killCount = 0;
            itemCount = 0;
            secretCount = 0;

            message = null;
            messageTime = 0;

            damageCount = 0;
            bonusCount = 0;

            attacker = null;

            extraLight = 0;

            fixedColorMap = 0;

            colorMap = 0;

            foreach (var psp in playerSprites)
            {
                psp.Clear();
            }

            didSecret = false;
        }

        public void Reborn()
        {
            mobj = null;
            playerState = PlayerState.Live;
            cmd.Clear();

            viewZ = Fixed.Zero;
            viewHeight = Fixed.Zero;
            deltaViewHeight = Fixed.Zero;
            bob = Fixed.Zero;

            health = DoomInfo.DeHackEdConst.InitialHealth;
            armorPoints = 0;
            armorType = 0;

            Array.Clear(powers, 0, powers.Length);
            Array.Clear(cards, 0, cards.Length);
            backpack = false;

            readyWeapon = WeaponType.Pistol;
            pendingWeapon = WeaponType.Pistol;

            Array.Clear(weaponOwned, 0, weaponOwned.Length);
            Array.Clear(ammo, 0, ammo.Length);
            Array.Clear(maxAmmo, 0, maxAmmo.Length);

            weaponOwned[(int)WeaponType.Fist] = true;
            weaponOwned[(int)WeaponType.Pistol] = true;
            ammo[(int)AmmoType.Clip] = DoomInfo.DeHackEdConst.InitialBullets;
            for (var i = 0; i < (int)AmmoType.Count; i++)
            {
                maxAmmo[i] = DoomInfo.AmmoInfos.Max[i];
            }

            // Don't do anything immediately.
            useDown = true;
            attackDown = true;

            cheats = 0;

            refire = 0;

            message = null;
            messageTime = 0;

            damageCount = 0;
            bonusCount = 0;

            attacker = null;

            extraLight = 0;

            fixedColorMap = 0;

            colorMap = 0;

            foreach (var psp in playerSprites)
            {
                psp.Clear();
            }

            didSecret = false;
        }

        public void FinishLevel()
        {
            Array.Clear(powers, 0, powers.Length);
            Array.Clear(cards, 0, cards.Length);

            // Cancel invisibility.
            mobj.Flags &= ~MobjFlags.Shadow;

            // Cancel gun flashes.
            extraLight = 0;

            // Cancel ir gogles.
            fixedColorMap = 0;

            // No palette changes.
            damageCount = 0;
            bonusCount = 0;
        }

        public void SendMessage(string message)
        {
            if (ReferenceEquals(this.message, (string)DoomInfo.Strings.MSGOFF) &&
                !ReferenceEquals(message, (string)DoomInfo.Strings.MSGON))
            {
                return;
            }

            this.message = message;
            messageTime = 4 * GameConst.TicRate;
        }

        public int Number => number;

        public string Name => name;

        public bool InGame
        {
            get => inGame;
            set => inGame = value;
        }

        public Mobj Mobj
        {
            get => mobj;
            set => mobj = value;
        }

        public PlayerState PlayerState
        {
            get => playerState;
            set => playerState = value;
        }

        public TicCmd Cmd
        {
            get => cmd;
        }

        public Fixed ViewZ
        {
            get => viewZ;
            set => viewZ = value;
        }

        public Fixed ViewHeight
        {
            get => viewHeight;
            set => viewHeight = value;
        }

        public Fixed DeltaViewHeight
        {
            get => deltaViewHeight;
            set => deltaViewHeight = value;
        }

        public Fixed Bob
        {
            get => bob;
            set => bob = value;
        }

        public int Health
        {
            get => health;
            set => health = value;
        }

        public int ArmorPoints
        {
            get => armorPoints;
            set => armorPoints = value;
        }

        public int ArmorType
        {
            get => armorType;
            set => armorType = value;
        }

        public int[] Powers
        {
            get => powers;
        }

        public bool[] Cards
        {
            get => cards;
        }

        public bool Backpack
        {
            get => backpack;
            set => backpack = value;
        }

        public int[] Frags
        {
            get => frags;
        }

        public WeaponType ReadyWeapon
        {
            get => readyWeapon;
            set => readyWeapon = value;
        }

        public WeaponType PendingWeapon
        {
            get => pendingWeapon;
            set => pendingWeapon = value;
        }

        public bool[] WeaponOwned
        {
            get => weaponOwned;
        }

        public int[] Ammo
        {
            get => ammo;
        }

        public int[] MaxAmmo
        {
            get => maxAmmo;
        }

        public bool AttackDown
        {
            get => attackDown;
            set => attackDown = value;
        }

        public bool UseDown
        {
            get => useDown;
            set => useDown = value;
        }

        public CheatFlags Cheats
        {
            get => cheats;
            set => cheats = value;
        }

        public int Refire
        {
            get => refire;
            set => refire = value;
        }

        public int KillCount
        {
            get => killCount;
            set => killCount = value;
        }

        public int ItemCount
        {
            get => itemCount;
            set => itemCount = value;
        }

        public int SecretCount
        {
            get => secretCount;
            set => secretCount = value;
        }

        public string Message
        {
            get => message;
            set => message = value;
        }

        public int MessageTime
        {
            get => messageTime;
            set => messageTime = value;
        }

        public int DamageCount
        {
            get => damageCount;
            set => damageCount = value;
        }

        public int BonusCount
        {
            get => bonusCount;
            set => bonusCount = value;
        }

        public Mobj Attacker
        {
            get => attacker;
            set => attacker = value;
        }

        public int ExtraLight
        {
            get => extraLight;
            set => extraLight = value;
        }

        public int FixedColorMap
        {
            get => fixedColorMap;
            set => fixedColorMap = value;
        }

        public int ColorMap
        {
            get => colorMap;
            set => colorMap = value;
        }

        public PlayerSpriteDef[] PlayerSprites
        {
            get => playerSprites;
        }

        public bool DidSecret
        {
            get => didSecret;
            set => didSecret = value;
        }
    }
}
