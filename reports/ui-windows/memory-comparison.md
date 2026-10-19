# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 166.7 MiB | 150.7 MiB | -9.6% |
| Settled private bytes | 85.9 MiB | 76.0 MiB | -11.5% |
| Peak working set | 243.1 MiB | 223.6 MiB | -8.0% |
| Settled working set | 20.5 MiB | 21.2 MiB | +3.7% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.686%; candidate 0.499%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
