import os
import re
import shutil
import sys
import yaml
import yaml_utils

"""
    This script converts all microwave meal recipes in a file to use a locale ID in the "name" field.
    Note that this script makes the kind of hefty assumption that all names in the file use the same
    name scheme as Wizard's Den, where the name matches the item and maybe ends in "recipe". For example,
    "crazy hamburger recipe" -> "microwave-meal-recipe-crazy-hamburger-name = crazy hamburger recipe"

    This will strip all comments from your prototypes - you have been warned!
"""

VALID_EXTENSION = yaml_utils.YML_EXTENSION
USAGE_HINT = f"Usage: py localize_microwave_recipes.py <path{VALID_EXTENSION}>"
prototypes_changed = 0

def convert_to_locale_id(value: str) -> str:
    """
        Converts a recipe name to a locale ID, which is written in kebab-case.
    """
    if (value.startswith("microwave-meal-") # probably already pre-converted
        or (value.count(" ") == 0 and value == value.lower())): # value seems LocID-like, ish
        return value

    value = value.lower() # lowercase
    value = value.replace("recipe", "") # remove redundant word "recipe", which many recipe names have
    value = re.sub(r'[^\s\w\-+]', "", value) # alphanumeric, plus dashes and spaces
    value = re.sub(r'[_\s]+', "-", value) # replace underscores and spaces with dashes
    value = re.sub(r'-+', "-", value) # remove double dashes
    value = value.strip("-") # remove trailing and leading spaces
    return f"microwave-meal-recipe-{value}-name" # format

def localize_prototype(proto: dict, locales: dict) -> dict:
    """
        Generates a locale ID for a recipe prototype with an unlocalized name value.
    """
    global prototypes_changed

    if ("type" not in proto # not a prototype
        or proto["type"] != "microwaveMealRecipe" # not a recipe
        or "name" not in proto): # lacks a name field
        print(f"Skipping over prototype: {proto.get("id")}")
        return proto

    new_proto = proto.copy()

    # localize this name
    nameValue = new_proto.get("name")
    nameId = convert_to_locale_id(nameValue)
    if (nameId == nameValue):
        return new_proto

    locales[nameId] = nameValue

    # convert prototype to use new locale
    new_proto["name"] = nameId
    prototypes_changed += 1
    return new_proto

def write_locales_to_file(path: str, locales: dict):
    """
        Convert a dictionary into a localization file.
    """
    lines = [
        "# converted with Tools/localize_microwave_recipes.py",
        ""
    ]

    # Convert dict values to locales
    for id, value in locales.items():
        if value is None or value == "":
            continue
        value = value.replace('"', '\\"') # Escape quotes
        lines.append(f"{id} = {value}")
    content = "\n".join(lines)

    # Write locales to file
    with (open(path, 'w', encoding="utf-8") as f):
        f.write(content)
        print(f"Successfully wrote {len(lines) - 2} locales to {path}.")

def convert_file(input_path: str):
    """
        Open an input YAML file and localize all microwave recipes in it.

        Parameters:
            input_file (str): The YAML prototype file to localize.
    """
    with (open(input_path, 'r') as f):
        prototypes = yaml.safe_load(f)

    # YML files must be lists of prototype objects
    if not isinstance(prototypes, list):
        raise TypeError(f"File {input_path} is not a valid YAML prototype file!")

    # Generate locale IDs for all prototypes
    locales = {}
    converted_prototypes: list = [localize_prototype(proto, locales) for proto in prototypes]
    if (prototypes_changed == 0):
        raise ValueError(f"No valid prototypes to convert in {input_path}.")

    # Write new YML and locale files
    write_locales_to_file("microwave-meal-recipe-prototype.ftl", locales)
    yaml_utils.write_yaml_to_file(input_path, converted_prototypes)
    print(f"Successfully converted {input_path}. Changed prototypes: {prototypes_changed}")

def main():
    """
        Parse a file name from command arguments and localize all prototypes in that file.
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

# Go go gadget recipe localization
main()
