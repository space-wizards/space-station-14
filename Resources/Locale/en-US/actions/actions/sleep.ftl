# SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
#
# SPDX-License-Identifier: MIT

action-name-wake = Wake up

sleep-onomatopoeia = Zzz...
sleep-examined = [color=lightblue]{CAPITALIZE(SUBJECT($target))} {CONJUGATE-BE($target)} asleep.[/color]

wake-other-success = You shake {THE($target)} awake.
wake-other-failure = You shake {THE($target)}, but {SUBJECT($target)} {CONJUGATE-BE($target)} not waking up.
