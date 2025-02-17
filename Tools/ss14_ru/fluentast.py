import typing

from fluent.syntax import ast, FluentParser, FluentSerializer
from lokalisemodels import LokaliseKey
from pydash import py_


class FluentAstAbstract:
    element = None
    @classmethod
    def get_id_name(cls, element):
        if isinstance(element, ast.Junk):
            return FluentAstJunk(element).get_id_name()
        elif isinstance(element, ast.Message):
            return FluentAstMessage(element).get_id_name()
        elif isinstance(element, ast.Term):
            return FluentAstTerm(element).get_id_name()
        else:
            return None

    @classmethod
    def create_element(cls, element):
        if isinstance(element, ast.Junk):
            cls.element = FluentAstJunk(element)
            return cls.element
        elif isinstance(element, ast.Message):
            cls.element = FluentAstMessage(element)
            return cls.element
        elif isinstance(element, ast.Term):
            cls.element = FluentAstTerm(element)
            return cls.element
        else:
            return None


class FluentAstMessage:
    def __init__(self, message: ast.Message):
        self.message = message
        self.element = message

    def get_id_name(self):
        return self.message.id.name


class FluentAstTerm:
    def __init__(self, term: ast.Term):
        self.term = term
        self.element = term

    def get_id_name(self):
        return self.term.id.name


class FluentAstAttribute:
    def __init__(self, id, value, parent_key = None):
        self.id = id
        self.value = value
        self.parent_key = parent_key


class FluentAstAttributeFactory:
    @classmethod
    def from_yaml_element(cls, element):
        attrs = []
        if element.description:
            attrs.append(FluentAstAttribute('desc', element.description))

        if element.suffix:
            attrs.append(FluentAstAttribute('suffix', element.suffix))

        if not len(attrs):
            return None

        return attrs


class FluentAstJunk:
    def __init__(self, junk: ast.Junk):
        self.junk = junk
        self.element = junk

    def get_id_name(self):
        return self.junk.content.split('=')[0].strip()


class FluentSerializedMessage:
    @classmethod
    def from_yaml_element(cls, id, value, attributes, parent_id = None, raw_key = False):
        if not value and not id and not parent_id:
            return None

        if not attributes:
            attributes = []

        if len(list(filter(lambda attr: attr.id == 'desc', attributes))) == 0:
            if parent_id:
                attributes.append(FluentAstAttribute('desc', '{ ' + FluentSerializedMessage.get_key(parent_id) + '.desc' + ' }'));
            else:
                attributes.append(FluentAstAttribute('desc', '{ "" }'))

        message = f'{cls.get_key(id, raw_key)} = {cls.get_value(value, parent_id)}\n'

        if attributes and len(attributes):
            full_message = message

            for attr in attributes:
                fluent_newlines = attr.value.replace("\n", "\n        ");
                full_message = cls.add_attr(full_message, attr.id, fluent_newlines, raw_key=raw_key)

            desc_attr = py_.find(attributes, lambda a: a.id == 'desc')
            if not desc_attr and parent_id:
                full_message = cls.add_attr(full_message, 'desc', '{ ' + FluentSerializedMessage.get_key(parent_id) + '.desc' + ' }')

            return full_message

        return cls.to_serialized_message(message)

    @classmethod
    def from_lokalise_keys(cls, keys: typing.List[LokaliseKey]):
        attributes_keys = list(filter(lambda k: k.is_attr, keys))
        attributes = list(map(lambda k: FluentAstAttribute(id='.{name}'.format(name=k.get_key_last_name(k.key_name)),
                                                           value=FluentSerializedMessage.get_attr(k, k.get_key_last_name(k.key_name)), parent_key=k.get_parent_key()),
                              attributes_keys))
        attributes_group = py_.group_by(attributes, 'parent_key')

        serialized_message = ''
        for key in keys:
            if key.is_attr:
                continue
            key_name = key.get_key_last_name(key.key_name)
            key_value = key.get_translation('ru').data['translation']
            key_attributes = []

            if len(attributes_group):
                k = f'{key.get_key_base_name(key.key_name)}.{key_name}'
                key_attributes = attributes_group[k] if k in attributes_group else []

            message = key.serialize_message()
            full_message = cls.from_yaml_element(key_name, key_value, key_attributes, key.get_parent_key(), True)

            if full_message:
                serialized_message = serialized_message + '\n' + full_message
            elif message:
                serialized_message = serialized_message + '\n' + message
            else:
                raise Exception('Что-то пошло не так')

        return serialized_message

    @staticmethod
    def get_attr(k, name, parent_id = None):
        if parent_id:
            return "{ " + parent_id + f'.{name}' + " }"
        else:
            return k.get_translation('ru').data['translation']


    @staticmethod
    def to_serialized_message(string_message):
        if not string_message:
            return None

        ast_message = FluentParser().parse(string_message)
        serialized = FluentSerializer(with_junk=True).serialize(ast_message)

        return serialized if serialized else ''

    @staticmethod
    def add_attr(message_str, attr_key, attr_value, raw_key = False):
        prefix = '' if raw_key else '.'
        return f'{message_str}\n  {prefix}{attr_key} = {attr_value}'

    @staticmethod
    def get_value(value, parent_id):
        if value:
            return value
        elif parent_id:
            return '{ ' + FluentSerializedMessage.get_key(parent_id) + ' }'
        else:
            return '{ "" }'

    @staticmethod
    def get_key(id, raw = False):
        if raw:
            return f'{id}'
        else:
            return f'ent-{id}'
