# BuduPing 0.2.1 — final visual review handoff

Date: 2026-10-19 (Asia/Bangkok).

The full 0.2 implementation, data contract, asset pipeline, performance approach and device checks are documented in [DEVELOPMENT-REPORT-0.2.0.md](DEVELOPMENT-REPORT-0.2.0.md).

A concurrent publisher created tag v0.2.0 at c3c112e while native screenshot review was still ongoing. That tag is preserved. This follow-up uses version 0.2.1 (Android versionCode 21) so its APK/EXE include the final visual fixes.

## Final review fixes

- Animate live WPF transforms directly; assert the entrance settles at scale 1 and slide 0.
- Wait for layout before the first particle burst and use stable layout coordinates, keeping effects near the character.
- Preserve v0.1.3 progressive combo growth up to 1.85x. Increase transparent host width to 520 DIP and anchor growth at the feet to keep the full character inside the window. Placement uses actual window dimensions.
- Show friendly monitor labels in native settings and include the background in screenshots.
- Fit all three emotion rows at normal Android font size; shorten the composer label for large text and prefer a cleaner love-tile thumbnail.
- Keep dialog surfaces in the rose/cocoa color system in both themes.
- Preserve the concurrent publish-job permission/token fix; publish APK and EXE together only after both builds succeed.

## Verification

Final native build, screenshot and release results will be recorded here after the matching CI runs complete. Screenshot artifacts include geometry diagnostics; all review fixtures are offline and never send to a real pair.

Real-device RAM/latency, mixed DPI, TalkBack, offline Firebase round trips and PC-to-phone replies still require device validation. Debug APK signing may require uninstalling an older APK and pairing again.
