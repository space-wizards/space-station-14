#!/usr/bin/env python3

# Copyright (c) 2021 Space Wizards Federation
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

import PIL
import PIL.Image
import sys
import iconsmooth_lib

if len(sys.argv) != 5:
    print("iconsmooth.py in.png METRICS <" + iconsmooth_lib.all_conv + "> OUTPREFIX")
    print("OUTPREFIX is something like, say, " + iconsmooth_lib.explain_prefix)
    print(iconsmooth_lib.explain_mm)
    raise Exception("see printed help")

# Input detail configuration
input_name = sys.argv[1]
metric_mode = sys.argv[2]
conversion_mode = sys.argv[3]
out_prefix = sys.argv[4]

# Metric configuration
tile_w, tile_h, subtile_w, subtile_h, remtile_w, remtile_h = iconsmooth_lib.parse_metric_mode(metric_mode)

# Output state configuration
out_states = iconsmooth_lib.conversion_modes[conversion_mode].states

# Source loading
src_img = PIL.Image.open(input_name)

input_row = src_img.size[0] // tile_w

tiles = []
# 48 is the amount of tiles that usually exist, but 56 covers walls with diagonal variants.
for i in range(56):
    tile = PIL.Image.new("RGBA", (tile_w, tile_h))
    tx = i % input_row
    ty = i // input_row
    tile.paste(src_img, (tx * -tile_w, ty * -tile_h))
    # now split that up
    # note that THIS is where the weird ordering gets put into place
    tile_a = PIL.Image.new("RGBA", (remtile_w, remtile_h))
    tile_a.paste(tile,           (-subtile_w, -subtile_h))
    tile_b = PIL.Image.new("RGBA", (subtile_w, subtile_h))
    tile_b.paste(tile,           (         0,          0))
    tile_c = PIL.Image.new("RGBA", (remtile_w, subtile_h))
    tile_c.paste(tile,           (-subtile_w,          0))
    tile_d = PIL.Image.new("RGBA", (subtile_w, remtile_h))
    tile_d.paste(tile,           (         0, -subtile_h))
    tiles.append([tile_a, tile_b, tile_c, tile_d])

state_size = (tile_w * 2, tile_h * 2)

for state in range(len(out_states)):
    full = PIL.Image.new("RGBA", state_size)
    full.paste(tiles[out_states[state][0]][0], (subtile_w, subtile_h))
    full.paste(tiles[out_states[state][1]][1], (tile_w, 0))
    full.paste(tiles[out_states[state][2]][2], (subtile_w, tile_h))
    full.paste(tiles[out_states[state][3]][3], (tile_w, tile_h + subtile_h))
    full.save(out_prefix + str(state) + ".png")

full_finale = PIL.Image.new("RGBA", (tile_w, tile_h))
full_finale.paste(tiles[out_states[0][0]][0], (subtile_w, subtile_h))
full_finale.paste(tiles[out_states[0][1]][1], (0, 0))
full_finale.paste(tiles[out_states[0][2]][2], (subtile_w, 0))
full_finale.paste(tiles[out_states[0][3]][3], (0, subtile_h))
full_finale.save(out_prefix + "full.png")

