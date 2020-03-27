# Tools for working with DreamMaker maps

import io
import bidict
import random
from collections import namedtuple

TGM_HEADER = "//MAP CONVERTED BY dmm2tgm.py THIS HEADER COMMENT PREVENTS RECONVERSION, DO NOT REMOVE"
ENCODING = 'utf-8'

Coordinate = namedtuple('Coordinate', ['x', 'y', 'z'])

class DMM:
    __slots__ = ['key_length', 'size', 'dictionary', 'grid', 'header']

    def __init__(self, key_length, size):
        self.key_length = key_length
        self.size = size
        self.dictionary = bidict.bidict()
        self.grid = {}
        self.header = None

    @staticmethod
    def from_file(fname):
        # stream the file rather than forcing all its contents to memory
        with open(fname, 'r', encoding=ENCODING) as f:
            return _parse(iter(lambda: f.read(1), ''))

    @staticmethod
    def from_bytes(bytes):
        return _parse(bytes.decode(ENCODING))

    def to_file(self, fname, tgm = True):
        self._presave_checks()
        with open(fname, 'w', newline='\n', encoding=ENCODING) as f:
            (save_tgm if tgm else save_dmm)(self, f)

    def to_bytes(self, tgm = True):
        self._presave_checks()
        bio = io.BytesIO()
        with io.TextIOWrapper(bio, newline='\n', encoding=ENCODING) as f:
            (save_tgm if tgm else save_dmm)(self, f)
            f.flush()
            return bio.getvalue()

    def generate_new_key(self):
        free_keys = self._ensure_free_keys(1)
        # choose one of the free keys at random
        key = 0
        while free_keys:
            if key not in self.dictionary:
                # this construction is used to avoid needing to construct the
                # full set in order to random.choice() from it
                if random.random() < 1 / free_keys:
                    return key
                free_keys -= 1
            key += 1

        raise RuntimeError("ran out of keys, this shouldn't happen")

    def overwrite_key(self, key, fixed, bad_keys):
        try:
            self.dictionary[key] = fixed
            return None
        except bidict.DuplicationError:
            old_key = self.dictionary.inv[fixed]
            bad_keys[key] = old_key
            print(f"Merging '{num_to_key(key, self.key_length)}' into '{num_to_key(old_key, self.key_length)}'")
            return old_key

    def reassign_bad_keys(self, bad_keys):
        if not bad_keys:
            return
        for k, v in self.grid.items():
            # reassign the grid entries which used the old key
            self.grid[k] = bad_keys.get(v, v)

    def _presave_checks(self):
        # last-second handling of bogus keys to help prevent and fix broken maps
        self._ensure_free_keys(0)
        max_key = max_key_for(self.key_length)
        bad_keys = {key: 0 for key in self.dictionary.keys() if key > max_key}
        if bad_keys:
            print(f"Warning: fixing {len(bad_keys)} overflowing keys")
            for k in bad_keys:
                # create a new non-bogus key and transfer that value to it
                new_key = bad_keys[k] = self.generate_new_key()
                self.dictionary.forceput(new_key, self.dictionary[k])
                print(f"    {num_to_key(k, self.key_length, True)} -> {num_to_key(new_key, self.key_length)}")

        # handle entries in the dictionary which have atoms in the wrong order
        keys = list(self.dictionary.keys())
        for key in keys:
            value = self.dictionary[key]
            if is_bad_atom_ordering(num_to_key(key, self.key_length, True), value):
                fixed = tuple(fix_atom_ordering(value))
                self.overwrite_key(key, fixed, bad_keys)

        self.reassign_bad_keys(bad_keys)

    def _ensure_free_keys(self, desired):
        # ensure that free keys exist by increasing the key length if necessary
        free_keys = max_key_for(self.key_length) - len(self.dictionary)
        while free_keys < desired:
            if self.key_length >= MAX_KEY_LENGTH:
                raise KeyTooLarge(f"can't expand beyond key length {MAX_KEY_LENGTH} ({len(self.dictionary)} keys)")
            self.key_length += 1
            free_keys = max_key_for(self.key_length) - len(self.dictionary)
        return free_keys

    @property
    def coords_zyx(self):
        for z in range(1, self.size.z + 1):
            for y in range(1, self.size.y + 1):
                for x in range(1, self.size.x + 1):
                    yield (z, y, x)

    @property
    def coords_z(self):
        return range(1, self.size.z + 1)

    @property
    def coords_yx(self):
        for y in range(1, self.size.y + 1):
            for x in range(1, self.size.x + 1):
                yield (y, x)

# ----------
# key handling

# Base 52 a-z A-Z dictionary for fast conversion
MAX_KEY_LENGTH = 3  # things will get ugly fast if you exceed this
BASE = 52
base52 = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
base52_r = {x: i for i, x in enumerate(base52)}
assert len(base52) == BASE and len(base52_r) == BASE

def key_to_num(key):
    num = 0
    for ch in key:
        num = BASE * num + base52_r[ch]
    return num

def num_to_key(num, key_length, allow_overflow=False):
    if num >= (BASE ** key_length if allow_overflow else max_key_for(key_length)):
        raise KeyTooLarge(f"num={num} does not fit in key_length={key_length}")

    result = ''
    while num:
        result = base52[num % BASE] + result
        num //= BASE

    assert len(result) <= key_length
    return base52[0] * (key_length - len(result)) + result

def max_key_for(key_length):
    # keys only go up to "ymo" = 65534, under-estimated just in case
    # https://secure.byond.com/forum/?post=2340796#comment23770802
    return min(65530, BASE ** key_length)

class KeyTooLarge(Exception):
    pass

# ----------
# An actual atom parser

def parse_map_atom(atom):
    try:
        i = atom.index('{')
    except ValueError:
        return atom, {}

    path, rest = atom[:i], atom[i+1:]
    vars = {}

    in_string = False
    in_name = False
    escaping = False
    current_name = ''
    current = ''
    for ch in rest:
        if escaping:
            escaping = False
            current += ch
        elif ch == '\\':
            escaping = True
        elif ch == '"':
            in_string = not in_string
            current += ch
        elif in_string:
            current += ch
        elif ch == ';':
            vars[current_name.strip()] = current.strip()
            current_name = current = ''
        elif ch == '=':
            current_name = current
            current = ''
        elif ch == '}':
            vars[current_name.strip()] = current.strip()
            break
        elif ch not in ' ':
            current += ch

    return path, vars

def is_bad_atom_ordering(key, atoms):
    seen_turfs = 0
    seen_areas = 0
    can_fix = False
    for each in atoms:
        if each.startswith('/turf'):
            if seen_turfs == 1:
                print(f"Warning: key '{key}' has multiple turfs!")
            if seen_areas:
                print(f"Warning: key '{key}' has area before turf (autofixing...)")
                can_fix = True
            seen_turfs += 1
        elif each.startswith('/area'):
            if seen_areas == 1:
                print(f"Warning: key '{key}' has multiple areas!!!")
            seen_areas += 1
        else:
            if (seen_turfs or seen_areas) and not can_fix:
                print(f"Warning: key '{key}' has movable after turf or area (autofixing...)")
                can_fix = True
    if not seen_areas or not seen_turfs:
        print(f"Warning: key '{key}' is missing either a turf or area")
    return can_fix

def fix_atom_ordering(atoms):
    movables = []
    turfs = []
    areas = []
    for each in atoms:
        if each.startswith('/turf'):
            turfs.append(each)
        elif each.startswith('/area'):
            areas.append(each)
        else:
            movables.append(each)
    movables.extend(turfs)
    movables.extend(areas)
    return movables

# ----------
# TGM writer

def save_tgm(dmm, output):
    output.write(f"{TGM_HEADER}\n")
    if dmm.header:
        output.write(f"{dmm.header}\n")

    # write dictionary in tgm format
    for key, value in sorted(dmm.dictionary.items()):
        output.write(f'"{num_to_key(key, dmm.key_length)}" = (\n')
        for idx, thing in enumerate(value):
            in_quote_block = False
            in_varedit_block = False
            for char in thing:
                if in_quote_block:
                    if char == '"':
                        in_quote_block = False
                    output.write(char)
                elif char == '"':
                    in_quote_block = True
                    output.write(char)
                elif not in_varedit_block:
                    if char == "{":
                        in_varedit_block = True
                        output.write("{\n\t")
                    else:
                        output.write(char)
                elif char == ";":
                    output.write(";\n\t")
                elif char == "}":
                    output.write("\n\t}")
                    in_varedit_block = False
                else:
                    output.write(char)
            if idx < len(value) - 1:
                output.write(",\n")
        output.write(")\n")

    # thanks to YotaXP for finding out about this one
    max_x, max_y, max_z = dmm.size
    for z in range(1, max_z + 1):
        output.write("\n")
        for x in range(1, max_x + 1):
            output.write(f"({x},{1},{z}) = {{\"\n")
            for y in range(1, max_y + 1):
                output.write(f"{num_to_key(dmm.grid[x, y, z], dmm.key_length)}\n")
            output.write("\"}\n")

# ----------
# DMM writer

def save_dmm(dmm, output):
    if dmm.header:
        output.write(f"{dmm.header}\n")

    # writes a tile dictionary the same way Dreammaker does
    for key, value in sorted(dmm.dictionary.items()):
        output.write(f'"{num_to_key(key, dmm.key_length)}" = ({",".join(value)})\n')

    output.write("\n")

    # writes a map grid the same way Dreammaker does
    max_x, max_y, max_z = dmm.size
    for z in range(1, max_z + 1):
        output.write(f"(1,1,{z}) = {{\"\n")

        for y in range(1, max_y + 1):
            for x in range(1, max_x + 1):
                try:
                    output.write(num_to_key(dmm.grid[x, y, z], dmm.key_length))
                except KeyError:
                    print(f"Key error: ({x}, {y}, {z})")
            output.write("\n")
        output.write("\"}\n")

# ----------
# Parser

def _parse(map_raw_text):
    in_comment_line = False
    comment_trigger = False

    in_quote_block = False
    in_key_block = False
    in_data_block = False
    in_varedit_block = False
    after_data_block = False
    escaping = False
    skip_whitespace = False

    dictionary = bidict.bidict()
    duplicate_keys = {}
    curr_key_len = 0
    curr_key = 0
    curr_datum = ""
    curr_data = list()

    in_map_block = False
    in_coord_block = False
    in_map_string = False
    base_x = 0
    adjust_y = True

    curr_num = ""
    reading_coord = "x"

    key_length = 0

    maxx = 0
    maxy = 0
    maxz = 0

    curr_x = 0
    curr_y = 0
    curr_z = 0
    grid = dict()

    it = iter(map_raw_text)

    # map block
    for char in it:
        if char == "\n":
            in_comment_line = False
            comment_trigger = False
            continue
        elif in_comment_line:
            continue
        elif char in "\r\t":
            continue

        if char == "/" and not in_quote_block:
            if comment_trigger:
                in_comment_line = True
                continue
            else:
                comment_trigger = True
        else:
            comment_trigger = False

        if in_data_block:

            if in_varedit_block:

                if in_quote_block:
                    if char == "\\":
                        curr_datum = curr_datum + char
                        escaping = True

                    elif escaping:
                        curr_datum = curr_datum + char
                        escaping = False

                    elif char == "\"":
                        curr_datum = curr_datum + char
                        in_quote_block = False

                    else:
                        curr_datum = curr_datum + char

                else:
                    if skip_whitespace and char == " ":
                        skip_whitespace = False
                        continue
                    skip_whitespace = False

                    if char == "\"":
                        curr_datum = curr_datum + char
                        in_quote_block = True

                    elif char == ";":
                        skip_whitespace = True
                        curr_datum = curr_datum + char

                    elif char == "}":
                        curr_datum = curr_datum + char
                        in_varedit_block = False

                    else:
                        curr_datum = curr_datum + char

            elif char == "{":
                curr_datum = curr_datum + char
                in_varedit_block = True

            elif char == ",":
                curr_data.append(curr_datum)
                curr_datum = ""

            elif char == ")":
                curr_data.append(curr_datum)
                curr_data = tuple(curr_data)
                try:
                    dictionary[curr_key] = curr_data
                except bidict.ValueDuplicationError:
                    # if the map has duplicate values, eliminate them now
                    duplicate_keys[curr_key] = dictionary.inv[curr_data]
                curr_data = list()
                curr_datum = ""
                curr_key = 0
                curr_key_len = 0
                in_data_block = False
                after_data_block = True

            else:
                curr_datum = curr_datum + char

        elif in_key_block:
            if char == "\"":
                in_key_block = False
                if key_length == 0:
                    key_length = curr_key_len
                else:
                    assert key_length == curr_key_len
            else:
                curr_key = BASE * curr_key + base52_r[char]
                curr_key_len += 1

        # else we're looking for a key block, a data block or the map block
        elif char == "\"":
            in_key_block = True
            after_data_block = False

        elif char == "(":
            if after_data_block:
                in_coord_block = True
                after_data_block = False
                curr_key = 0
                curr_key_len = 0
                break
            else:
                in_data_block = True
                after_data_block = False

    # grid block
    for char in it:
        if char == "\r":
            continue

        if in_coord_block:
            if char == ",":
                if reading_coord == "x":
                    curr_x = int(curr_num)
                    if curr_x > maxx:
                        maxx = curr_x
                    base_x = curr_x
                    curr_num = ""
                    reading_coord = "y"
                elif reading_coord == "y":
                    curr_y = int(curr_num)
                    if curr_y > maxy:
                        maxy = curr_y
                    curr_num = ""
                    reading_coord = "z"
                else:
                    raise ValueError("too many dimensions")

            elif char == ")":
                curr_z = int(curr_num)
                if curr_z > maxz:
                    maxz = curr_z
                in_coord_block = False
                reading_coord = "x"
                curr_num = ""

            else:
                curr_num = curr_num + char

        elif in_map_string:
            if char == "\"":
                in_map_string = False
                adjust_y = True
                curr_y -= 1

            elif char == "\n":
                if adjust_y:
                    adjust_y = False
                else:
                    curr_y += 1
                curr_x = base_x
            else:
                curr_key = BASE * curr_key + base52_r[char]
                curr_key_len += 1
                if curr_key_len == key_length:
                    grid[curr_x, curr_y, curr_z] = duplicate_keys.get(curr_key, curr_key)
                    if curr_x > maxx:
                        maxx = curr_x
                    curr_x += 1
                    curr_key = 0
                    curr_key_len = 0

        # else look for coordinate block or a map string
        elif char == "(":
            in_coord_block = True
        elif char == "\"":
            in_map_string = True

    if curr_y > maxy:
        maxy = curr_y

    data = DMM(key_length, Coordinate(maxx, maxy, maxz))
    data.dictionary = dictionary
    data.grid = grid
    return data
