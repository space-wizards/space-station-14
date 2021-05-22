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
    public static partial class DoomInfo
    {
        public static readonly MobjInfo[] MobjInfos = new MobjInfo[]
        {
            new MobjInfo( // MobjType.Player
                -1, // doomEdNum
                MobjState.Play, // spawnState
                100, // spawnHealth
                MobjState.PlayRun1, // seeState
                Sfx.NONE, // seeSound
                0, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.PlayPain, // painState
                255, // painChance
                Sfx.PLPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.PlayAtk1, // missileState
                MobjState.PlayDie1, // deathState
                MobjState.PlayXdie1, // xdeathState
                Sfx.PLDETH, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(56), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.DropOff | MobjFlags.PickUp | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Possessed
                3004, // doomEdNum
                MobjState.PossStnd, // spawnState
                20, // spawnHealth
                MobjState.PossRun1, // seeState
                Sfx.POSIT1, // seeSound
                8, // reactionTime
                Sfx.PISTOL, // attackSound
                MobjState.PossPain, // painState
                200, // painChance
                Sfx.POPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.PossAtk1, // missileState
                MobjState.PossDie1, // deathState
                MobjState.PossXdie1, // xdeathState
                Sfx.PODTH1, // deathSound
                8, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                100, // mass
                0, // damage
                Sfx.POSACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.PossRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Shotguy
                9, // doomEdNum
                MobjState.SposStnd, // spawnState
                30, // spawnHealth
                MobjState.SposRun1, // seeState
                Sfx.POSIT2, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.SposPain, // painState
                170, // painChance
                Sfx.POPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.SposAtk1, // missileState
                MobjState.SposDie1, // deathState
                MobjState.SposXdie1, // xdeathState
                Sfx.PODTH2, // deathSound
                8, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                100, // mass
                0, // damage
                Sfx.POSACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.SposRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Vile
                64, // doomEdNum
                MobjState.VileStnd, // spawnState
                700, // spawnHealth
                MobjState.VileRun1, // seeState
                Sfx.VILSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.VilePain, // painState
                10, // painChance
                Sfx.VIPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.VileAtk1, // missileState
                MobjState.VileDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.VILDTH, // deathSound
                15, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                500, // mass
                0, // damage
                Sfx.VILACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Fire
                -1, // doomEdNum
                MobjState.Fire1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Undead
                66, // doomEdNum
                MobjState.SkelStnd, // spawnState
                300, // spawnHealth
                MobjState.SkelRun1, // seeState
                Sfx.SKESIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.SkelPain, // painState
                100, // painChance
                Sfx.POPAIN, // painSound
                MobjState.SkelFist1, // meleeState
                MobjState.SkelMiss1, // missileState
                MobjState.SkelDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.SKEDTH, // deathSound
                10, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                500, // mass
                0, // damage
                Sfx.SKEACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.SkelRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Tracer
                -1, // doomEdNum
                MobjState.Tracer, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.SKEATK, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Traceexp1, // deathState
                MobjState.Null, // xdeathState
                Sfx.BAREXP, // deathSound
                10 * Fixed.FracUnit, // speed
                Fixed.FromInt(11), // radius
                Fixed.FromInt(8), // height
                100, // mass
                10, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Smoke
                -1, // doomEdNum
                MobjState.Smoke1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Fatso
                67, // doomEdNum
                MobjState.FattStnd, // spawnState
                600, // spawnHealth
                MobjState.FattRun1, // seeState
                Sfx.MANSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.FattPain, // painState
                80, // painChance
                Sfx.MNPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.FattAtk1, // missileState
                MobjState.FattDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.MANDTH, // deathSound
                8, // speed
                Fixed.FromInt(48), // radius
                Fixed.FromInt(64), // height
                1000, // mass
                0, // damage
                Sfx.POSACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.FattRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Fatshot
                -1, // doomEdNum
                MobjState.Fatshot1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.FIRSHT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Fatshotx1, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                20 * Fixed.FracUnit, // speed
                Fixed.FromInt(6), // radius
                Fixed.FromInt(8), // height
                100, // mass
                8, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Chainguy
                65, // doomEdNum
                MobjState.CposStnd, // spawnState
                70, // spawnHealth
                MobjState.CposRun1, // seeState
                Sfx.POSIT2, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.CposPain, // painState
                170, // painChance
                Sfx.POPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.CposAtk1, // missileState
                MobjState.CposDie1, // deathState
                MobjState.CposXdie1, // xdeathState
                Sfx.PODTH2, // deathSound
                8, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                100, // mass
                0, // damage
                Sfx.POSACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.CposRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Troop
                3001, // doomEdNum
                MobjState.TrooStnd, // spawnState
                60, // spawnHealth
                MobjState.TrooRun1, // seeState
                Sfx.BGSIT1, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.TrooPain, // painState
                200, // painChance
                Sfx.POPAIN, // painSound
                MobjState.TrooAtk1, // meleeState
                MobjState.TrooAtk1, // missileState
                MobjState.TrooDie1, // deathState
                MobjState.TrooXdie1, // xdeathState
                Sfx.BGDTH1, // deathSound
                8, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                100, // mass
                0, // damage
                Sfx.BGACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.TrooRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Sergeant
                3002, // doomEdNum
                MobjState.SargStnd, // spawnState
                150, // spawnHealth
                MobjState.SargRun1, // seeState
                Sfx.SGTSIT, // seeSound
                8, // reactionTime
                Sfx.SGTATK, // attackSound
                MobjState.SargPain, // painState
                180, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.SargAtk1, // meleeState
                MobjState.Null, // missileState
                MobjState.SargDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.SGTDTH, // deathSound
                10, // speed
                Fixed.FromInt(30), // radius
                Fixed.FromInt(56), // height
                400, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.SargRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Shadows
                58, // doomEdNum
                MobjState.SargStnd, // spawnState
                150, // spawnHealth
                MobjState.SargRun1, // seeState
                Sfx.SGTSIT, // seeSound
                8, // reactionTime
                Sfx.SGTATK, // attackSound
                MobjState.SargPain, // painState
                180, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.SargAtk1, // meleeState
                MobjState.Null, // missileState
                MobjState.SargDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.SGTDTH, // deathSound
                10, // speed
                Fixed.FromInt(30), // radius
                Fixed.FromInt(56), // height
                400, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.Shadow | MobjFlags.CountKill, // flags
                MobjState.SargRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Head
                3005, // doomEdNum
                MobjState.HeadStnd, // spawnState
                400, // spawnHealth
                MobjState.HeadRun1, // seeState
                Sfx.CACSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.HeadPain, // painState
                128, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.HeadAtk1, // missileState
                MobjState.HeadDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.CACDTH, // deathSound
                8, // speed
                Fixed.FromInt(31), // radius
                Fixed.FromInt(56), // height
                400, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.Float | MobjFlags.NoGravity | MobjFlags.CountKill, // flags
                MobjState.HeadRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Bruiser
                3003, // doomEdNum
                MobjState.BossStnd, // spawnState
                1000, // spawnHealth
                MobjState.BossRun1, // seeState
                Sfx.BRSSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.BossPain, // painState
                50, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.BossAtk1, // meleeState
                MobjState.BossAtk1, // missileState
                MobjState.BossDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.BRSDTH, // deathSound
                8, // speed
                Fixed.FromInt(24), // radius
                Fixed.FromInt(64), // height
                1000, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.BossRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Bruisershot
                -1, // doomEdNum
                MobjState.Brball1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.FIRSHT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Brballx1, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                15 * Fixed.FracUnit, // speed
                Fixed.FromInt(6), // radius
                Fixed.FromInt(8), // height
                100, // mass
                8, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Knight
                69, // doomEdNum
                MobjState.Bos2Stnd, // spawnState
                500, // spawnHealth
                MobjState.Bos2Run1, // seeState
                Sfx.KNTSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Bos2Pain, // painState
                50, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.Bos2Atk1, // meleeState
                MobjState.Bos2Atk1, // missileState
                MobjState.Bos2Die1, // deathState
                MobjState.Null, // xdeathState
                Sfx.KNTDTH, // deathSound
                8, // speed
                Fixed.FromInt(24), // radius
                Fixed.FromInt(64), // height
                1000, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.Bos2Raise1 // raiseState
            ),

            new MobjInfo( // MobjType.Skull
                3006, // doomEdNum
                MobjState.SkullStnd, // spawnState
                100, // spawnHealth
                MobjState.SkullRun1, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.SKLATK, // attackSound
                MobjState.SkullPain, // painState
                256, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.SkullAtk1, // missileState
                MobjState.SkullDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                8, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(56), // height
                50, // mass
                3, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.Float | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Spider
                7, // doomEdNum
                MobjState.SpidStnd, // spawnState
                3000, // spawnHealth
                MobjState.SpidRun1, // seeState
                Sfx.SPISIT, // seeSound
                8, // reactionTime
                Sfx.SHOTGN, // attackSound
                MobjState.SpidPain, // painState
                40, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.SpidAtk1, // missileState
                MobjState.SpidDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.SPIDTH, // deathSound
                12, // speed
                Fixed.FromInt(128), // radius
                Fixed.FromInt(100), // height
                1000, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Baby
                68, // doomEdNum
                MobjState.BspiStnd, // spawnState
                500, // spawnHealth
                MobjState.BspiSight, // seeState
                Sfx.BSPSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.BspiPain, // painState
                128, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.BspiAtk1, // missileState
                MobjState.BspiDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.BSPDTH, // deathSound
                12, // speed
                Fixed.FromInt(64), // radius
                Fixed.FromInt(64), // height
                600, // mass
                0, // damage
                Sfx.BSPACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.BspiRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Cyborg
                16, // doomEdNum
                MobjState.CyberStnd, // spawnState
                4000, // spawnHealth
                MobjState.CyberRun1, // seeState
                Sfx.CYBSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.CyberPain, // painState
                20, // painChance
                Sfx.DMPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.CyberAtk1, // missileState
                MobjState.CyberDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.CYBDTH, // deathSound
                16, // speed
                Fixed.FromInt(40), // radius
                Fixed.FromInt(110), // height
                1000, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Pain
                71, // doomEdNum
                MobjState.PainStnd, // spawnState
                400, // spawnHealth
                MobjState.PainRun1, // seeState
                Sfx.PESIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.PainPain, // painState
                128, // painChance
                Sfx.PEPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.PainAtk1, // missileState
                MobjState.PainDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.PEDTH, // deathSound
                8, // speed
                Fixed.FromInt(31), // radius
                Fixed.FromInt(56), // height
                400, // mass
                0, // damage
                Sfx.DMACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.Float | MobjFlags.NoGravity | MobjFlags.CountKill, // flags
                MobjState.PainRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Wolfss
                84, // doomEdNum
                MobjState.SswvStnd, // spawnState
                50, // spawnHealth
                MobjState.SswvRun1, // seeState
                Sfx.SSSIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.SswvPain, // painState
                170, // painChance
                Sfx.POPAIN, // painSound
                MobjState.Null, // meleeState
                MobjState.SswvAtk1, // missileState
                MobjState.SswvDie1, // deathState
                MobjState.SswvXdie1, // xdeathState
                Sfx.SSDTH, // deathSound
                8, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(56), // height
                100, // mass
                0, // damage
                Sfx.POSACT, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.SswvRaise1 // raiseState
            ),

            new MobjInfo( // MobjType.Keen
                72, // doomEdNum
                MobjState.Keenstnd, // spawnState
                100, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Keenpain, // painState
                256, // painChance
                Sfx.KEENPN, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Commkeen, // deathState
                MobjState.Null, // xdeathState
                Sfx.KEENDT, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(72), // height
                10000000, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity | MobjFlags.Shootable | MobjFlags.CountKill, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Bossbrain
                88, // doomEdNum
                MobjState.Brain, // spawnState
                250, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.BrainPain, // painState
                255, // painChance
                Sfx.BOSPN, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.BrainDie1, // deathState
                MobjState.Null, // xdeathState
                Sfx.BOSDTH, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                10000000, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Bossspit
                89, // doomEdNum
                MobjState.Braineye, // spawnState
                1000, // spawnHealth
                MobjState.Braineyesee, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(32), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoSector, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Bosstarget
                87, // doomEdNum
                MobjState.Null, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(32), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoSector, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Spawnshot
                -1, // doomEdNum
                MobjState.Spawn1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.BOSPIT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                10 * Fixed.FracUnit, // speed
                Fixed.FromInt(6), // radius
                Fixed.FromInt(32), // height
                100, // mass
                3, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity | MobjFlags.NoClip, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Spawnfire
                -1, // doomEdNum
                MobjState.Spawnfire1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Barrel
                2035, // doomEdNum
                MobjState.Bar1, // spawnState
                20, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Bexp, // deathState
                MobjState.Null, // xdeathState
                Sfx.BAREXP, // deathSound
                0, // speed
                Fixed.FromInt(10), // radius
                Fixed.FromInt(42), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.NoBlood, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Troopshot
                -1, // doomEdNum
                MobjState.Tball1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.FIRSHT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Tballx1, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                10 * Fixed.FracUnit, // speed
                Fixed.FromInt(6), // radius
                Fixed.FromInt(8), // height
                100, // mass
                3, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Headshot
                -1, // doomEdNum
                MobjState.Rball1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.FIRSHT, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Rballx1, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                10 * Fixed.FracUnit, // speed
                Fixed.FromInt(6), // radius
                Fixed.FromInt(8), // height
                100, // mass
                5, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Rocket
                -1, // doomEdNum
                MobjState.Rocket, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.RLAUNC, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Explode1, // deathState
                MobjState.Null, // xdeathState
                Sfx.BAREXP, // deathSound
                20 * Fixed.FracUnit, // speed
                Fixed.FromInt(11), // radius
                Fixed.FromInt(8), // height
                100, // mass
                20, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Plasma
                -1, // doomEdNum
                MobjState.Plasball, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.PLASMA, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Plasexp, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                25 * Fixed.FracUnit, // speed
                Fixed.FromInt(13), // radius
                Fixed.FromInt(8), // height
                100, // mass
                5, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Bfg
                -1, // doomEdNum
                MobjState.Bfgshot, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Bfgland, // deathState
                MobjState.Null, // xdeathState
                Sfx.RXPLOD, // deathSound
                25 * Fixed.FracUnit, // speed
                Fixed.FromInt(13), // radius
                Fixed.FromInt(8), // height
                100, // mass
                100, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Arachplaz
                -1, // doomEdNum
                MobjState.ArachPlaz, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.PLASMA, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.ArachPlex, // deathState
                MobjState.Null, // xdeathState
                Sfx.FIRXPL, // deathSound
                25 * Fixed.FracUnit, // speed
                Fixed.FromInt(13), // radius
                Fixed.FromInt(8), // height
                100, // mass
                5, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.Missile | MobjFlags.DropOff | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Puff
                -1, // doomEdNum
                MobjState.Puff1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Blood
                -1, // doomEdNum
                MobjState.Blood1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Tfog
                -1, // doomEdNum
                MobjState.Tfog, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Ifog
                -1, // doomEdNum
                MobjState.Ifog, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Teleportman
                14, // doomEdNum
                MobjState.Null, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoSector, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Extrabfg
                -1, // doomEdNum
                MobjState.Bfgexp, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc0
                2018, // doomEdNum
                MobjState.Arm1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc1
                2019, // doomEdNum
                MobjState.Arm2, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc2
                2014, // doomEdNum
                MobjState.Bon1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc3
                2015, // doomEdNum
                MobjState.Bon2, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc4
                5, // doomEdNum
                MobjState.Bkey, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc5
                13, // doomEdNum
                MobjState.Rkey, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc6
                6, // doomEdNum
                MobjState.Ykey, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc7
                39, // doomEdNum
                MobjState.Yskull, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc8
                38, // doomEdNum
                MobjState.Rskull, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc9
                40, // doomEdNum
                MobjState.Bskull, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.NotDeathmatch, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc10
                2011, // doomEdNum
                MobjState.Stim, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc11
                2012, // doomEdNum
                MobjState.Medi, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc12
                2013, // doomEdNum
                MobjState.Soul, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Inv
                2022, // doomEdNum
                MobjState.Pinv, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc13
                2023, // doomEdNum
                MobjState.Pstr, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Ins
                2024, // doomEdNum
                MobjState.Pins, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc14
                2025, // doomEdNum
                MobjState.Suit, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc15
                2026, // doomEdNum
                MobjState.Pmap, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc16
                2045, // doomEdNum
                MobjState.Pvis, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Mega
                83, // doomEdNum
                MobjState.Mega, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special | MobjFlags.CountItem, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Clip
                2007, // doomEdNum
                MobjState.Clip, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc17
                2048, // doomEdNum
                MobjState.Ammo, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc18
                2010, // doomEdNum
                MobjState.Rock, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc19
                2046, // doomEdNum
                MobjState.Brok, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc20
                2047, // doomEdNum
                MobjState.Cell, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc21
                17, // doomEdNum
                MobjState.Celp, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc22
                2008, // doomEdNum
                MobjState.Shel, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc23
                2049, // doomEdNum
                MobjState.Sbox, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc24
                8, // doomEdNum
                MobjState.Bpak, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc25
                2006, // doomEdNum
                MobjState.Bfug, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Chaingun
                2002, // doomEdNum
                MobjState.Mgun, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc26
                2005, // doomEdNum
                MobjState.Csaw, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc27
                2003, // doomEdNum
                MobjState.Laun, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc28
                2004, // doomEdNum
                MobjState.Plas, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Shotgun
                2001, // doomEdNum
                MobjState.Shot, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Supershotgun
                82, // doomEdNum
                MobjState.Shot2, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Special, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc29
                85, // doomEdNum
                MobjState.Techlamp, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc30
                86, // doomEdNum
                MobjState.Tech2Lamp, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc31
                2028, // doomEdNum
                MobjState.Colu, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc32
                30, // doomEdNum
                MobjState.Tallgrncol, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc33
                31, // doomEdNum
                MobjState.Shrtgrncol, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc34
                32, // doomEdNum
                MobjState.Tallredcol, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc35
                33, // doomEdNum
                MobjState.Shrtredcol, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc36
                37, // doomEdNum
                MobjState.Skullcol, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc37
                36, // doomEdNum
                MobjState.Heartcol, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc38
                41, // doomEdNum
                MobjState.Evileye, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc39
                42, // doomEdNum
                MobjState.Floatskull, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc40
                43, // doomEdNum
                MobjState.Torchtree, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc41
                44, // doomEdNum
                MobjState.Bluetorch, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc42
                45, // doomEdNum
                MobjState.Greentorch, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc43
                46, // doomEdNum
                MobjState.Redtorch, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc44
                55, // doomEdNum
                MobjState.Btorchshrt, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc45
                56, // doomEdNum
                MobjState.Gtorchshrt, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc46
                57, // doomEdNum
                MobjState.Rtorchshrt, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc47
                47, // doomEdNum
                MobjState.Stalagtite, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc48
                48, // doomEdNum
                MobjState.Techpillar, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc49
                34, // doomEdNum
                MobjState.Candlestik, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc50
                35, // doomEdNum
                MobjState.Candelabra, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc51
                49, // doomEdNum
                MobjState.Bloodytwitch, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(68), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc52
                50, // doomEdNum
                MobjState.Meat2, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(84), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc53
                51, // doomEdNum
                MobjState.Meat3, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(84), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc54
                52, // doomEdNum
                MobjState.Meat4, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(68), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc55
                53, // doomEdNum
                MobjState.Meat5, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(52), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc56
                59, // doomEdNum
                MobjState.Meat2, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(84), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc57
                60, // doomEdNum
                MobjState.Meat4, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(68), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc58
                61, // doomEdNum
                MobjState.Meat3, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(52), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc59
                62, // doomEdNum
                MobjState.Meat5, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(52), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc60
                63, // doomEdNum
                MobjState.Bloodytwitch, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(68), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc61
                22, // doomEdNum
                MobjState.HeadDie6, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc62
                15, // doomEdNum
                MobjState.PlayDie7, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc63
                18, // doomEdNum
                MobjState.PossDie5, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc64
                21, // doomEdNum
                MobjState.SargDie6, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc65
                23, // doomEdNum
                MobjState.SkullDie6, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc66
                20, // doomEdNum
                MobjState.TrooDie5, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc67
                19, // doomEdNum
                MobjState.SposDie5, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc68
                10, // doomEdNum
                MobjState.PlayXdie9, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc69
                12, // doomEdNum
                MobjState.PlayXdie9, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc70
                28, // doomEdNum
                MobjState.Headsonstick, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc71
                24, // doomEdNum
                MobjState.Gibs, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                0, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc72
                27, // doomEdNum
                MobjState.Headonastick, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc73
                29, // doomEdNum
                MobjState.Headcandles, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc74
                25, // doomEdNum
                MobjState.Deadstick, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc75
                26, // doomEdNum
                MobjState.Livestick, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc76
                54, // doomEdNum
                MobjState.Bigtree, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(32), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc77
                70, // doomEdNum
                MobjState.Bbar1, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc78
                73, // doomEdNum
                MobjState.Hangnoguts, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(88), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc79
                74, // doomEdNum
                MobjState.Hangbnobrain, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(88), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc80
                75, // doomEdNum
                MobjState.Hangtlookdn, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(64), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc81
                76, // doomEdNum
                MobjState.Hangtskull, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(64), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc82
                77, // doomEdNum
                MobjState.Hangtlookup, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(64), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc83
                78, // doomEdNum
                MobjState.Hangtnobrain, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(16), // radius
                Fixed.FromInt(64), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.Solid | MobjFlags.SpawnCeiling | MobjFlags.NoGravity, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc84
                79, // doomEdNum
                MobjState.Colongibs, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc85
                80, // doomEdNum
                MobjState.Smallpool, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap, // flags
                MobjState.Null // raiseState
            ),

            new MobjInfo( // MobjType.Misc86
                81, // doomEdNum
                MobjState.Brainstem, // spawnState
                1000, // spawnHealth
                MobjState.Null, // seeState
                Sfx.NONE, // seeSound
                8, // reactionTime
                Sfx.NONE, // attackSound
                MobjState.Null, // painState
                0, // painChance
                Sfx.NONE, // painSound
                MobjState.Null, // meleeState
                MobjState.Null, // missileState
                MobjState.Null, // deathState
                MobjState.Null, // xdeathState
                Sfx.NONE, // deathSound
                0, // speed
                Fixed.FromInt(20), // radius
                Fixed.FromInt(16), // height
                100, // mass
                0, // damage
                Sfx.NONE, // activeSound
                MobjFlags.NoBlockMap, // flags
                MobjState.Null // raiseState
            )

        };
    }
}
