# BuduPing 0.2.2 — smaller reactions, atata feedback, clear startup

Owner requests: 2026-10-19, Asia/Bangkok, 08:49 and 08:57.
Base: shipped **v0.2.1**, commit `1a1db6472360b9db9019d08496f4fc61eb5cd9da`.
Work branch: `codex/buduping-0.2.2`. Main was still v0.1.3 when work began; this branch includes the full shipped 0.2.1 history. No tag is rewritten.

## Scope and decisions

- The owner reports that 0.2.1 RAM is already good. Preserve its WpfAnimatedGif renderer, bounded decode cache, compressed sources, winmm playback and idle memory policy. Do **not** apply the older unpushed XamlAnimatedGif/NAudio experiment from another local checkout.
- Native WPF + Kotlin/Compose remain the stack. Apply MotionDesign's motion/accessibility principles with native transforms; website AIDA/GSAP/hero requirements do not apply to these screens. No browser runtime or new media framework is added.
- Scale the entire sticker card to **45%**, independently of message text. Artwork: 244 DIP previously; now approximately **66 / 110 / 143 DIP** for Small / Medium / Large. Existing saved sizes migrate to the matching named choice. Message body stays readable at 18 DIP. DPI scaling still applies normally.
- Combo growth is now bounded at 1.20x, with a brief squash/stretch. Accent bursts at 3, 5, then every fifth tap. At most **12 particles**, two static mini images once per burst, visual beat gap 110 ms, variant decode gap 800 ms. No GIF decoder is created for miniature copies.
- The existing winmm audio engine plays three small mono PCM cues: **a-ta-ta** on a new reaction, short **ta** on subsequent taps, alternate syllable at accents. Two voices maximum, minimum 110 ms between accepted sounds, drop saturated audio beats rather than enqueueing or resetting a live buffer. Counter/delivery processing still handles every event. Mute/discreet stops active audio immediately.
- Remove the redundant settings **Đóng** button. Title-bar close continues hiding settings; tray Quit exits.
- Message header uses a vector heart, plain mood labels and restrained cocoa/rose colors rather than mixed emoji/font colors.

## Correct identities and persistence

**Bubu = white panda; Dudu = brown bear.** Shared catalog, selector portraits, PC labels and solo tags are corrected. Duo animations remain available to either character.

The shipped 0.2.1 picker had reversed labels. A one-time schema migration swaps saved role IDs **to preserve the previously selected avatar**; the displayed name now becomes correct. Android `character_schema=2`; Windows `PresentationSchema=2`. Pair codes, sound/discreet/privacy preferences and favorites stay intact. New selections use canonical IDs.

The generated catalog is now authoritative on Android (removed a duplicated fallback character map). A mood with no eligible sticker is visibly unavailable; it never falls back to the other character's solo. In the current set Dudu has no sad/sorry solo or duo, so those tiles are disabled. Selecting Bubu enables the white-panda assets. Unknown/missing IDs do not pick an unrelated mood on PC.

## Protocol and spam fix

Android adds `character` and `variantLocked` to inbox writes. Quick taps send a concrete eligible variant with `variantLocked=false`; long-press manual selection sends `true`. Windows combines only same mood, same character, same local/remote origin, no text, and two unlocked events within two seconds. Missing flag preserves old clients' explicit-variant semantics. This fixes 0.2.1 quick taps being treated as manual variants, preventing receiver combos. Rotation during a combo remains filtered by the sender's role. PC reply preview and Android received-reply dialog honor the chosen reply variant.

## Discreet behavior

The reported problem was a privacy bypass: local test/reply confirmations had `IsLocal=true`, bypassing the quiet branch and showing the full bubble. The config flag did not need to be cleared to cause this symptom. Local and remote reactions now follow the same peek policy. Only remote events remain pending; tests/reply confirmations do not populate the pending inbox or change privacy preferences. `OpenPending` remains the explicit action that turns off discreet/hide-text to read waiting messages. Local preview events cannot consume a remote combo or complete its delivery.

## Startup UX follow-up

Manual launch after setup shows a small native **BuduPing đã chạy** notice for 2.4 seconds; click opens settings. No GIF/audio is loaded for this notice and no repeating timer remains. First run already opens setup. Autostart uses `--autostart` and stays quiet. A per-user named mutex/event lets a second manual launch activate existing settings instead of creating another resident process. Offline review/benchmark modes bypass instance registration and production Firebase/config loading.

## Audio reference and reproducibility

Reference researched: [Bubu dudu atata, Myinstants](https://www.myinstants.com/en/instant/bubu-dudu-atata-30881/), uploaded by `shoro7`; linked media `/media/sounds/bubu-dudu-atata.mp3`. This is a user-uploaded reference, **not a verified creator master or a claim of ownership/licensing**. The original 2.142 s download stays outside the repo. Only short prepared PCM cuts are embedded: 0.780 / 0.220 / 0.230 s, total roughly 53 KiB. `tools/prepare_sounds.py` records the reference SHA-256, exact cut points, fades and normalization; developer-only dependency ffmpeg + numpy. Builds use the committed WAVs and never download audio. Playback on a physical speaker remains a listening check.

## Validation and continuation

Local: all XAML parses; asset pipeline regenerates 27 GIF entries and 12 moods; three existing asset tests pass. Matching native CI results will be appended below.

Windows `--ui-review DIR` now captures settings, message, compact size, combo, discreet preview, and startup notice; checks 13-tap and 200-tap counters/particle bounds and local test/reply quiet behavior. Core checks cover text/queue TTL, old/manual variants, new auto combos and role boundaries. Android instrumented tests check both role mappings, strict filtering, one-time preference migration, character dialog and disabled unavailable moods, plus existing themes/large text/setup.

Windows CI compares native memory/CPU using the **same offline workload** compiled into unmodified shipped 0.2.1 plus a measurement hook, then the candidate. It samples working set, private bytes and managed heap through six GIF moods twice, a 20-tap burst and 25 s idle. Both versions retain the shipped idle GC/working-set trim; the harness forces neither. One runner measurement is an observation, not a physical-PC budget or statistical performance proof. CI JSON and screenshots are persisted on `ci-reports` as well as uploaded as artifacts.

Remaining device checks: audible rhythm/volume during rapid taps; mixed-DPI and all-corner placement; clicking an already running EXE; autostart suppression; real Firebase Android → PC combo and reply; long idle/foreground Task Manager readings on the owner's PC. No production pair is used by tests.

### Native visual follow-up

At d7dcafa, WPF publish, 558 core assertions, quiet-mode tests and 200-tap test passed. Native screenshots confirmed 109.8 / 65.88 DIP artwork, readable unscaled message text, removed close button and the startup notice. Visual review found that center-origin particle scaling displaced a large halo below the small card; use a zero origin for particle elements so size and position are scaled consistently. Revalidate matching native captures after this fix.

### Android visual follow-up

At d7dcafa, APK run [39188178441](https://github.com/0x23null/BuduPing/actions/runs/39188178441) and all six Android tests in [39188178366](https://github.com/0x23null/BuduPing/actions/runs/39188178366) passed. Inspecting the actual PNGs found header crowding after the character chip was added: the duplicated sender/status column wrapped vertically at 320 dp / 1.3 font scale. Remove that duplicate column; sender/status remain in the existing summary card. Capture explicit tagged screen/dialog surfaces rather than choosing the last Compose root, which could capture the background instead of the modal. These changes receive a fresh APK/UI run.

First Windows comparison at d7dcafa (run [39188178342](https://github.com/0x23null/BuduPing/actions/runs/39188178342)): peak private 158.2 → 124.3 MiB; settled private 85.1 → 77.8 MiB; peak working set 234.0 → 201.0 MiB; settled working set 20.9 → 19.6 MiB. Idle CPU was 0.559% → 0.935% of one logical core. This single run supports lower memory in that workload, **not** a zero-idle-CPU claim or a device-independent improvement percentage. The follow-up particle-origin fix was visually confirmed in native captures from de3cd7e; its matching full benchmark is recorded separately.

Second comparison at de3cd7e, [39188492993](https://github.com/0x23null/BuduPing/actions/runs/39188492993), also passed all Windows checks: peak private 149.4 → 132.3 MiB; settled private 78.9 → 93.3 MiB; peak working set 227.1 → 204.1 MiB; settled working set 21.7 → 10.9 MiB. Idle CPU 0.999% → 1.311% of one core. Settled private allocation varied in the opposite direction, so do not claim every memory metric improves. Runtime combo rotation can choose different eligible GIFs, and optimized GC/retained heap commitments vary.

Measurement correction: the first two idle CPU results included the harness's own 500 ms process-query timer. Stop that periodic sampler before the 25 s idle interval in **both** baseline and candidate, then sample again at the end. This changes measurement only; production memory/GC/audio behavior stays unchanged. The final comparison uses this corrected harness. Physical-device checking remains open.

## Final source validation before release

- Android production/UI source `15a3798`: [APK 39188973677](https://github.com/0x23null/BuduPing/actions/runs/39188973677) and [six instrumented tests 39188973650](https://github.com/0x23null/BuduPing/actions/runs/39188973650) pass. Seven PNGs collected: light, dark, Dudu, large text, character picker, variant picker and setup. Tagged dialog capture now shows the correct modal; large-font header stays horizontal. Later commits change only Windows/profiling/docs; [APK 39189088503](https://github.com/0x23null/BuduPing/actions/runs/39189088503) also passes.
- Final Windows application source `b97464f`: [39189367228](https://github.com/0x23null/BuduPing/actions/runs/39189367228) passes publish, 558 core assertions, three asset tests, native character availability mapping, 13/200-tap checks, discreet test/reply checks, screenshot geometry and baseline/candidate measurement. The test button now chooses only moods supported by the selected role, avoiding an empty preview for Dudu's unavailable sad/sorry moods.
- Final corrected-harness comparison from that run:

| Metric | Shipped 0.2.1 | Candidate 0.2.2 |
| --- | ---: | ---: |
| Peak private | 149.7 MiB | 152.2 MiB |
| Settled private | 98.1 MiB | 84.4 MiB |
| Peak working set | 227.6 MiB | 226.5 MiB |
| Settled working set | 18.6 MiB | 17.7 MiB |
| Idle CPU, one logical core | 0.186% | 0.312% |

Across runs, peak memory is comparable or lower; one peak-private result is +1.7%. Idle/private metrics vary, so **do not promise a fixed percentage saving or identical readings on the owner's machine**. The performance decision is to retain the proven 0.2.1 renderer/cache/trim, bound work and avoid new media frameworks. Both private allocation and working set remain visible in reports.

- Asset corner audit: 27 packaged GIFs, **292 frames**, every frame's four corners transparent. This does not prove edge/halo quality. The revised [STICKER-TEST-PLAN.md](STICKER-TEST-PLAN.md) distinguishes baked asset backgrounds from the intentional native bright card, corrects role names/retired love-01, and covers physical-speaker spam, quiet mode and startup/relaunch.
- Native Windows PNG/JSON and memory evidence: [ci-reports/reports/ui-windows](https://github.com/0x23null/BuduPing/tree/ci-reports/reports/ui-windows). Android PNGs: artifact on the linked matching UI run.

The documentation-only final commit skips redundant branch CI. Product code matches the validated commits above; merge/release still performs both platform builds and publishes APK + EXE together. Publication result is recorded below.

## Published

[PR #2](https://github.com/0x23null/BuduPing/pull/2) merged as `35da72f47912bbb764f843d98391f3f08d2a815e`. [Release workflow 39189895840](https://github.com/0x23null/BuduPing/actions/runs/39189895840) completed successfully: version, APK, Windows and publish jobs all passed. [v0.2.2](https://github.com/0x23null/BuduPing/releases/tag/v0.2.2) is a prerelease with both files uploaded from that exact merge commit. The older draft PR #1 was also marked merged automatically because its history is included.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| BuduPing.apk | 20176416 | `c52b1a53e8bbe2dc24b570ccda9e59588db7574419433ae26f6462ce8b6c1ff5` |
| BuduPing.exe | 170814896 | `a3be67a72f06f59e6bfcaa25d33bf15b593a76329bac325fdabdaf353eea48bf` |

The final main documentation commit only records this publication; application code remains the validated release source. Debug APK signatures can differ between runners; if Android refuses an in-place update, uninstalling/re-pairing may be necessary. Close the previous portable EXE before starting the new version. Physical-speaker listening, actual-device RAM/latency, mixed-DPI, autostart/relaunch and real Firebase round trips remain the explicit device checklist above.
