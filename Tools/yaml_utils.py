import os
import shutil
import sys
import yaml

YML_EXTENSION = ".yml"
TAG_PREFIX = "!type:"

"""
    Utility classes and functions that are useful for writing python scripts that
    interact with Robust YAML.
"""

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

# region utility functions
# yup this is the stuff you want to use

def convert_field_to_inline_list(data: dict, field: str):
    """
        Convert a data field to use inline list representation, assuming it is a list.

        Parameters:
            marking (dict): The marking prototype.
            field (str): The name of the field to convert.
    """

    datafield = data.get(field)
    if not datafield or not isinstance(datafield, list):
        return

    data[field] = InlineListRepresentation(datafield)

def add_yaml_representers():
    """
        Initialize constructors and representers to format certain data types a certain way -
        such as in-line lists and C# class tags (!type:).
    """
    yaml.add_multi_constructor(TAG_PREFIX, class_tag_constructor, Loader=yaml.SafeLoader)
    yaml.add_representer(ClassRepresentation, class_tag_representer, Dumper=PrototypeDumper)
    yaml.add_representer(InlineListRepresentation, inline_list_representer, Dumper=PrototypeDumper)

def write_yaml_to_file(input_path: str, data: any):
    file_path, ext = os.path.splitext(input_path)
    backup_path: str = f"{file_path}{ext}.bak"

    if (ext != YML_EXTENSION):
        raise ValueError(f"ERROR: Input path is not a {YML_EXTENSION} file! Path: {input_path}")

    # Back up original file
    shutil.copy(input_path, backup_path)

    # Dump data to this path
    with (open(input_path, 'w') as f):
        yaml.dump(data, f,
            Dumper=PrototypeDumper,
            default_flow_style=False,
            allow_unicode=True,
            sort_keys=False)

# endregion utility functions
