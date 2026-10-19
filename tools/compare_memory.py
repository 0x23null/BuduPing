"""Summarize two native process runs. Measurements are observations, not hardware-independent budgets."""
import json
from pathlib import Path
import sys

root = Path(sys.argv[1])
before = json.loads((root / 'before/memory.json').read_text())
after = json.loads((root / 'after/memory.json').read_text())
rows = [
    ('Peak private bytes', 'peakPrivateBytes'), ('Settled private bytes', 'settledPrivateBytes'),
    ('Peak working set', 'peakWorkingSet'), ('Settled working set', 'settledWorkingSet'),
]
lines = ['# Native offline memory comparison', '', 'Same Windows runner, same harness, GIFs, mood sequence and 20-tap combo; both retain the shipped idle GC/working-set trim. The harness itself forces neither.', '',
         '| Metric | v0.2.1 | Candidate | Change |', '| --- | ---: | ---: | ---: |']
for label, key in rows:
    b, a = before[key], after[key]
    lines.append(f'| {label} | {b/1048576:.1f} MiB | {a/1048576:.1f} MiB | {(a/b-1)*100:+.1f}% |')
lines += ['', f'Idle CPU (one logical core): baseline {before["idleOneCorePercent"]:.3f}%; candidate {after["idleOneCorePercent"]:.3f}%.', '',
          'Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.']
(root / 'comparison.md').write_text('\n'.join(lines)+'\n')
print('\n'.join(lines))
