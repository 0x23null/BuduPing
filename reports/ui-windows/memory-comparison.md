# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 156.0 MiB | 131.0 MiB | -16.0% |
| Settled private bytes | 81.6 MiB | 82.7 MiB | +1.3% |
| Peak working set | 231.0 MiB | 210.2 MiB | -9.0% |
| Settled working set | 20.3 MiB | 22.4 MiB | +10.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.311%; candidate 0.561%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
