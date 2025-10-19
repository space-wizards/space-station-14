// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Threading;

namespace Content.Server.Power.Pow3r
{
    public interface IPowerSolver
    {
        void Tick(float frameTime, PowerState state, IParallelManager parallel);
        void Validate(PowerState state);
    }
}
