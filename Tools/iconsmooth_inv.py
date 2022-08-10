#!/usr/bin/env python3

# Copyright (c) 2022 Space Wizards Federation
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
    print("iconsmooth_inv.py INPREFIX METRICS <" + iconsmooth_lib.all_conv + "> out.png")
    print("INPREFIX is something like, say, " + iconsmooth_lib.explain_prefix)
    print(iconsmooth_lib.explain_mm)
    raise Exception("see printed help")
# Input detail configuration
input_prefix = sys.argv[1]
metric_mode = sys.argv[2]
conversion_mode = iconsmooth_lib.conversion_modes[sys.argv[3]]
output_name = sys.argv[4]

tile_w, tile_h, subtile_w, subtile_h, remtile_w, remtile_h = iconsmooth_lib.parse_metric_mode(metric_mode)

# Source loading
tiles = []

for j in range(8):
    src_img = PIL.Image.open(input_prefix + str(j) + ".png")

    tile_a = PIL.Image.new("RGBA", (remtile_w, remtile_h))
    tile_a.paste(src_img, (-subtile_w, -subtile_h))
    tile_b = PIL.Image.new("RGBA", (subtile_w, subtile_h))
    tile_b.paste(src_img, (-tile_w, 0))
    tile_c = PIL.Image.new("RGBA", (remtile_w, subtile_h))
    tile_c.paste(src_img, (-subtile_w, -tile_h))
    tile_d = PIL.Image.new("RGBA", (subtile_w, remtile_h))
    tile_d.paste(src_img, (-tile_w, -(tile_h + subtile_h)))
    tiles.append([tile_a, tile_b, tile_c, tile_d])

# Prepare finale
output_tw = conversion_mode.tw
output_th = conversion_mode.th

full_finale = PIL.Image.new("RGBA", (tile_w * output_tw, tile_h * output_th))

# State table to be inverted
out_states = conversion_mode.states

# Directions to subtile offsets
subtile_ofx = [1, 0, 1, 0]
subtile_ofy = [1, 0, 0, 1]

for i in [7, 6, 5, 4, 3, 2, 1, 0]:
    for j in range(4):
        target_tile = out_states[i][j]
        if target_tile != -1:
            target_stx = (target_tile % output_tw) * tile_w
            target_sty = (target_tile // output_tw) * tile_h
            target_stx += subtile_ofx[j] * subtile_w
            target_sty += subtile_ofy[j] * subtile_h
            full_finale.paste(tiles[i][j], (target_stx, target_sty))

# Done!
full_finale.save(output_name)

