# Common code for the frontend interface of map tools
import sys
import os
import pathlib
import shutil
from collections import namedtuple

Settings = namedtuple('Settings', ['map_folder', 'tgm'])
MapsToRun = namedtuple('MapsToRun', ['files', 'indices'])

def string_to_num(s):
    try:
        return int(s)
    except ValueError:
        return -1

def read_settings():
    # discover map folder if needed
    try:
        map_folder = os.environ['MAPROOT']
    except KeyError:
        map_folder = '_maps/'
        for _ in range(8):
            if os.path.exists(map_folder):
                break
            map_folder = os.path.join('..', map_folder)
        else:
            map_folder = None

    # assume TGM is True by default
    tgm = os.environ.get('TGM', "1") == "1"

    return Settings(map_folder, tgm)

def pretty_path(settings, path_str):
    if settings.map_folder:
        return path_str[len(os.path.commonpath([settings.map_folder, path_str]))+1:]
    else:
        return path_str

def prompt_maps(settings, verb):
    if not settings.map_folder:
        print("Could not autodetect the _maps folder, set MAPROOT")
        exit(1)

    list_of_files = list()
    for root, directories, filenames in os.walk(settings.map_folder):
        for filename in [f for f in filenames if f.endswith(".dmm")]:
            list_of_files.append(pathlib.Path(root, filename))

    last_dir = ""
    for i, this_file in enumerate(list_of_files):
        this_dir = this_file.parent
        if last_dir != this_dir:
            print("--------------------------------")
            last_dir = this_dir
        print("[{}]: {}".format(i, pretty_path(settings, str(this_file))))

    print("--------------------------------")
    in_list = input("List the maps you want to " + verb + " (example: 1,3-5,12):\n")
    in_list = in_list.replace(" ", "")
    in_list = in_list.split(",")

    valid_indices = list()
    for m in in_list:
        index_range = m.split("-")
        if len(index_range) == 1:
            index = string_to_num(index_range[0])
            if index >= 0 and index < len(list_of_files):
                valid_indices.append(index)
        elif len(index_range) == 2:
            index0 = string_to_num(index_range[0])
            index1 = string_to_num(index_range[1])
            if index0 >= 0 and index0 <= index1 and index1 < len(list_of_files):
                valid_indices.extend(range(index0, index1 + 1))

    return MapsToRun(list_of_files, valid_indices)

def process(settings, verb, *, modify=True, backup=None):
    if backup is None:
        backup = modify  # by default, backup when we modify
    assert modify or not backup  # doesn't make sense to backup when not modifying

    if len(sys.argv) > 1:
        maps = sys.argv[1:]
    else:
        maps = prompt_maps(settings, verb)
        maps = [str(maps.files[i]) for i in maps.indices]
        print()

    if not maps:
        print("No maps selected.")
        return

    if modify:
        print(f"Maps WILL{'' if settings.tgm else ' NOT'} be converted to tgm.")
        if backup:
            print("Backups will be created with a \".before\" extension.")
        else:
            print("Warning: backups are NOT being taken.")

    print(f"\nWill {verb} these maps:")
    for path_str in maps:
        print(pretty_path(settings, path_str))

    try:
        confirm = input(f"\nPress Enter to {verb}...\n")
    except KeyboardInterrupt:
        confirm = "^C"
    if confirm != "":
        print(f"\nAborted.")
        return

    for path_str in maps:
        print(f' - {pretty_path(settings, path_str)}')

        if backup:
            shutil.copyfile(path_str, path_str + ".before")

        try:
            yield path_str
        except Exception as e:
            print(f"Error: {e}")
        else:
            print("Succeeded.")

    print("\nFinished.")
