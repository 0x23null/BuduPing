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
