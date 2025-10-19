# SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
#
# SPDX-License-Identifier: MIT

multi-handed-item-pick-up-fail = {$number -> 
    [one] You need one more free hand to pick up { THE($item) }.
    *[other] You need { $number } more free hands to pick up { THE($item) }.
}
