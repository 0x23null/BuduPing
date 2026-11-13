# BuduPing character-voice sound research — report (2026-10-19)

Goal: give Bubu and Dudu audibly DIFFERENT voices and more than one cue each,
replacing the generic pop/atata PCM set. Staged in `sound-candidates/`; nothing
in the repo touched, nothing committed.

## ⚠️ Naming (read first)

Files are named for the **shipped app's labels** so wiring is direct:
**white panda = `bubu` (high voice), brown bear = `dudu` (low voice)** — the
v0.2.2 identity decision. In the creator's original canon these names are
swapped (white = 一二 Yier, brown = 布布 Bubu). Do not "correct" the filenames
toward canon; the app ships white=`bubu`.

## Sources surveyed

| Source | Result |
|---|---|
| Myinstants search "bubu dudu" / "dudu bubu" | 2 hits, both downloaded (see below) |
| Uploader shoro7's Myinstants profile | only those 2 uploads — nothing more |
| Voicy, 101soundboards, general web search for other mp3 hosts | no character-vocalization hits |
| YouTube (original-animation compilations) | bot-guard blocked extraction — noted, not bypassed |
| TikTok (@yierbubu20 official + fan comps) | bot-guard page, no media URL — noted, not bypassed |
| fish.audio "bubududu AI voice" | AI-generated TTS, not the characters' real voices — excluded |
| Tubidy/Peach-Goma fan-animation rips | different fan series with its own voices — excluded |
| Chinese web: 一二布布 配音 (voice-actor docs) | no public voice-actor documentation found |

**Candidate recordings downloaded: 2** (both fan uploads by `shoro7` on
Myinstants, same babble-voice lineage as the animations):

| File | URL | Duration | SHA-256 |
|---|---|---|---|
| `raw/atata.mp3` | https://www.myinstants.com/en/instant/bubu-dudu-atata-30881/ | 2.09 s | `8504bc4e7edded2711fec3e0ec2c87edee0c643ba9b37c2899bd5cabd0197731` (= the hash recorded in `tools/prepare_sounds.py` — identical file) |
| `raw/tatalala.mp3` | https://www.myinstants.com/en/instant/bubu-dudu-tata-lala-69975/ | 2.23 s | `fefbd5e18cd6cee960a1bb2979c63d31a45a94ba23850240140beaae0a7f0ad8` |

No music, no human speech, no meme songs — both are pure character babble.

## The two voices — measured, not asserted

Method: 16 kHz mono, 30 ms frames, voiced = RMS ≥ 25% of peak; F0 per frame
by autocorrelation with octave disambiguation (70–1400 Hz), median over voiced
frames; spectral centroid over the same frames. Scripts: `prepare_cues.py`
(cuts) + the analysis snippets in this session's log.

Per-syllable segmentation (energy onsets) of the two recordings:

| Clip | Segment | Content | Median F0 |
|---|---|---|---|
| atata | 0.14–0.24 s | "a" | 500 Hz |
| atata | 0.38–0.54 s | "ta" | 485 Hz |
| atata | 0.66–0.80 s | "ta" | 457 Hz |
| atata | 1.16–1.28 s | "ta" | 500 Hz |
| atata | 1.42–1.66 s | "ta" | 500 Hz |
| atata | 1.80–1.96 s | "ta" | 471 Hz |
| tatalala | 0.38–0.98 s | "ta-ta" | **203 Hz** |
| tatalala | 1.30–1.50 s | low hum | **127 Hz** |
| tatalala | 1.60–2.22 s | "la-la" | **405 Hz** |

Two cleanly separated groups, roughly an octave apart:

- **HIGH voice ≈ 400–510 Hz** (atata all syllables; tatalala "la-la") —
  bright, chattery, childlike. Spectral centroid ≈ 1700 Hz.
- **LOW voice ≈ 127–205 Hz** (tatalala "ta-ta" + hum) — warm, calm, deeper.
  Spectral centroid ≈ 1380 Hz.

Notably, `tatalala` is a tiny **dialogue**: the low voice says "ta-ta", then
the high voice answers "la-la" — both characters in one clip.

### Voice → character assignment (evidence-graded)

- **Objective (measured):** two distinct voices exist; pitch table above.
- **Consistent with canon, not proven by the uploader:** the high, talkative
  voice → the white panda; the low, calm voice → the brown bear. Canon
  personality descriptions agree: white Yier is 跳脫/talkative/dramatic, brown
  Bubu is 溫厚/quiet/gentle (Akko official creator article
  https://en.akkogear.com/bubu-dudu-or-yier-bubu-meet-the-real-creator-behind-the-internets-favorite-panda-and-bear/
  and zh-classical Wikipedia 布布一二). The uploader's titles do not attribute
  individual syllables to a character, so treat the mapping as
  "measurement + canon-consistent", not "uploader-confirmed".

## Final cue set — `final/` (6 cues, 135.5 KiB total)

Mono 16-bit PCM @ 22050 Hz, ≤ 1.0 s each, 6 ms fades, 0.80 peak normalize —
the exact convention of the app's existing cues, so they drop into the winmm
engine unchanged. Built by `prepare_cues.py` (reproducible; hashes verified).

| File | Cut | Dur | Meas. F0 | Character | Suggested moods |
|---|---|---|---|---|---|
| `bubu-atata.wav` | atata 0.10–0.88 | 0.78 s | 474 Hz | bubu (white) | hello, love, cheer — signature greeting |
| `bubu-tata.wav` | atata 1.12–1.72 | 0.60 s | 507 Hz | bubu (white) | kiss, miss, hug |
| `bubu-ta.wav` | atata 1.78–2.00 | 0.22 s | 459 Hz | bubu (white) | tap beat / combo tick, funny |
| `bubu-lala.wav` | tatalala 1.58–2.23 | 0.65 s | 401 Hz | bubu (white) | tease, funny, wow |
| `dudu-tata.wav` | tatalala 0.35–1.00 | 0.65 s | 202 Hz | dudu (brown) | hello, love, hug (brown-side variants) |
| `dudu-hum.wav` | tatalala 1.28–1.52 | 0.24 s | 127 Hz | dudu (brown) | sad, sleepy, sorry, angry (low grumble) |

Suggested engine routing: choose the cue **set by the sender's character**
(`bubu*` vs `dudu*`), then by mood within the set; keep the existing
two-voice polyphony cap and 110 ms beat spacing. `bubu-ta` / `dudu-hum` are
the natural replacements for the current generic tap/accent ticks.

## Licensing & honesty notes

- Both source clips are **fan uploads** (Myinstants user `shoro7`), not
  creator masters and not a claim of ownership/licensing. Original characters

- Same posture as the sticker set: **private 2-person use only**, tiny
  sub-second transformed excerpts, no redistribution. Myinstants permits
  personal download; this stays within that.
- Only **2 authentic character-vocalization recordings** were publicly
  accessible after surveying soundboards, video sites and the Chinese web;
  the 6 cues are the evidence-based maximum — no synthesized "voices", no
  wrong-character fan audio, no AI TTS.
- Not verified: per-syllable character attribution (uploader never labeled
  it); voice-actor identity; whether both clips share one recording session
  (stylistically they do, same uploader).
- Playback on a physical speaker remains a listening check (as with 0.2.2).
