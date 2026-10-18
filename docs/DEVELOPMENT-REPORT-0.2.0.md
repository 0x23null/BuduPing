# BuduPing 0.2.0 — implementation and handoff

Date: 2026-10-19 (Asia/Bangkok). Baseline: released v0.1.2 (`390791b`).
Work branch: `codex/buduping-0.2`. Status: implementation under CI validation; not a claim of device-tested performance.

## User direction

- Keep the native Kotlin/Compose sender and C# WPF receiver.
- Reference only the sending/receiving effects from https://muse.ai/s/loveping-web-prototype-cxj66tx0xaxqxfxue.
- Android should resemble the compact prototype layout: header, pair pill, combo card, 4-by-3 emotions, message field and quick messages. Never add the yellow time-based suggestion.
- Additional versions may land on main concurrently. Preserve independent improvements; do not reset or force-push main.
- Selected plugins: Themely for color direction, MotionDesign for motion/accessibility guidance, Mobile App Builder for the native core loop. Web-specific GSAP/hero requirements are inapplicable to native Android/WPF; no webview was introduced.

## Implemented

### Delivery and interaction

- `PingBacklog` keeps at most two waiting ambient taps and 64 total events. Ambient taps cannot evict text. Overflow stays in the Firebase inbox; an event-driven refresh retries it when the queue drains. There is no periodic inbox polling.
- Text-bearing events and explicit variants never merge into ambient combos. Accepted messages retain order.
- PC acknowledges only after entering a composition frame. An atomic multi-location PATCH writes `feedback/delivery` and removes `inbox/{pushKey}`. Retries are serialized with backoff. A process crash before completion can cause replay; exactly-once delivery is not claimed.
- Ambient events expire after 10 minutes; text after 24 hours. Old events receive `expired`; a small local summary replaces an old burst.
- Ctrl+Alt+Space or a tray/settings action sends one hug back. Registration failure is visible in settings; tray reply remains available. Android shows the returning hug and two short vibrations while foregrounded. No background push notification service was added.
- Manual discreet mode queues text on the server, peeks with a small silent sticker, and offers an explicit open-pending action. Privacy mode similarly avoids falsely declaring hidden text displayed. No fullscreen/screen-sharing detection is claimed.

### Visuals and UI

- Android: soft rose/cocoa light and dark themes; actual static character thumbnails; 4-column grid; compact combo progress; permanent optional message composer; long-press variant picker with animated preview on Android 9+; up to four manually pinned favorites; connection status has text and accessibility semantics.
- Sending button springs back and emits a short vector heart. No synthetic network delay is added.
- WPF: 680 ms spring entrance, 100 ms delayed bubble scaling, combo badge bounce, emotion-specific pose transforms and colored local glow. Combo changes variant every fourth tap and stays within a bounded scale and particle count.
- Settings: rounded native controls and switches, a visual placement preview, monitor/corner/size controls, privacy, reduced motion, sound, autostart and reply fallback. Android name and pairing are now one setup screen; the old NameScreen source is unused.
- Shared catalog is built from `windows/BuduPing/Assets/Stickers` ONLY. `tools/prepare_assets.py` scans all frames for alpha-union bounds, generates padded static thumbnails, copies the curated GIF set to Android, and embeds metadata in Windows. Original GIF bytes are preserved.

### Performance

- Decoded cache uses an estimated 32 MiB budget (active GIF may exceed this) and six-entry ceiling; clears after 20 seconds of true idle rather than after every display.
- Active GIF sources are reference-pinned so budget eviction cannot dispose a playing stream. Mini parade uses one shared static frame rather than three extra GIF decoders. Hidden GIF controllers are detached. Particle state and text glyphs are reused; geometry remains bounded to the corner.
- Removed forced GC / EmptyWorkingSet. Optional `BUDUPING_DIAGNOSTICS=1` writes bounded local JSONL with private bytes, working set, managed bytes and receive-to-composition timing. It contains no message text or pair codes. This timing does not measure end-to-end network latency.
- Settings are created lazily. Presence uses async PeriodicTimer; SSE ReadLine honors cancellation.
- v0.1.3 appeared during work (`e63894a`). Its overlapping 10-note SoundService is retained rather than reverting to the old SoundPlayer cutoff behavior.
- Integration at `7a88013` preserved main's bright sticker backdrop, larger message/badge text and +7% combo growth up to 1.85x. Main's history is merged, not reset. The earlier 1.3x growth cap was superseded during integration.

## Wire contract (backward compatible additions)

`/pairs/{pairId}/inbox/{pushKey}` remains the input path. Pair ID: eight normalized characters, no dash.

```json
{"id":"reference UUID","type":"hug","text":"up to 80 UTF-16 code units","from":"up to 24","ts":1234567890,"variant":"hug-02"}
```

`variant` is optional; unknown variants fall back within the emotion. The push key remains the identifier for completion.

`/pairs/{pairId}/feedback/delivery` is a bounded LAST status, not a delivery history:

```json
{"eventId":"pushKey","type":"hug","status":"displayed","ts":1234567890}
```

Other statuses: `received` (redacted content), `superseded` (ambient event coalesced), `expired`. Android matches `eventId` to its latest pending send and never calls this a human read receipt.

`/pairs/{pairId}/feedback/reply` is a bounded LAST reply:

```json
{"id":"unique reply ID","replyTo":"pushKey","type":"hug","ts":1234567890}
```

Android persists the last consumed reply ID per pair, and ignores reply animations older than ten minutes. Feedback is observed only while foregrounded. Android enables Firebase disk persistence for pending writes. Pairing settings are excluded from Android cloud backup.

Presence stays `{online:true,ts:<server timestamp>}`. Android locally ages it against server-clock offset; it also considers its own Firebase connection state. This local UI clock is not a database poll.

## Validation

- Local Roslyn/.NET 8 core checks: **550 assertions passed** (queue order/capacity, no lost message in combo, TTL, variant preservation, pairing checksum).
- Local Python asset checks: **2 tests passed**, including full-animation bounds beyond the first frame and checks on every curated GIF.
- Local XML parsing: all XAML and project XML valid.
- Local full dotnet CLI/MSBuild is unavailable due to this container's process metadata restrictions; direct Roslyn compiled and ran the core checks. Full native builds are validated by GitHub Actions. The APK and EXE passed at `7cf2cb4`, `c7f39bf` and `1a26d02`; subsequent changes are checked again.
- Native Windows settings screenshots rendered successfully at `1a26d02` (run `39154366713`). The offline `--ui-review` mode never loads personal config or connects to Firebase.
- Initial Windows CI exposed MSYS converting `/p:` arguments; changed to `-p:`. SDK is pinned to .NET 8 via global.json.
- CI outcomes and exact SHAs will be added after final validation. Read the matching SHA in ci-reports before interpreting a report, since other agents also push builds.

## Device verification still required

1. Windows + Android 8 fallback preview and Android 9+ animated picker.
2. End-to-end pairing/send/receipt/reply with actual Firebase, including offline writes and restart.
3. Two text messages during same-emotion spam; queue overflow; discreet mode, unhide and restart.
4. Multiple monitors at 100/150/200% DPI, all corners, non-default taskbar position.
5. Cold-start/idle/post-spam private bytes and latency with diagnostics; verify no idle animation continues.
6. Hotkey conflict, mute, autostart, tray lifecycle and switching pair while events arrive.
7. TalkBack, large fonts, keyboard visibility and 320/360/412 dp Android widths; screenshots and screen-reader checks have not been run in this Linux container.

## Remaining ideas, not shipped claims

- Bespoke new character clips for multi-step couple choreography. Current implementation reuses curated clips with native motion.
- Automatic fullscreen/presentation detection, draggable placement preview, quiet-hour schedule, custom embedded typography.
- A native Win32 renderer rewrite only after comparable measurements justify it.
- Firebase production authentication/rules redesign is separate work; no live rules were changed.

## Release procedure and signing

- Bumping root VERSION on main triggers the release pipeline. Both native builds and required core/asset checks must pass before one publish job attaches APK and EXE together. Tag-based releases and workflow_dispatch remain supported.
- 0.2.0 is published as a prerelease for device feedback, not a benchmark claim.
- APK is debug-signed on an ephemeral runner. It may require uninstalling an older independently signed APK, which clears name/pair preferences. Stable release signing remains open; no private signing key was committed.
