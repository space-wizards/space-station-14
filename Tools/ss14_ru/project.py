import pathlib
import os
import glob
from file import FluentFile

class Project:
    def __init__(self):
        self.base_dir_path = pathlib.Path(os.path.abspath(os.curdir)).parent.parent.resolve()
        self.resources_dir_path = os.path.join(self.base_dir_path, 'Resources')
        self.locales_dir_path = os.path.join(self.resources_dir_path, 'Locale')
        self.ru_locale_dir_path = os.path.join(self.locales_dir_path, 'ru-RU')
        self.en_locale_dir_path = os.path.join(self.locales_dir_path, 'en-US')
        self.prototypes_dir_path = os.path.join(self.resources_dir_path, "Prototypes")
        self.en_locale_prototypes_dir_path = os.path.join(self.en_locale_dir_path, 'ss14-ru', 'prototypes')
        self.ru_locale_prototypes_dir_path = os.path.join(self.ru_locale_dir_path, 'ss14-ru', 'prototypes')
        self.ru_locale_exclude_dir_paths_ds = ['_adt', '_backmen', '_deadspace', '_lw'] # DS14
        self.exclude_dir_paths_ds = [r'_DeadSpace\Sponsor', r'_DeadSpace\Necromorfs', r'_DeadSpace\Spiders', r'_deadspace\sponsor'] # DS14

    def get_files_paths_by_dir(self, dir_path, files_extenstion):
        all_files = glob.glob(f'{dir_path}/**/*.{files_extenstion}', recursive=True)
        return [file for file in all_files if not any(excluded in file for excluded in self.exclude_dir_paths_ds)]

    def get_fluent_files_by_dir(self, dir_path):
        files = []
        files_paths_list = glob.glob(f'{dir_path}/**/*.ftl', recursive=True)
        
        for file_path in files_paths_list:
            if any(excluded in file_path for excluded in self.exclude_dir_paths_ds):
                continue
            
            try:
                files.append(FluentFile(file_path))
            except:
                continue

        return files

