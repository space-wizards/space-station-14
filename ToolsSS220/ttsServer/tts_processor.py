import os
import torch
from pydub import AudioSegment
import soundfile as sf
import base64

class tts_creator:
    def __init__(self) -> None:
        self.device = torch.device('cpu')
        torch.set_num_threads(4)
        self.local_file = 'model.pt'

        if not os.path.isfile(self.local_file):
            torch.hub.download_url_to_file('https://models.silero.ai/models/tts/ru/v3_1_ru.pt',
                                   self.local_file)

        self.model = torch.package.PackageImporter(self.local_file).load_pickle("tts_models", "model")
        self.model.to(self.device)  

    def make_ogg_base64(self, text, speaker, sample_rate):
        audio_paths = self.model.save_wav(text=text,
                             speaker=speaker,
                             sample_rate=sample_rate)
        AudioSegment.from_wav(audio_paths).export('result.ogg', format='ogg')
        with open("result.ogg", 'rb') as f:
            return base64.b64encode(f.read()).decode()
    