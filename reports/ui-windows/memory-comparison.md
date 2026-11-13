# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 159.5 MiB | 137.1 MiB | -14.0% |
| Settled private bytes | 113.3 MiB | 89.9 MiB | -20.7% |
| Peak working set | 236.1 MiB | 209.4 MiB | -11.3% |
| Settled working set | 11.2 MiB | 8.5 MiB | -23.9% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.312%; candidate 0.185%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
