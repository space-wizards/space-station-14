import yaml
import re
import sys

def sort_tags(yaml_file):
    with open(yaml_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    tag_blocks = []
    current_block = []
    inside_block = False
    
    for line in lines:
        if re.match(r'\s*-\s*type:\s*Tag', line):
            if current_block:
                tag_blocks.append(current_block)
            current_block = [line]
            inside_block = True
        elif inside_block and (line.strip() == '' or line.startswith('#')):
            inside_block = False
            tag_blocks.append(current_block)
            current_block = []
        elif inside_block:
            current_block.append(line)
    
    if current_block:
        tag_blocks.append(current_block)
    
    def extract_id(block):
        for line in block:
            match = re.match(r'\s*id:\s*(\S+)', line)
            if match:
                return match.group(1)
        return ''
    
    tag_blocks.sort(key=extract_id)
    
    sorted_lines = []
    tag_indices = iter(tag_blocks)
    
    inside_tag_section = False
    
    for line in lines:
        if re.match(r'\s*-\s*type:\s*Tag', line):
            if not inside_tag_section:
                sorted_lines.extend(next(tag_indices))
                inside_tag_section = True
        elif inside_tag_section and (line.strip() == '' or line.startswith('#')):
            sorted_lines.append(line)
            inside_tag_section = False
        elif inside_tag_section:
            continue
        else:
            sorted_lines.append(line)
    
    with open(yaml_file, 'w', encoding='utf-8') as f:
        f.writelines(sorted_lines)

def main():
    if len(sys.argv) != 2:
        print("Drag and drop a YAML file onto this script to alphabetize tags in it.")
        input("Press any key to continue...")
        sys.exit(1)
    file_path = sys.argv[1]

    sort_tags(file_path)
    
    input("Press any key to continue...")

if __name__ == "__main__":
   main()