BuduPing 0.2.1 introduces a native UI inspired by the approved prototype, without its time-based suggestion.

- Android: compact 4×3 character grid, light/dark themes, combined name/pair setup, native variant preview, favorites, local send effects and a returning hug.
- Windows: spring entrance and delayed chat bubble, local vector hearts, bounded combo effects, native settings with a placement preview, monitor/corner/size selection, discreet mode and reduced motion.
- Ctrl+Alt+Space, the tray or settings sends a hug back to the phone. The phone receives replies while foregrounded.
- New delivery receipts distinguish app display from a human read receipt. Text is kept separate from ambient combos; deferred text stays in the inbox.
- Decoded GIFs stay cached during bursts and are released after idle; no forced GC/working-set trim. Active animation resources are pinned, and mini-parade stickers reuse a static frame to avoid extra GIF decoders.
- Both apps use the same curated sticker catalog. Windows remains one self-contained EXE.

This is a preview for real-device feedback. Native builds and automated checks are documented in `docs/DEVELOPMENT-REPORT-0.2.1.md`. RAM/latency on real hardware, mixed-DPI placement and end-to-end offline/reply behavior still need device validation. Fullscreen/screen-sharing detection and new bespoke character choreography are not included.

The APK is a debug build. Existing APKs from independent GitHub runners may have a different signing certificate; uninstalling the old APK may be necessary and clears local pairing/name settings. Stable release signing is not configured in this repository.
