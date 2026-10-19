# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 163.3 MiB | 145.1 MiB | -11.2% |
| Settled private bytes | 94.9 MiB | 88.6 MiB | -6.6% |
| Peak working set | 241.8 MiB | 221.1 MiB | -8.5% |
| Settled working set | 13.3 MiB | 19.6 MiB | +47.8% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.812%; candidate 0.562%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
