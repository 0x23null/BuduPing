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
