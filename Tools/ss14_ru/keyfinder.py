import typing
import logging

from pydash import py_

from file import FluentFile
from fluentast import FluentAstAbstract
from fluentformatter import FluentFormatter
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


# Осуществляет актуализацию ключей. Находит файлы английского перевода, проверяет: есть ли русскоязычная пара
# Если нет - создаёт файл с копией переводов из англоязычного
# Далее, пофайлово проверяются ключи. Если в английском файле больше ключей - создает недостающие в русском, с английской копией перевода
# Отмечает русские файлы, в которых есть те ключи, что нет в аналогичных английских
# Отмечает русские файлы, у которых нет англоязычной пары

######################################### Class defifitions ############################################################
class RelativeFile:
    def __init__(self, file: FluentFile, locale: typing.AnyStr, relative_path_from_locale: typing.AnyStr):
        self.file = file
        self.locale = locale
        self.relative_path_from_locale = relative_path_from_locale


class FilesFinder:
    def __init__(self, project: Project):
        self.project: Project = project
        self.created_files: typing.List[FluentFile] = []

    def get_relative_path_dict(self, file: FluentFile, locale):
        if locale == 'ru-RU':
            return RelativeFile(file=file, locale=locale,
                                relative_path_from_locale=file.get_relative_path(self.project.ru_locale_dir_path))
        elif locale == 'en-US':
            return RelativeFile(file=file, locale=locale,
                                relative_path_from_locale=file.get_relative_path(self.project.en_locale_dir_path))
        else:
            raise Exception(f'Локаль {locale} не поддерживается')

    def get_file_pair(self, en_file: FluentFile) -> typing.Tuple[FluentFile, FluentFile]:
        ru_file_path = en_file.full_path.replace('en-US', 'ru-RU')
        ru_file = FluentFile(ru_file_path)

        return en_file, ru_file

    def execute(self):
        self.created_files = []
        groups = self.get_files_pars()
        keys_without_pair = list(filter(lambda g: len(groups[g]) < 2, groups))

        for key_without_pair in keys_without_pair:
            relative_file: RelativeFile = groups.get(key_without_pair)[0]

            if relative_file.locale == 'en-US':
                ru_file = self.create_ru_analog(relative_file)
                self.created_files.append(ru_file)
            elif relative_file.locale == 'ru-RU':
                is_engine_files = "robust-toolbox" in (relative_file.file.full_path)
                is_corvax_files = "corvax" in (relative_file.file.full_path)
                if not is_engine_files and not is_corvax_files:
                    self.warn_en_analog_not_exist(relative_file)
            else:
                raise Exception(f'Файл {relative_file.file.full_path} имеет неизвестную локаль "{relative_file.locale}"')

        return self.created_files

    def get_files_pars(self):
        en_fluent_files = self.project.get_fluent_files_by_dir(project.en_locale_dir_path)
        ru_fluent_files = self.project.get_fluent_files_by_dir(project.ru_locale_dir_path)

        en_fluent_relative_files = list(map(lambda f: self.get_relative_path_dict(f, 'en-US'), en_fluent_files))
        ru_fluent_relative_files = list(map(lambda f: self.get_relative_path_dict(f, 'ru-RU'), ru_fluent_files))
        relative_files = py_.flatten_depth(py_.concat(en_fluent_relative_files, ru_fluent_relative_files), depth=1)

        return py_.group_by(relative_files, 'relative_path_from_locale')

    def create_ru_analog(self, en_relative_file: RelativeFile) -> FluentFile:
        en_file: FluentFile = en_relative_file.file
        en_file_data = en_file.read_data()
        ru_file_path = en_file.full_path.replace('en-US', 'ru-RU')
        ru_file = FluentFile(ru_file_path)
        ru_file.save_data(en_file_data)

        logging.info(f'Создан файл {ru_file_path} с переводами из английского файла')

        return ru_file

    def warn_en_analog_not_exist(self, ru_relative_file: RelativeFile):
        file: FluentFile = ru_relative_file.file
        en_file_path = file.full_path.replace('ru-RU', 'en-US')

        logging.warning(f'Файл {file.full_path} не имеет английского аналога по пути {en_file_path}')


class KeyFinder:
    def __init__(self, files_dict):
        self.files_dict = files_dict
        self.changed_files: typing.List[FluentFile] = []

    def execute(self) -> typing.List[FluentFile]:
        self.changed_files = []
        for pair in self.files_dict:
            ru_relative_file = py_.find(self.files_dict[pair], {'locale': 'ru-RU'})
            en_relative_file = py_.find(self.files_dict[pair], {'locale': 'en-US'})

            if not en_relative_file or not ru_relative_file:
                continue

            ru_file: FluentFile = ru_relative_file.file
            en_file: FluentFile = en_relative_file.file

            self.compare_files(en_file, ru_file)

        return self.changed_files


    def compare_files(self, en_file, ru_file):
        ru_file_parsed: ast.Resource = ru_file.parse_data(ru_file.read_data())
        en_file_parsed: ast.Resource = en_file.parse_data(en_file.read_data())

        self.write_to_ru_files(ru_file, ru_file_parsed, en_file_parsed)
        self.log_not_exist_en_files(en_file, ru_file_parsed, en_file_parsed)


    def write_to_ru_files(self, ru_file, ru_file_parsed, en_file_parsed):
        for idx, en_message in enumerate(en_file_parsed.body):
            if isinstance(en_message, ast.ResourceComment) or isinstance(en_message, ast.GroupComment) or isinstance(en_message, ast.Comment):
                continue

            ru_message_analog_idx = py_.find_index(ru_file_parsed.body, lambda ru_message: self.find_duplicate_message_id_name(ru_message, en_message))
            have_changes = False

            # Attributes
            if getattr(en_message, 'attributes', None) and ru_message_analog_idx != -1:
                if not ru_file_parsed.body[ru_message_analog_idx].attributes:
                    ru_file_parsed.body[ru_message_analog_idx].attributes = en_message.attributes
                    have_changes = True
                else:
                    for en_attr in en_message.attributes:
                        ru_attr_analog = py_.find(ru_file_parsed.body[ru_message_analog_idx].attributes, lambda ru_attr: ru_attr.id.name == en_attr.id.name)
                        if not ru_attr_analog:
                            ru_file_parsed.body[ru_message_analog_idx].attributes.append(en_attr)
                            have_changes = True

            # New elements
            if ru_message_analog_idx == -1:
                ru_file_body = ru_file_parsed.body
                if (len(ru_file_body) >= idx + 1):
                    ru_file_parsed = self.append_message(ru_file_parsed, en_message, idx)
                else:
                    ru_file_parsed = self.push_message(ru_file_parsed, en_message)
                have_changes = True

            if have_changes:
                serialized = serializer.serialize(ru_file_parsed)
                self.save_and_log_file(ru_file, serialized, en_message)

    def log_not_exist_en_files(self, en_file, ru_file_parsed, en_file_parsed):
        for idx, ru_message in enumerate(ru_file_parsed.body):
            if isinstance(ru_message, ast.ResourceComment) or isinstance(ru_message, ast.GroupComment) or isinstance(ru_message, ast.Comment):
                continue

            en_message_analog = py_.find(en_file_parsed.body, lambda en_message: self.find_duplicate_message_id_name(ru_message, en_message))

            if not en_message_analog:
                logging.warning(f'Ключ "{FluentAstAbstract.get_id_name(ru_message)}" не имеет английского аналога по пути {en_file.full_path}"')

    def append_message(self, ru_file_parsed, en_message, en_message_idx):
        ru_message_part_1 = ru_file_parsed.body[0:en_message_idx]
        ru_message_part_middle = [en_message]
        ru_message_part_2 = ru_file_parsed.body[en_message_idx:]
        new_body = py_.flatten_depth([ru_message_part_1, ru_message_part_middle, ru_message_part_2], depth=1)
        ru_file_parsed.body = new_body

        return ru_file_parsed

    def push_message(self,  ru_file_parsed, en_message):
        ru_file_parsed.body.append(en_message)
        return ru_file_parsed

    def save_and_log_file(self, file, file_data, message):
        file.save_data(file_data)
        logging.info(f'В файл {file.full_path} добавлен ключ "{FluentAstAbstract.get_id_name(message)}"')
        self.changed_files.append(file)

    def find_duplicate_message_id_name(self, ru_message, en_message):
        ru_element_id_name = FluentAstAbstract.get_id_name(ru_message)
        en_element_id_name = FluentAstAbstract.get_id_name(en_message)

        if not ru_element_id_name or not en_element_id_name:
            return False

        if ru_element_id_name == en_element_id_name:
            return ru_message
        else:
            return None

######################################## Var definitions ###############################################################

logging.basicConfig(level = logging.INFO)
project = Project()
parser = FluentParser()
serializer = FluentSerializer(with_junk=True)
files_finder = FilesFinder(project)
key_finder = KeyFinder(files_finder.get_files_pars())

########################################################################################################################

print('Проверка актуальности файлов ...')
created_files = files_finder.execute()
if len(created_files):
    print('Форматирование созданных файлов ...')
    FluentFormatter.format(created_files)
print('Проверка актуальности ключей ...')
changed_files = key_finder.execute()
if len(changed_files):
    print('Форматирование изменённых файлов ...')
    FluentFormatter.format(changed_files)
