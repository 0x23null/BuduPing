import importlib.util
import tempfile
import unittest
from pathlib import Path
from PIL import Image

SPEC = importlib.util.spec_from_file_location('prepare_assets', Path(__file__).resolve().parents[2] / 'tools/prepare_assets.py')
assets = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(assets)

class AssetTests(unittest.TestCase):
    def test_union_includes_motion_outside_first_frame(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'hug.gif'
            a, b = Image.new('RGBA', (40, 40)), Image.new('RGBA', (40, 40))
            for x in range(3, 12):
                for y in range(5, 15): a.putpixel((x, y), (200, 50, 20, 255))
            for x in range(22, 35):
                for y in range(25, 38): b.putpixel((x, y), (200, 50, 20, 255))
            a.save(path, save_all=True, append_images=[b], duration=100, disposal=2)
            info, preview = assets.inspect_gif(path)
            self.assertEqual(info['bounds'], [3, 5, 35, 38])
            self.assertEqual(preview.size, (184, 184))
            self.assertEqual(info['frames'], 2)
            self.assertEqual(preview.getpixel((0, 0))[3], 0)

    def test_curated_files_have_all_emotions_and_valid_bounds(self):
        files = list(assets.SOURCE.glob('*/*.gif'))
        self.assertTrue(files)
        self.assertEqual({p.parent.name for p in files}, assets.TYPES)
        for path in files:
            with self.subTest(path=path.name):
                info, _ = assets.inspect_gif(path)
                x, y, r, b = info['bounds']
                self.assertTrue(0 <= x < r <= info['width'])
                self.assertTrue(0 <= y < b <= info['height'])
                self.assertGreater(info['estimatedBytes'], 0)

    def test_characters_map_covers_every_sticker(self):
        files = list(assets.SOURCE.glob('*/*.gif'))
        self.assertTrue(files)
        valid = {'bubu', 'dudu'}
        for path in files:
            with self.subTest(stem=path.stem):
                tags = assets.CHARACTERS.get(path.stem)
                self.assertIsNotNone(tags, f'missing character tag for {path.stem}')
                self.assertTrue(len(tags) >= 1)
                self.assertEqual(set(tags), set(tags) & valid, f'unknown character in {path.stem}: {tags}')
                self.assertEqual(tags, sorted(tags), f'keep character tags sorted in {path.stem}')

if __name__ == '__main__': unittest.main()
