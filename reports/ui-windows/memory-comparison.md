# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 166.7 MiB | 157.6 MiB | -5.5% |
| Settled private bytes | 110.8 MiB | 100.2 MiB | -9.6% |
| Peak working set | 242.8 MiB | 236.6 MiB | -2.6% |
| Settled working set | 10.6 MiB | 8.8 MiB | -17.4% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.311%; candidate 0.561%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
