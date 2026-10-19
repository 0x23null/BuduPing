# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 163.2 MiB | 136.2 MiB | -16.5% |
| Settled private bytes | 116.7 MiB | 102.1 MiB | -12.5% |
| Peak working set | 236.0 MiB | 213.5 MiB | -9.5% |
| Settled working set | 11.1 MiB | 12.8 MiB | +15.4% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.312%; candidate 0.437%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
