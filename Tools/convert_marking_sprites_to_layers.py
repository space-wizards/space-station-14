import os
import shutil
import sys
import yaml
import yaml_utils

"""
    What's all this then...???

    This script converts per-layer shaders and marking coloring to be in the "sprites" datafield
    It used to do more than this, but then I made markings more backward-compatible in YML!

    Converted marking files will replace their original file path.
    The old version of the file is saved to a `.bak` file in the same directory.

    Arguments:
        <path>: Input .yml file
        --add-hair-names: Optional flag, adds "marking-layer-hair" and "marking-layer-facial-hair" locale names
            to hair and facial hair, respectively.

    Example usage:
        py convert_marking_sprites_to_layers.py Resources/Prototypes/Entities/Mobs/Customization/Markings/ears.yml

    (it's a mouthful. sorry)
"""

VALID_EXTENSION = ".yml"
USAGE_HINT = f"Usage: py convert_marking_sprites_to_layers.py <path{VALID_EXTENSION}> [--add-hair-names]"
TAG_PREFIX = "!type:"
prototypes_changed = 0

def misc_conversion_for_my_convenience(marking: dict, flags: list):
    """
        Conversions that exist purely to reduce tedium on my part. Smiles.

        Parameters:
            marking (dict): A marking prototype that has been converted to the new format.
    """

    sprites: list = marking.get("sprites")
    sprite_count = len(sprites)
    if not sprites or sprite_count <= 0:
        return

    if "--add-hair-names" in flags:
        # Make the first layer of  all hair markings use a "hair" locale ID
        body_part = marking.get("bodyPart")
        if body_part == "Hair":
            sprites[0]["name"] = "marking-layer-hair"

        # Make the first layer of all facial hair markings use a "facial hair" locale ID
        if body_part == "FacialHair":
            sprites[0]["name"] = "marking-layer-facial-hair"

def convert_prototype(proto: dict, flags: list) -> dict:
    """
        Convert an individual prototype into the new format, if it is a marking.

        This is done by moving per-layer coloring settings to layer metadata
        objects in the "sprites" field.

        Parameters:
            proto (dict): An individual prototype. Not necessarily a marking.

        Returns:
            dict: The final prototype data.
    """
    global prototypes_changed

    if ("type" not in proto or proto["type"] != "marking" # Not a marking prototype
        or "sprites" not in proto): # Lacks sprites
        print(f"Skipping over prototype: {proto.get("id")}")
        return proto

    new_marking: dict = proto.copy()
    sprites: list = new_marking.get("sprites")

    # Convert certain data fields into inline lists for consistency.
    yaml_utils.convert_field_to_inline_list(new_marking, "groupWhitelist")
    yaml_utils.convert_field_to_inline_list(new_marking, "sexRestriction")

    # Get per-layer coloring if it exists
    layer_coloring: dict = {}
    coloring: dict = new_marking.get("coloring")
    if coloring:
        if "layers" in coloring:
            layer_coloring = new_marking["coloring"].pop("layers")
        if len(coloring) == 0: # Clear if empty
            new_marking.pop("coloring")

    # Get per-layer shaders if it exists
    shaders: dict = new_marking.get("shaders")

    for sprite in sprites:
        state: str = sprite.get("state")
        if state:
            if (state in layer_coloring): # Convert layer coloring
                sprite["coloring"] = layer_coloring.pop(state)
            if (shaders and state in shaders):  # Convert shaders
                sprite["shaders"] = shaders.pop(state);

    if (shaders and len(shaders) == 0):
        new_marking.pop("shaders") # Clear if empty


    # Convert other shit
    misc_conversion_for_my_convenience(new_marking, flags)

    prototypes_changed += 1
    return new_marking

def convert_file(input_file: str, flags: list):
    """
        Open an input YAML file and convert all markings inside it.

        Parameters:
            input_file (str): The YAML prototype file to convert.
    """
    yaml_utils.add_yaml_representers()

    file_path, ext = os.path.splitext(input_file)
    backup_path: str = f"{file_path}{ext}.bak"

    if (ext != VALID_EXTENSION):
        raise ValueError(f"ERROR: Prototype file is not a {VALID_EXTENSION} file! Path: {input_file}")

    with (open(input_file, 'r') as f):
        prototypes = yaml.safe_load(f)

    # YML files must be lists of prototype objects
    if not isinstance(prototypes, list):
        raise ValueError(f"ERROR: File {input_file} is not a valid YAML prototype file!")

    # Convert each applicable prototype to use the new marking system.
    converted_prototypes: list = [convert_prototype(proto, flags) for proto in prototypes]
    if (prototypes_changed == 0):
        raise ValueError(f"ERROR: No valid prototypes to convert in {input_file}.")

    # Copy the old prototype file to a backup
    shutil.copy(input_file, backup_path)

    # Replace the old prototype file.
    yaml_utils.write_yaml_to_file(input_file, converted_prototypes)
    print(f"Successfully converted {input_file}. Changed prototypes: {prototypes_changed}")

def main():
    """
        Parse a file name from command arguments and convert all prototypes in that file.
    """

    if (len(sys.argv) < 1):
        raise ValueError(USAGE_HINT)

    input_file: str = sys.argv[1]
    flags: list = sys.argv[2:]

    if not os.path.exists(input_file):
        raise FileNotFoundError(f"File {input_file} not found.")

    convert_file(input_file, flags)

# Go go gadget marking conversion
main()
