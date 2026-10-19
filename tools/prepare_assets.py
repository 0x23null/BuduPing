"""Build the shared catalog from the curated Windows GIFs without rewriting animation data.
Requires Pillow. Run from any directory before either app is compiled.
"""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'windows/BuduPing/Assets/Stickers'
ANDROID = ROOT / 'android/app/src/main/assets'
TYPES = {'miss', 'love', 'hug', 'kiss', 'tease', 'angry', 'sad', 'sorry', 'funny', 'cheer', 'hungry', 'goodnight'}

# Which character(s) appear in each sticker, keyed by file stem.
# "dudu" = brown bear, "bubu" = white panda. Corrected by owner 2026-10-19. Curated by visual audit
# 2026-10-19: every frame contact-sheet was eyeballed; single-character
# stickers name exactly one character. Keep sorted alphabetically.
CHARACTERS = {
    'angry-01': ['bubu', 'dudu'],  # animation alternates: panda yelling, then bear yelling
    'angry-02': ['dudu'],
    'cheer-01': ['bubu', 'dudu'],
    'cheer-03': ['bubu', 'dudu'],
    'funny-01': ['bubu'],
    'funny-02': ['bubu', 'dudu'],
    'funny-03': ['dudu'],
    'goodnight-01': ['bubu', 'dudu'],
    'goodnight-03': ['bubu', 'dudu'],
    'hug-02': ['bubu', 'dudu'],
    'hug-03': ['bubu', 'dudu'],
    'hug-04': ['bubu', 'dudu'],
    'hungry-01': ['bubu', 'dudu'],
    'hungry-02': ['bubu'],  # white panda eating ice cream
    'kiss-01': ['bubu', 'dudu'],
    'kiss-02': ['bubu', 'dudu'],
    'kiss-03': ['bubu', 'dudu'],
    'love-02': ['dudu'],  # brown bear with "i love you" baked caption
    'love-03': ['bubu'],
    'miss-01': ['bubu', 'dudu'],
    'miss-03': ['bubu', 'dudu'],
    'miss-04': ['bubu'],
    'sad-01': ['bubu'],  # white panda crying a tear pool
    'sad-02': ['bubu'],  # white panda alone with purple object
    'sorry-01': ['bubu'],  # white panda bowing alone
    'sorry-02': ['bubu'],
    'tease-02': ['bubu', 'dudu'],
}


def inspect_gif(path):
    with Image.open(path) as gif:
        width, height = gif.size
        bounds = None
        best_frame, best_area = None, -1
        frames, duration = 0, 0
        for i in range(gif.n_frames):
            gif.seek(i)
            rgba = gif.convert('RGBA')
            # Ignore near-transparent edge noise when choosing the visible content box.
            mask = rgba.getchannel('A').point(lambda a: 255 if a > 16 else 0)
            box = mask.getbbox()
            if box:
                bounds = box if bounds is None else (
                    min(bounds[0], box[0]), min(bounds[1], box[1]),
                    max(bounds[2], box[2]), max(bounds[3], box[3]))
                area = sum(mask.histogram()[128:])
                if area > best_area:
                    best_frame, best_area = rgba.copy(), area
            frames += 1
            duration += max(20, gif.info.get('duration', 100))
        if best_frame is None:
            raise ValueError(f'Invisible GIF: {path}')
        bounds = bounds or (0, 0, width, height)
        # The union, not the first frame, prevents animated limbs from being clipped.
        preview = best_frame.crop(bounds)
        preview.thumbnail((168, 168), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA', (184, 184))
        canvas.alpha_composite(preview, ((184-preview.width)//2, (184-preview.height)//2))
        return dict(width=width, height=height, bounds=list(bounds), frames=frames,
                    durationMs=duration, estimatedBytes=width*height*4*frames), canvas


def main():
    files = sorted(SOURCE.glob('*/*.gif'))
    if not files:
        raise SystemExit('No curated GIFs found. Check out the full repository before preparing assets.')
    target = ANDROID / 'stickers'
    if target.exists():
        shutil.rmtree(target)
    previews = ANDROID / 'previews'
    if previews.exists():
        shutil.rmtree(previews)
    previews.mkdir(parents=True)
    catalog = []
    for path in files:
        kind = path.parent.name
        if kind not in TYPES:
            raise ValueError(f'Unknown emotion: {kind}')
        metadata, preview = inspect_gif(path)
        name = path.stem
        characters = CHARACTERS.get(name)
        if not characters:
            raise ValueError(f'Missing character tag for sticker: {name}')
        if any(c not in ('bubu', 'dudu') for c in characters):
            raise ValueError(f'Bad character tag on {name}: {characters}')
        dest = target / kind / path.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, dest)
        preview.save(previews / f'{name}.png', optimize=True)
        catalog.append(dict(id=name, type=kind, file=f'{kind}/{path.name}',
                            thumbnail=f'previews/{name}.png',
                            characters=characters,
                            sha256=hashlib.sha256(path.read_bytes()).hexdigest(), **metadata))
    assert {x['type'] for x in catalog} == TYPES
    data = json.dumps(catalog, ensure_ascii=False, indent=2) + '\n'
    (SOURCE / 'catalog.json').write_text(data, encoding='utf-8')
    (target / 'catalog.json').write_text(data, encoding='utf-8')
    print(f'Prepared {len(catalog)} curated stickers, {len(TYPES)} emotions, alpha-union bounds and static thumbnails.')


if __name__ == '__main__':
    main()
