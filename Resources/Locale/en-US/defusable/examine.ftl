# SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
#
# SPDX-License-Identifier: MIT

defusable-examine-defused = {CAPITALIZE(THE($name))} is [color=lime]defused[/color].
defusable-examine-live = {CAPITALIZE(THE($name))} is [color=red]ticking[/color] and has [color=red]{$time}[/color] seconds remaining.
defusable-examine-live-display-off = {CAPITALIZE(THE($name))} is [color=red]ticking[/color], and the timer appears to be off.
defusable-examine-inactive = {CAPITALIZE(THE($name))} is [color=lime]inactive[/color], but can still be armed.
defusable-examine-bolts = The bolts are {$down ->
[true] [color=red]down[/color]
*[false] [color=green]up[/color]
}.
