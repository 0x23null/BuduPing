# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 155.2 MiB | 142.1 MiB | -8.5% |
| Settled private bytes | 126.5 MiB | 96.6 MiB | -23.6% |
| Peak working set | 234.2 MiB | 219.5 MiB | -6.3% |
| Settled working set | 17.5 MiB | 24.0 MiB | +37.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 1.187%; candidate 1.685%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
