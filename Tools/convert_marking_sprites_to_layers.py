import os
import shutil
import sys
import yaml

"""
    What's all this then...???

    This script converts the old format of marking prototypes, in which layers are specified under the "sprites"
    datafield and per-layer coloring is specified in the "coloring" datafield, to the new format.

    In the new format, "layers" is a field containing a list of layer metadata. This metadata specifies sprite
    and coloring; it also supports individual locale IDs. This makes it easier to add per-layer properties to
    markings, which is useful for downstreams.

    Converted marking files will replace their original file path.
    The old version of the file is saved to a `.bak` file in the same directory.
"""

VALID_EXTENSION = ".yml"
USAGE_HINT = f"Usage: py convert_marking_sprites_to_layers.py <path{VALID_EXTENSION}>"
TAG_PREFIX = "!type:"
prototypes_changed = 0

class ClassRepresentation:
    """
        Representation of a C# class, such as marking coloring data.

        Attributes:
            prefix (str): The class tag itself ("!type:SimpleColoring")
            data (Any): The data contained in this tag. ({ "color" : "#FF0000" })
    """
    def __init__(self, prefix, data):
        self.prefix = prefix
        self.data = data

class InlineListRepresentation(list):
    """
        Marks a list as needing to be represented inline.
    """
    pass

class PrototypeDumper(yaml.SafeDumper):
    """
        Custom dumper for special YML formatting.
    """
    def write_line_break(self, data = None):
        super().write_line_break(data)

        # Add an extra line break between top-level list items (prototypes).
        if (len(self.indents) == 1):
            super().write_line_break()

def class_tag_constructor(loader: yaml.Loader, tag_suffix: str, node: yaml.Node):
    """
        Parses a C# class data tag as a mapping.
    """
    if (isinstance(node, yaml.MappingNode)):
        value = loader.construct_mapping(node, deep=True)
    if (isinstance(node, yaml.SequenceNode)):
        value = loader.construct_sequence(node, deep=True)
    if (isinstance(node, yaml.ScalarNode)):
        value = loader.construct_scalar(node)

    if value is not None:
        tag = ClassRepresentation(f"{TAG_PREFIX}{tag_suffix}", value)
        return tag

    # If we continue we might break something. Uuuhh
    print(f"Tag declaration was not accounted for in constructor: {node}")
    sys.exit(1)

def class_tag_representer(dumper: yaml.Dumper, data: ClassRepresentation):
    """
        Represent class tags as mappings.
    """
    return dumper.represent_mapping(data.prefix, data.data)

def inline_list_representer(dumper: yaml.Dumper, data: InlineListRepresentation):
    """
        Represent inline lists with flow style.
    """
    return dumper.represent_sequence("tag:yaml.org,2002:seq", data, flow_style=True)

def misc_conversion_for_my_convenience(marking: dict):
    """
        Conversions that exist purely to reduce tedium on my part. Smiles.

        Parameters:
            marking (dict): A marking prototype that has been converted to the new format.
    """

    layers: list = marking.get("layers")
    layer_count = len(layers)
    if not layers or layer_count <= 0:
        return

    # Make the first layer of  all hair markings use a "hair" locale ID
    body_part = marking.get("bodyPart")
    if body_part == "Hair":
        layers[0]["name"] = "marking-layer-hair"

    # Make the first layer of all facial hair markings use a "facial hair" locale ID
    if body_part == "FacialHair":
        layers[0]["name"] = "marking-layer-facial-hair"

def convert_to_inline_list(marking: dict, field: str):
    """
        Convert a data field to use inline list representation, assuming it is a list.

        Parameters:
            marking (dict): The marking prototype.
            field (str): The name of the field to convert.
    """

    datafield = marking.get(field)
    if not datafield or not isinstance(datafield, list):
        return

    marking[field] = InlineListRepresentation(datafield)

def convert_prototype(proto: dict) -> dict:
    """
        Convert an individual prototype into the new format, if it is a marking.

        This is done by moving sprites and per-layer coloring settings to layer metadata
        objects in the "layers" field.

        Parameters:
            proto (dict): An individual prototype. Not necessarily a marking.

        Returns:
            dict: The final prototype data.
    """
    global prototypes_changed

    if ("type" not in proto or proto["type"] != "marking" # Not a marking prototype
        or "sprites" not in proto # Lacks sprites
        or "layers" in proto): # Already has layers
        print(f"Skipping over prototype: {proto.get("id")}")
        return proto

    new_marking: dict = proto.copy()
    sprites: list = new_marking.pop("sprites")
    layers: list  = []
    layer_coloring: dict = {}

    # Convert certain data fields into inline lists.
    convert_to_inline_list(new_marking, "groupWhitelist")
    convert_to_inline_list(new_marking, "sexRestriction")

    # Get per-layer coloring if it exists
    coloring: dict = new_marking.get("coloring")
    if coloring and "layers" in coloring:
        layer_coloring = new_marking["coloring"].pop("layers")

    # Convert all sprites to layer metadata
    for sprite in sprites:
        layer: dict = { "sprite": sprite }

        # Convert layer coloring, if it exists
        state: str = sprite.get("state")
        if state and state in layer_coloring:
            coloring = layer_coloring.pop(state)
            layer["coloring"] = coloring

        # Add this layer to our new layer list
        layers.append(layer)

    # Add "layers" field to prototype
    new_marking["layers"] = layers
    misc_conversion_for_my_convenience(new_marking)

    prototypes_changed += 1
    return new_marking

def add_yaml_representers():
    """
        Initialize constructors and representers to format certain data types a certain way -
        such as in-line lists and C# class tags (!type:).
    """
    yaml.add_multi_constructor(TAG_PREFIX, class_tag_constructor, Loader=yaml.SafeLoader)
    yaml.add_representer(ClassRepresentation, class_tag_representer, Dumper=PrototypeDumper)
    yaml.add_representer(InlineListRepresentation, inline_list_representer, Dumper=PrototypeDumper)

def convert_file(input_file: str):
    """
        Open an input YAML file and convert all markings inside it.

        Parameters:
            input_file (str): The YAML prototype file to convert.
    """
    add_yaml_representers()

    file_path, ext = os.path.splitext(input_file)
    backup_path: str = f"{file_path}{ext}.bak"

    if (ext != VALID_EXTENSION):
        print(f"ERROR: Prototype file is not a {VALID_EXTENSION} file! Path: {input_file}")
        sys.exit(1)

    with (open(input_file, 'r') as f):
        prototypes = yaml.safe_load(f)

    # YML files must be lists of prototype objects
    if not isinstance(prototypes, list):
        print(f"ERROR: File {input_file} is not a valid YAML prototype file!")
        sys.exit(1)

    # Convert each applicable prototype to use the new marking system.
    converted_prototypes: list = [convert_prototype(proto) for proto in prototypes]
    if (prototypes_changed == 0):
        print(f"ERROR: No valid prototypes to convert in {input_file}.")
        sys.exit(1)

    # Copy the old prototype file to a backup
    shutil.copy(input_file, backup_path)

    # Replace the old prototype file.
    with (open(input_file, 'w') as f):
        yaml.dump(converted_prototypes, f,
            Dumper=PrototypeDumper,
            default_flow_style=False,
            allow_unicode=True,
            sort_keys=False)
        print(f"Successfully converted {input_file}. Changed prototypes: {prototypes_changed}")

def main():
    """
        Parse a file name from command arguments and convert all prototypes in that file.
    """

    if (len(sys.argv) < 1):
        print(USAGE_HINT)
        sys.exit(1)

    input_file: str = sys.argv[1]
    if not os.path.exists(input_file):
        print(f"File {input_file} not found.")
        print(USAGE_HINT)
        sys.exit(1)

    convert_file(input_file)

# Go go gadget marking conversion
main()
