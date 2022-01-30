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

# Input detail configuration
input_name = "rplasma_window.dmi"
input_row = 7
tile_w = 32
tile_h = 32
shodan = False # Citadel Station

# Output state configuration
if not shodan:
    # TG
    out_states = [
        # Each output state gives a source quadrant for BR, TL, TR, BL.
        # The idea is that each of the 4 directions is a different rotation of the same state.
        # These states are associated by a bitfield indicating occupance relative to the indicated corner:
        # 1: Tile anti-clockwise of indicated diagonal occupied.
        # 2: Tile in indicated diagonal occupied.
        # 4: Tile clockwise of indicated diagonal occupied.
        [  0,  0,  0,  0], # 0 : Standing / Outer corners
        [ 12, 12,  3,  3], # 1 : Straight line ; top half horizontal bottom half vertical
        [  0,  0,  0,  0], # 2 : Standing / Outer corners diagonal
        [ 12, 12,  3,  3], # 3 : Seems to match 1
        [  3,  3, 12, 12], # 4 : Straight line ; top half vertical bottom half horizontal
        [ 15, 15, 15, 15], # 5 : Inner corners
        [  3,  3, 12, 12], # 6 : Seems to match 4
        [ 46, 46, 46, 46], # 7 : Full
    ]
else:
    # Citadel Station
    out_states = [
        [  3,  0,  1,  2],
        [ 11,  8,  5,  6],
        [  3,  0,  1,  2],
        [ 11,  8,  5,  6],
        [  7,  4,  9, 10],
        [ 15, 12, 13, 14],
        [  7,  4,  9, 10],
        [ 19, 16, 17, 18],
    ]

# Infer
subtile_w = tile_w // 2
subtile_h = tile_h // 2

# Source loading
src_img = PIL.Image.open(input_name)

tiles = []
# 48 is the amount of tiles that usually exist
for i in range(48):
    tile = PIL.Image.new("RGBA", (tile_w, tile_h))
    tx = i % input_row
    ty = i // input_row
    tile.paste(src_img, (tx * -tile_w, ty * -tile_h))
    # now split that up
    # note that THIS is where the weird ordering gets put into place
    tile_a = PIL.Image.new("RGBA", (subtile_w, subtile_h))
    tile_a.paste(tile, (-subtile_w, -subtile_h))
    tile_b = PIL.Image.new("RGBA", (subtile_w, subtile_h))
    tile_b.paste(tile, (0, 0))
    tile_c = PIL.Image.new("RGBA", (subtile_w, subtile_h))
    tile_c.paste(tile, (-subtile_w, 0))
    tile_d = PIL.Image.new("RGBA", (subtile_w, subtile_h))
    tile_d.paste(tile, (0, -subtile_h))
    tiles.append([tile_a, tile_b, tile_c, tile_d])

state_size = (tile_w * 2, tile_h * 2)

def subtile_copy(dst, dst_x, dst_y, src, src_x, src_y):
    dst_x += src_x
    dst_y += src_y

for state in range(len(out_states)):
    full = PIL.Image.new("RGBA", state_size)
    full.paste(tiles[out_states[state][0]][0], (subtile_w, subtile_h))
    full.paste(tiles[out_states[state][1]][1], (tile_w, 0))
    full.paste(tiles[out_states[state][2]][2], (subtile_w, tile_h))
    full.paste(tiles[out_states[state][3]][3], (tile_w, tile_h + subtile_h))
    full.save("state_" + str(state) + ".png")

full_finale = PIL.Image.new("RGBA", (tile_w, tile_h))
full_finale.paste(tiles[out_states[0][0]][0], (subtile_w, subtile_h))
full_finale.paste(tiles[out_states[0][1]][1], (0, 0))
full_finale.paste(tiles[out_states[0][2]][2], (subtile_w, 0))
full_finale.paste(tiles[out_states[0][3]][3], (0, subtile_h))
full_finale.save("full.png")

