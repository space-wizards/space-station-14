# SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
#
# SPDX-License-Identifier: MIT

health-change-display =
    { $deltasign ->
        [-1] [color=green]{NATURALFIXED($amount, 2)}[/color] {$kind}
        *[1] [color=red]{NATURALFIXED($amount, 2)}[/color] {$kind}
    }
