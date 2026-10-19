"""Prepare short PCM cues from the researched atata reference; never run on app startup.
Usage: python tools/prepare_sounds.py /path/to/bubu-dudu-atata.mp3 (requires ffmpeg).
The reference is a user-uploaded soundboard clip, not a verified creator master.
"""
import hashlib
import io
from pathlib import Path
import subprocess
import sys
import wave
import numpy as np

source = Path(sys.argv[1])
expected = '8504bc4e7edded2711fec3e0ec2c87edee0c643ba9b37c2899bd5cabd0197731'
if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
    raise SystemExit('Reference SHA-256 differs; review the source before generating cues.')
raw = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(source), '-ar', '22050', '-ac', '1', '-f', 's16le', '-'])
samples = np.frombuffer(raw, dtype='<i2').astype(float)
root = Path(__file__).resolve().parents[1] / 'windows/BuduPing/Assets/Sounds'
# First three syllables for a new reaction; single syllables for immediate spam beats.
for name, start, stop in [('atata', .10, .88), ('ta', .35, .57), ('ta-accent', .64, .87)]:
    cue = samples[round(start*22050):round(stop*22050)].copy()
    fade = round(.006*22050)
    cue[:fade] *= np.linspace(0, 1, fade)
    cue[-fade:] *= np.linspace(1, 0, fade)
    cue *= .80 * 32767 / max(1, np.max(np.abs(cue)))
    with wave.open(str(root / (name+'.wav')), 'wb') as out:
        out.setparams((1, 2, 22050, 0, 'NONE', 'not compressed'))
        out.writeframes(cue.astype('<i2').tobytes())
    print(f'{name}: {len(cue)/22050:.3f}s, {len(cue)*2} bytes PCM')


# Character voice cues (0.2.4, measured — see docs/SOUND-REPORT.md):
# cut from the same Myinstants references above plus "tata lala" (same uploader).
#   bubu-atata.wav  atata 0.10-0.88   F0 ~474 Hz  (white panda voice)
#   bubu-tata.wav   atata 1.12-1.72   F0 ~507 Hz
#   bubu-ta.wav     atata 1.78-2.00   F0 ~459 Hz
#   bubu-lala.wav   tatalala 1.58-2.23 F0 ~401 Hz
#   dudu-tata.wav   tatalala 0.35-1.00 F0 ~202 Hz  (brown bear voice, ~an octave lower)
#   dudu-hum.wav    tatalala 1.28-1.52 F0 ~127 Hz
# Staged + verified by the sound-candidates pipeline; WAVs committed under
# windows/BuduPing/Assets/Sounds/ and loaded by SoundService.VoiceCues.
