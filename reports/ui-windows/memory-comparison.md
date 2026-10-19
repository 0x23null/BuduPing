# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 159.2 MiB | 156.6 MiB | -1.6% |
| Settled private bytes | 99.9 MiB | 89.6 MiB | -10.3% |
| Peak working set | 236.0 MiB | 234.4 MiB | -0.7% |
| Settled working set | 20.5 MiB | 19.5 MiB | -4.6% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.616%; candidate 0.434%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
