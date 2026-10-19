# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 153.7 MiB | 155.3 MiB | +1.0% |
| Settled private bytes | 101.1 MiB | 84.1 MiB | -16.8% |
| Peak working set | 230.7 MiB | 234.4 MiB | +1.6% |
| Settled working set | 21.0 MiB | 21.0 MiB | -0.1% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.811%; candidate 0.936%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
