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
        private class MobjActions
        {
            public void BFGSpray(World world, Mobj actor)
            {
                world.WeaponBehavior.BFGSpray(actor);
            }

            public void Explode(World world, Mobj actor)
            {
                world.MonsterBehavior.Explode(actor);
            }

            public void Pain(World world, Mobj actor)
            {
                world.MonsterBehavior.Pain(actor);
            }

            public void PlayerScream(World world, Mobj actor)
            {
                world.PlayerBehavior.PlayerScream(actor);
            }

            public void Fall(World world, Mobj actor)
            {
                world.MonsterBehavior.Fall(actor);
            }

            public void XScream(World world, Mobj actor)
            {
                world.MonsterBehavior.XScream(actor);
            }

            public void Look(World world, Mobj actor)
            {
                world.MonsterBehavior.Look(actor);
            }

            public void Chase(World world, Mobj actor)
            {
                world.MonsterBehavior.Chase(actor);
            }

            public void FaceTarget(World world, Mobj actor)
            {
                world.MonsterBehavior.FaceTarget(actor);
            }

            public void PosAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.PosAttack(actor);
            }

            public void Scream(World world, Mobj actor)
            {
                world.MonsterBehavior.Scream(actor);
            }

            public void SPosAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.SPosAttack(actor);
            }

            public void VileChase(World world, Mobj actor)
            {
                world.MonsterBehavior.VileChase(actor);
            }

            public void VileStart(World world, Mobj actor)
            {
                world.MonsterBehavior.VileStart(actor);
            }

            public void VileTarget(World world, Mobj actor)
            {
                world.MonsterBehavior.VileTarget(actor);
            }

            public void VileAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.VileAttack(actor);
            }

            public void StartFire(World world, Mobj actor)
            {
                world.MonsterBehavior.StartFire(actor);
            }

            public void Fire(World world, Mobj actor)
            {
                world.MonsterBehavior.Fire(actor);
            }

            public void FireCrackle(World world, Mobj actor)
            {
                world.MonsterBehavior.FireCrackle(actor);
            }

            public void Tracer(World world, Mobj actor)
            {
                world.MonsterBehavior.Tracer(actor);
            }

            public void SkelWhoosh(World world, Mobj actor)
            {
                world.MonsterBehavior.SkelWhoosh(actor);
            }

            public void SkelFist(World world, Mobj actor)
            {
                world.MonsterBehavior.SkelFist(actor);
            }

            public void SkelMissile(World world, Mobj actor)
            {
                world.MonsterBehavior.SkelMissile(actor);
            }

            public void FatRaise(World world, Mobj actor)
            {
                world.MonsterBehavior.FatRaise(actor);
            }

            public void FatAttack1(World world, Mobj actor)
            {
                world.MonsterBehavior.FatAttack1(actor);
            }

            public void FatAttack2(World world, Mobj actor)
            {
                world.MonsterBehavior.FatAttack2(actor);
            }

            public void FatAttack3(World world, Mobj actor)
            {
                world.MonsterBehavior.FatAttack3(actor);
            }

            public void BossDeath(World world, Mobj actor)
            {
                world.MonsterBehavior.BossDeath(actor);
            }

            public void CPosAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.CPosAttack(actor);
            }

            public void CPosRefire(World world, Mobj actor)
            {
                world.MonsterBehavior.CPosRefire(actor);
            }

            public void TroopAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.TroopAttack(actor);
            }

            public void SargAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.SargAttack(actor);
            }

            public void HeadAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.HeadAttack(actor);
            }

            public void BruisAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.BruisAttack(actor);
            }

            public void SkullAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.SkullAttack(actor);
            }

            public void Metal(World world, Mobj actor)
            {
                world.MonsterBehavior.Metal(actor);
            }

            public void SpidRefire(World world, Mobj actor)
            {
                world.MonsterBehavior.SpidRefire(actor);
            }

            public void BabyMetal(World world, Mobj actor)
            {
                world.MonsterBehavior.BabyMetal(actor);
            }

            public void BspiAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.BspiAttack(actor);
            }

            public void Hoof(World world, Mobj actor)
            {
                world.MonsterBehavior.Hoof(actor);
            }

            public void CyberAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.CyberAttack(actor);
            }

            public void PainAttack(World world, Mobj actor)
            {
                world.MonsterBehavior.PainAttack(actor);
            }

            public void PainDie(World world, Mobj actor)
            {
                world.MonsterBehavior.PainDie(actor);
            }

            public void KeenDie(World world, Mobj actor)
            {
                world.MonsterBehavior.KeenDie(actor);
            }

            public void BrainPain(World world, Mobj actor)
            {
                world.MonsterBehavior.BrainPain(actor);
            }

            public void BrainScream(World world, Mobj actor)
            {
                world.MonsterBehavior.BrainScream(actor);
            }

            public void BrainDie(World world, Mobj actor)
            {
                world.MonsterBehavior.BrainDie(actor);
            }

            public void BrainAwake(World world, Mobj actor)
            {
                world.MonsterBehavior.BrainAwake(actor);
            }

            public void BrainSpit(World world, Mobj actor)
            {
                world.MonsterBehavior.BrainSpit(actor);
            }

            public void SpawnSound(World world, Mobj actor)
            {
                world.MonsterBehavior.SpawnSound(actor);
            }

            public void SpawnFly(World world, Mobj actor)
            {
                world.MonsterBehavior.SpawnFly(actor);
            }

            public void BrainExplode(World world, Mobj actor)
            {
                world.MonsterBehavior.BrainExplode(actor);
            }
        }
    }
}
