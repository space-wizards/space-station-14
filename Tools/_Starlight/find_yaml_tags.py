import os
import re
import json

DIRECTORY = "../../Resources/Prototypes/"

tag_pattern = re.compile(r"!type:([A-Za-z0-9_]+)")

unique_tags = set()

print("Process started, it may take some time.")

for root, _, files in os.walk(DIRECTORY):
    for file in files:
        if file.endswith(".yml"):
            file_path = os.path.join(root, file)
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
                matches = tag_pattern.findall(content)
                for tag in matches:
                    unique_tags.add(f"!type:{tag} mapping")

unique_tags = sorted(unique_tags)

output = {
    "yaml.customTags": list(unique_tags)
}

with open("custom_tags.json", "w", encoding="utf-8") as f:
    json.dump(output, f, indent=4, ensure_ascii=False)

print("Done, result will be saved in 'custom_tags.json'")
input("Press any key to continue...")
