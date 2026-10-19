# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 152.4 MiB | 135.7 MiB | -10.9% |
| Settled private bytes | 99.8 MiB | 90.6 MiB | -9.2% |
| Peak working set | 228.3 MiB | 211.1 MiB | -7.5% |
| Settled working set | 20.7 MiB | 20.9 MiB | +0.6% |

Idle CPU (one logical core, periodic sampler stopped): baseline 1.061%; candidate 0.687%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
