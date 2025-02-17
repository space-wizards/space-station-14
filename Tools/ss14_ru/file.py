import typing

from fluent.syntax import ast
from yamlmodels import YAMLElements
import os
import re


class File:
    def __init__(self, full_path):
        self.full_path = full_path

    def read_data(self):
        file = open(self.full_path, 'r', encoding='utf8')
        # replace необходим для того, чтобы 1-е сообщение не считалось ast.Junk
        file_data = file.read().replace('﻿', '')
        file.close()

        return file_data

    def save_data(self, file_data: typing.AnyStr):
        os.makedirs(os.path.dirname(self.full_path), exist_ok=True)
        file = open(self.full_path, 'w', encoding='utf8')
        file.write(file_data)
        file.close()

    def get_relative_path(self, base_path):
        return os.path.relpath(self.full_path, base_path)

    def get_relative_path_without_extension(self, base_path):
        return self.get_relative_path(base_path).split('.', maxsplit=1)[0]

    def get_relative_parent_dir(self, base_path):
        return os.path.relpath(self.get_parent_dir(), base_path)

    def get_parent_dir(self):
        return os.path.dirname(self.full_path)

    def get_name(self):
        return os.path.basename(self.full_path).split('.')[0]


class FluentFile(File):
    def __init__(self, full_path):
        super().__init__(full_path)

        self.newline_exceptions_regex = re.compile(r"^\s*[\[\]{}#%^*]")
        self.newline_remover_tag = "%ERASE_NEWLINE%"
        self.newline_remover_regex = re.compile(r"\n?\s*" + self.newline_remover_tag)

        "%ERASE_NEWLINE%"

        self.full_path = full_path

    def kludge(self, element):
        return str.replace(
            element.value,
            self.prefixed_newline,
            self.prefixed_newline_substitute
        )


    def parse_data(self, file_data: typing.AnyStr):
        from fluent.syntax import FluentParser

        parsed_data = FluentParser().parse(file_data)

        for body_element in parsed_data.body:
            if not isinstance(body_element, ast.Term) and not isinstance(body_element, ast.Message):
                continue

            if not len(body_element.value.elements):
                continue

            first_element = body_element.value.elements[0]
            if not isinstance(first_element, ast.TextElement):
                continue

            if not self.newline_exceptions_regex.match(first_element.value):
                continue

            first_element.value = f"{self.newline_remover_tag}{first_element.value}"

        return parsed_data

    def serialize_data(self, parsed_file_data: ast.Resource):
        from fluent.syntax import FluentSerializer

        serialized_data = FluentSerializer(with_junk=True).serialize(parsed_file_data)
        serialized_data = self.newline_remover_regex.sub(' ', serialized_data)

        return serialized_data

    def read_serialized_data(self):
        return self.serialize_data(self.parse_data(self.read_data()))

    def read_parsed_data(self):
        return self.parse_data(self.read_data())


class YAMLFluentFileAdapter(File):
    def __init__(self, full_path):
        super().__init__(full_path)
        self.full_path = full_path

    # def create_fluent_from_yaml_elements(self, yaml_elements):



class YAMLFile(File):
    def __init__(self, full_path):
        super().__init__(full_path)

    def parse_data(self, file_data: typing.AnyStr):
        import yaml

        return yaml.load(file_data, Loader=yaml.BaseLoader)

    def get_elements(self, parsed_data):

        if isinstance(parsed_data, list):
            elements = YAMLElements(parsed_data).elements
            # элемент может быть None, если имеет неизвестный тип
            exist_elements = list(filter(lambda el: el, elements))

            return exist_elements

        return []
