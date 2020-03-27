# A script and syntax for applying path updates to maps.
import re
import os
import argparse
import frontend
from dmm import *

desc = """
Update dmm files given update file/string.
Replacement syntax example:
    /turf/open/floor/plasteel/warningline : /obj/effect/turf_decal {dir = @OLD ;tag = @SKIP;icon_state = @SKIP}
    /turf/open/floor/plasteel/warningline : /obj/effect/turf_decal {@OLD} , /obj/thing {icon_state = @OLD:name; name = "meme"}
    /turf/open/floor/plasteel/warningline{dir=2} : /obj/thing
New paths properties:
    @OLD - if used as property name copies all modified properties from original path to this one
    property = @SKIP - will not copy this property through when global @OLD is used.
    property = @OLD - will copy this modified property from original object even if global @OLD is not used
    property = @OLD:name - will copy [name] property from original object even if global @OLD is not used
    Anything else is copied as written.
Old paths properties:
    Will be used as a filter.
    property = @UNSET - will apply the rule only if the property is not mapedited
"""

default_map_directory = "../../_maps"
replacement_re = re.compile(r'\s*(?P<path>[^{]*)\s*(\{(?P<props>.*)\})?')

#urgent todo: replace with actual parser, this is slow as janitor in crit
split_re = re.compile(r'((?:[A-Za-z0-9_\-$]+)\s*=\s*(?:"(?:.+?)"|[^";]*)|@OLD)')


def props_to_string(props):
    return "{{{}}}".format(";".join([f"{k} = {v}" for k, v in props.items()]))


def string_to_props(propstring, verbose = False):
    props = dict()
    for raw_prop in re.split(split_re, propstring):
        if not raw_prop or raw_prop.strip() == ';':
            continue
        prop = raw_prop.split('=', maxsplit=1)
        props[prop[0].strip()] = prop[1].strip() if len(prop) > 1 else None
    if verbose:
        print("{0} to {1}".format(propstring, props))
    return props


def parse_rep_string(replacement_string, verbose = False):
    # translates /blah/blah {meme = "test",} into path,prop dictionary tuple
    match = re.match(replacement_re, replacement_string)
    path = match['path']
    props = match['props']
    if props:
        prop_dict = string_to_props(props, verbose)
    else:
        prop_dict = dict()
    return path.strip(), prop_dict


def update_path(dmm_data, replacement_string, verbose=False):
    old_path_part, new_path_part = replacement_string.split(':', maxsplit=1)
    old_path, old_path_props = parse_rep_string(old_path_part, verbose)
    new_paths = list()
    for replacement_def in new_path_part.split(','):
        new_path, new_path_props = parse_rep_string(replacement_def, verbose)
        new_paths.append((new_path, new_path_props))

    subtypes = ""
    if old_path.endswith("/@SUBTYPES"):
        old_path = old_path[:-len("/@SUBTYPES")]
        if verbose:
            print("Looking for subtypes of", old_path)
        subtypes = r"(?:/\w+)*"

    replacement_pattern = re.compile(rf"(?P<path>{re.escape(old_path)}{subtypes})\s*(:?{{(?P<props>.*)}})?$")

    def replace_def(match):
        if match['props']:
            old_props = string_to_props(match['props'], verbose)
        else:
            old_props = dict()
        for filter_prop in old_path_props:
            if filter_prop not in old_props:
                if old_path_props[filter_prop] == "@UNSET":
                    continue
                else:
                    return [match.group(0)]
            else:
                if old_props[filter_prop] != old_path_props[filter_prop] or old_path_props[filter_prop] == "@UNSET":
                    return [match.group(0)] #does not match current filter, skip the change.
        if verbose:
            print("Found match : {0}".format(match.group(0)))
        out_paths = []
        for new_path, new_props in new_paths:
            if new_path == "@OLD":
                out = match.group('path')
            else:
                out = new_path
            out_props = dict()
            for prop_name, prop_value in new_props.items():
                if prop_name == "@OLD":
                    out_props = dict(old_props)
                    continue
                if prop_value == "@SKIP":
                    out_props.pop(prop_name, None)
                    continue
                if prop_value.startswith("@OLD"):
                    params = prop_value.split(":")
                    if prop_name in old_props:
                        out_props[prop_name] = old_props[params[1]] if len(params) > 1 else old_props[prop_name]
                    continue
                out_props[prop_name] = prop_value
            if out_props:
                out += props_to_string(out_props)
            out_paths.append(out)
        if verbose:
            print("Replacing with: {0}".format(out_paths))
        return out_paths

    def get_result(element):
        match = replacement_pattern.match(element)
        if match:
            return replace_def(match)
        else:
            return [element]

    bad_keys = {}
    keys = list(dmm_data.dictionary.keys())
    for definition_key in keys:
        def_value = dmm_data.dictionary[definition_key]
        new_value = tuple(y for x in def_value for y in get_result(x))
        if new_value != def_value:
            dmm_data.overwrite_key(definition_key, new_value, bad_keys)
    dmm_data.reassign_bad_keys(bad_keys)


def update_map(map_filepath, updates, verbose=False):
    print("Updating: {0}".format(map_filepath))
    dmm_data = DMM.from_file(map_filepath)
    for update_string in updates:
        update_path(dmm_data, update_string, verbose)
    dmm_data.to_file(map_filepath, True)


def update_all_maps(map_directory, updates, verbose=False):
    for root, _, files in os.walk(map_directory):
        for filepath in files:
            if filepath.endswith(".dmm"):
                path = os.path.join(root, filepath)
                update_map(path, updates, verbose)


def main(args):
    if args.inline:
        print("Using replacement:", args.update_source)
        updates = [args.update_source]
    else:
        with open(args.update_source) as f:
            updates = [line for line in f if line and not line.startswith("#") and not line.isspace()]
        print(f"Using {len(updates)} replacements from file:", args.update_source)

    if args.map:
        update_map(args.map, updates, verbose=args.verbose)
    else:
        map_directory = args.directory or frontend.read_settings().map_folder
        update_all_maps(map_directory, updates, verbose=args.verbose)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=desc, formatter_class=argparse.RawTextHelpFormatter)
    parser.add_argument("update_source", help="update file path / line of update notation")
    parser.add_argument("--map", "-m", help="path to update, defaults to all maps in maps directory")
    parser.add_argument("--directory", "-d", help="path to maps directory, defaults to _maps/")
    parser.add_argument("--inline", "-i", help="treat update source as update string instead of path", action="store_true")
    parser.add_argument("--verbose", "-v", help="toggle detailed update information", action="store_true")
    main(parser.parse_args())
