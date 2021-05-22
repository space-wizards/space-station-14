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
    public sealed class ItemPickup
    {
        private World world;

        public ItemPickup(World world)
        {
            this.world = world;
        }



        /// <summary>
        /// Give the player the ammo.
        /// </summary>
        /// <param name="amount">
        /// The number of clip loads, not the individual count (0 = 1/2 clip).
        /// </param>
        /// <returns>
        /// False if the ammo can't be picked up at all.
        /// </returns>
        public bool GiveAmmo(Player player, AmmoType ammo, int amount)
        {
            if (ammo == AmmoType.NoAmmo)
            {
                return false;
            }

            if (ammo < 0 || (int)ammo > (int)AmmoType.Count)
            {
                throw new Exception("Bad ammo type: " + ammo);
            }

            if (player.Ammo[(int)ammo] == player.MaxAmmo[(int)ammo])
            {
                return false;
            }

            if (amount != 0)
            {
                amount *= DoomInfo.AmmoInfos.Clip[(int)ammo];
            }
            else
            {
                amount = DoomInfo.AmmoInfos.Clip[(int)ammo] / 2;
            }

            if (world.Options.Skill == GameSkill.Baby ||
                world.Options.Skill == GameSkill.Nightmare)
            {
                // Give double ammo in trainer mode, you'll need in nightmare.
                amount <<= 1;
            }

            var oldammo = player.Ammo[(int)ammo];
            player.Ammo[(int)ammo] += amount;

            if (player.Ammo[(int)ammo] > player.MaxAmmo[(int)ammo])
            {
                player.Ammo[(int)ammo] = player.MaxAmmo[(int)ammo];
            }

            // If non zero ammo, don't change up weapons, player was lower on purpose.
            if (oldammo != 0)
            {
                return true;
            }

            // We were down to zero, so select a new weapon.
            // Preferences are not user selectable.
            switch (ammo)
            {
                case AmmoType.Clip:
                    if (player.ReadyWeapon == WeaponType.Fist)
                    {
                        if (player.WeaponOwned[(int)WeaponType.Chaingun])
                        {
                            player.PendingWeapon = WeaponType.Chaingun;
                        }
                        else
                        {
                            player.PendingWeapon = WeaponType.Pistol;
                        }
                    }
                    break;

                case AmmoType.Shell:
                    if (player.ReadyWeapon == WeaponType.Fist
                        || player.ReadyWeapon == WeaponType.Pistol)
                    {
                        if (player.WeaponOwned[(int)WeaponType.Shotgun])
                        {
                            player.PendingWeapon = WeaponType.Shotgun;
                        }
                    }
                    break;

                case AmmoType.Cell:
                    if (player.ReadyWeapon == WeaponType.Fist
                        || player.ReadyWeapon == WeaponType.Pistol)
                    {
                        if (player.WeaponOwned[(int)WeaponType.Plasma])
                        {
                            player.PendingWeapon = WeaponType.Plasma;
                        }
                    }
                    break;

                case AmmoType.Missile:
                    if (player.ReadyWeapon == WeaponType.Fist)
                    {
                        if (player.WeaponOwned[(int)WeaponType.Missile])
                        {
                            player.PendingWeapon = WeaponType.Missile;
                        }
                    }
                    break;

                default:
                    break;
            }

            return true;
        }


        private static readonly int bonusAdd = 6;

        /// <summary>
        /// Give the weapon to the player.
        /// </summary>
        /// <param name="dropped">
        /// True if the weapons is dropped by a monster.
        /// </param>
        public bool GiveWeapon(Player player, WeaponType weapon, bool dropped)
        {
            if (world.Options.NetGame && (world.Options.Deathmatch != 2) && !dropped)
            {
                // Leave placed weapons forever on net games.
                if (player.WeaponOwned[(int)weapon])
                {
                    return false;
                }

                player.BonusCount += bonusAdd;
                player.WeaponOwned[(int)weapon] = true;

                if (world.Options.Deathmatch != 0)
                {
                    GiveAmmo(player, DoomInfo.WeaponInfos[(int)weapon].Ammo, 5);
                }
                else
                {
                    GiveAmmo(player, DoomInfo.WeaponInfos[(int)weapon].Ammo, 2);
                }

                player.PendingWeapon = weapon;

                if (player == world.ConsolePlayer)
                {
                    world.StartSound(player.Mobj, Sfx.WPNUP, SfxType.Misc);
                }

                return false;
            }

            bool gaveAmmo;
            if (DoomInfo.WeaponInfos[(int)weapon].Ammo != AmmoType.NoAmmo)
            {
                // Give one clip with a dropped weapon, two clips with a found weapon.
                if (dropped)
                {
                    gaveAmmo = GiveAmmo(player, DoomInfo.WeaponInfos[(int)weapon].Ammo, 1);
                }
                else
                {
                    gaveAmmo = GiveAmmo(player, DoomInfo.WeaponInfos[(int)weapon].Ammo, 2);
                }
            }
            else
            {
                gaveAmmo = false;
            }

            bool gaveWeapon;
            if (player.WeaponOwned[(int)weapon])
            {
                gaveWeapon = false;
            }
            else
            {
                gaveWeapon = true;
                player.WeaponOwned[(int)weapon] = true;
                player.PendingWeapon = weapon;
            }

            return (gaveWeapon || gaveAmmo);
        }


        /// <summary>
        /// Give the health point to the player.
        /// </summary>
        /// <returns>
        /// False if the health point isn't needed at all.
        /// </returns>
        private bool GiveHealth(Player player, int amount)
        {
            if (player.Health >= DoomInfo.DeHackEdConst.InitialHealth)
            {
                return false;
            }

            player.Health += amount;
            if (player.Health > DoomInfo.DeHackEdConst.InitialHealth)
            {
                player.Health = DoomInfo.DeHackEdConst.InitialHealth;
            }

            player.Mobj.Health = player.Health;

            return true;
        }


        /// <summary>
        /// Give the armor to the player.
        /// </summary>
        /// <returns>
        /// Returns false if the armor is worse than the current armor.
        /// </returns>
        private bool GiveArmor(Player player, int type)
        {
            var hits = type * 100;

            if (player.ArmorPoints >= hits)
            {
                // Don't pick up.
                return false;
            }

            player.ArmorType = type;
            player.ArmorPoints = hits;

            return true;
        }


        /// <summary>
        /// Give the card to the player.
        /// </summary>
        private void GiveCard(Player player, CardType card)
        {
            if (player.Cards[(int)card])
            {
                return;
            }

            player.BonusCount = bonusAdd;
            player.Cards[(int)card] = true;
        }


        /// <summary>
        /// Give the power up to the player.
        /// </summary>
        /// <returns>
        /// False if the power up is not necessary.
        /// </returns>
        private bool GivePower(Player player, PowerType type)
        {
            if (type == PowerType.Invulnerability)
            {
                player.Powers[(int)type] = DoomInfo.PowerDuration.Invulnerability;
                return true;
            }

            if (type == PowerType.Invisibility)
            {
                player.Powers[(int)type] = DoomInfo.PowerDuration.Invisibility;
                player.Mobj.Flags |= MobjFlags.Shadow;
                return true;
            }

            if (type == PowerType.Infrared)
            {
                player.Powers[(int)type] = DoomInfo.PowerDuration.Infrared;
                return true;
            }

            if (type == PowerType.IronFeet)
            {
                player.Powers[(int)type] = DoomInfo.PowerDuration.IronFeet;
                return true;
            }

            if (type == PowerType.Strength)
            {
                GiveHealth(player, 100);
                player.Powers[(int)type] = 1;
                return true;
            }

            if (player.Powers[(int)type] != 0)
            {
                // Already got it.
                return false;
            }

            player.Powers[(int)type] = 1;

            return true;
        }


        /// <summary>
        /// Check for item pickup.
        /// </summary>
        public void TouchSpecialThing(Mobj special, Mobj toucher)
        {
            var delta = special.Z - toucher.Z;

            if (delta > toucher.Height || delta < Fixed.FromInt(-8))
            {
                // Out of reach.
                return;
            }

            var sound = Sfx.ITEMUP;
            var player = toucher.Player;

            // Dead thing touching.
            // Can happen with a sliding player corpse.
            if (toucher.Health <= 0)
            {
                return;
            }

            // Identify by sprite.
            switch (special.Sprite)
            {
                // Armor.
                case Sprite.ARM1:
                    if (!GiveArmor(player, DoomInfo.DeHackEdConst.GreenArmorClass))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTARMOR);
                    break;

                case Sprite.ARM2:
                    if (!GiveArmor(player, DoomInfo.DeHackEdConst.BlueArmorClass))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTMEGA);
                    break;

                // Bonus items.
                case Sprite.BON1:
                    // Can go over 100%.
                    player.Health++;
                    if (player.Health > DoomInfo.DeHackEdConst.MaxHealth)
                    {
                        player.Health = DoomInfo.DeHackEdConst.MaxHealth;
                    }
                    player.Mobj.Health = player.Health;
                    player.SendMessage(DoomInfo.Strings.GOTHTHBONUS);
                    break;

                case Sprite.BON2:
                    // Can go over 100%.
                    player.ArmorPoints++;
                    if (player.ArmorPoints > DoomInfo.DeHackEdConst.MaxArmor)
                    {
                        player.ArmorPoints = DoomInfo.DeHackEdConst.MaxArmor;
                    }
                    if (player.ArmorType == 0)
                    {
                        player.ArmorType = DoomInfo.DeHackEdConst.GreenArmorClass;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTARMBONUS);
                    break;

                case Sprite.SOUL:
                    player.Health += DoomInfo.DeHackEdConst.SoulsphereHealth;
                    if (player.Health > DoomInfo.DeHackEdConst.MaxSoulsphere)
                    {
                        player.Health = DoomInfo.DeHackEdConst.MaxSoulsphere;
                    }
                    player.Mobj.Health = player.Health;
                    player.SendMessage(DoomInfo.Strings.GOTSUPER);
                    sound = Sfx.GETPOW;
                    break;

                case Sprite.MEGA:
                    if (world.Options.GameMode != GameMode.Commercial)
                    {
                        return;
                    }

                    player.Health = DoomInfo.DeHackEdConst.MegasphereHealth;
                    player.Mobj.Health = player.Health;
                    GiveArmor(player, DoomInfo.DeHackEdConst.BlueArmorClass);
                    player.SendMessage(DoomInfo.Strings.GOTMSPHERE);
                    sound = Sfx.GETPOW;
                    break;

                // Cards.
                // Leave cards for everyone.
                case Sprite.BKEY:
                    if (!player.Cards[(int)CardType.BlueCard])
                    {
                        player.SendMessage(DoomInfo.Strings.GOTBLUECARD);
                    }
                    GiveCard(player, CardType.BlueCard);
                    if (!world.Options.NetGame)
                    {
                        break;
                    }
                    return;

                case Sprite.YKEY:
                    if (!player.Cards[(int)CardType.YellowCard])
                    {
                        player.SendMessage(DoomInfo.Strings.GOTYELWCARD);
                    }
                    GiveCard(player, CardType.YellowCard);
                    if (!world.Options.NetGame)
                    {
                        break;
                    }
                    return;

                case Sprite.RKEY:
                    if (!player.Cards[(int)CardType.RedCard])
                    {
                        player.SendMessage(DoomInfo.Strings.GOTREDCARD);
                    }
                    GiveCard(player, CardType.RedCard);
                    if (!world.Options.NetGame)
                    {
                        break;
                    }
                    return;

                case Sprite.BSKU:
                    if (!player.Cards[(int)CardType.BlueSkull])
                    {
                        player.SendMessage(DoomInfo.Strings.GOTBLUESKUL);
                    }
                    GiveCard(player, CardType.BlueSkull);
                    if (!world.Options.NetGame)
                    {
                        break;
                    }
                    return;

                case Sprite.YSKU:
                    if (!player.Cards[(int)CardType.YellowSkull])
                    {
                        player.SendMessage(DoomInfo.Strings.GOTYELWSKUL);
                    }
                    GiveCard(player, CardType.YellowSkull);
                    if (!world.Options.NetGame)
                    {
                        break;
                    }
                    return;

                case Sprite.RSKU:
                    if (!player.Cards[(int)CardType.RedSkull])
                    {
                        player.SendMessage(DoomInfo.Strings.GOTREDSKULL);
                    }
                    GiveCard(player, CardType.RedSkull);
                    if (!world.Options.NetGame)
                    {
                        break;
                    }
                    return;

                // Medikits, heals.
                case Sprite.STIM:
                    if (!GiveHealth(player, 10))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTSTIM);
                    break;

                case Sprite.MEDI:
                    if (!GiveHealth(player, 25))
                    {
                        return;
                    }
                    if (player.Health < 25)
                    {
                        player.SendMessage(DoomInfo.Strings.GOTMEDINEED);
                    }
                    else
                    {
                        player.SendMessage(DoomInfo.Strings.GOTMEDIKIT);
                    }
                    break;


                // Power ups.
                case Sprite.PINV:
                    if (!GivePower(player, PowerType.Invulnerability))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTINVUL);
                    sound = Sfx.GETPOW;
                    break;

                case Sprite.PSTR:
                    if (!GivePower(player, PowerType.Strength))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTBERSERK);
                    if (player.ReadyWeapon != WeaponType.Fist)
                    {
                        player.PendingWeapon = WeaponType.Fist;
                    }
                    sound = Sfx.GETPOW;
                    break;

                case Sprite.PINS:
                    if (!GivePower(player, PowerType.Invisibility))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTINVIS);
                    sound = Sfx.GETPOW;
                    break;

                case Sprite.SUIT:
                    if (!GivePower(player, PowerType.IronFeet))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTSUIT);
                    sound = Sfx.GETPOW;
                    break;

                case Sprite.PMAP:
                    if (!GivePower(player, PowerType.AllMap))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTMAP);
                    sound = Sfx.GETPOW;
                    break;

                case Sprite.PVIS:
                    if (!GivePower(player, PowerType.Infrared))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTVISOR);
                    sound = Sfx.GETPOW;
                    break;

                // Ammo.
                case Sprite.CLIP:
                    if ((special.Flags & MobjFlags.Dropped) != 0)
                    {
                        if (!GiveAmmo(player, AmmoType.Clip, 0))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (!GiveAmmo(player, AmmoType.Clip, 1))
                        {
                            return;
                        }
                    }
                    player.SendMessage(DoomInfo.Strings.GOTCLIP);
                    break;

                case Sprite.AMMO:
                    if (!GiveAmmo(player, AmmoType.Clip, 5))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTCLIPBOX);
                    break;

                case Sprite.ROCK:
                    if (!GiveAmmo(player, AmmoType.Missile, 1))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTROCKET);
                    break;

                case Sprite.BROK:
                    if (!GiveAmmo(player, AmmoType.Missile, 5))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTROCKBOX);
                    break;

                case Sprite.CELL:
                    if (!GiveAmmo(player, AmmoType.Cell, 1))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTCELL);
                    break;

                case Sprite.CELP:
                    if (!GiveAmmo(player, AmmoType.Cell, 5))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTCELLBOX);
                    break;

                case Sprite.SHEL:
                    if (!GiveAmmo(player, AmmoType.Shell, 1))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTSHELLS);
                    break;

                case Sprite.SBOX:
                    if (!GiveAmmo(player, AmmoType.Shell, 5))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTSHELLBOX);
                    break;

                case Sprite.BPAK:
                    if (!player.Backpack)
                    {
                        for (var i = 0; i < (int)AmmoType.Count; i++)
                        {
                            player.MaxAmmo[i] *= 2;
                        }
                        player.Backpack = true;
                    }
                    for (var i = 0; i < (int)AmmoType.Count; i++)
                    {
                        GiveAmmo(player, (AmmoType)i, 1);
                    }
                    player.SendMessage(DoomInfo.Strings.GOTBACKPACK);
                    break;

                // Weapons.
                case Sprite.BFUG:
                    if (!GiveWeapon(player, WeaponType.Bfg, false))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTBFG9000);
                    sound = Sfx.WPNUP;
                    break;

                case Sprite.MGUN:
                    if (!GiveWeapon(player, WeaponType.Chaingun, (special.Flags & MobjFlags.Dropped) != 0))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTCHAINGUN);
                    sound = Sfx.WPNUP;
                    break;

                case Sprite.CSAW:
                    if (!GiveWeapon(player, WeaponType.Chainsaw, false))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTCHAINSAW);
                    sound = Sfx.WPNUP;
                    break;

                case Sprite.LAUN:
                    if (!GiveWeapon(player, WeaponType.Missile, false))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTLAUNCHER);
                    sound = Sfx.WPNUP;
                    break;

                case Sprite.PLAS:
                    if (!GiveWeapon(player, WeaponType.Plasma, false))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTPLASMA);
                    sound = Sfx.WPNUP;
                    break;

                case Sprite.SHOT:
                    if (!GiveWeapon(player, WeaponType.Shotgun, (special.Flags & MobjFlags.Dropped) != 0))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTSHOTGUN);
                    sound = Sfx.WPNUP;
                    break;

                case Sprite.SGN2:
                    if (!GiveWeapon(player, WeaponType.SuperShotgun, (special.Flags & MobjFlags.Dropped) != 0))
                    {
                        return;
                    }
                    player.SendMessage(DoomInfo.Strings.GOTSHOTGUN2);
                    sound = Sfx.WPNUP;
                    break;

                default:
                    throw new Exception("Unknown gettable thing!");
            }

            if ((special.Flags & MobjFlags.CountItem) != 0)
            {
                player.ItemCount++;
            }

            world.ThingAllocation.RemoveMobj(special);

            player.BonusCount += bonusAdd;

            if (player == world.ConsolePlayer)
            {
                world.StartSound(player.Mobj, sound, SfxType.Misc);
            }
        }
    }
}
