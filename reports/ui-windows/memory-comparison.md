# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 149.7 MiB | 152.2 MiB | +1.7% |
| Settled private bytes | 98.1 MiB | 84.4 MiB | -14.0% |
| Peak working set | 227.6 MiB | 226.5 MiB | -0.5% |
| Settled working set | 18.6 MiB | 17.7 MiB | -5.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.186%; candidate 0.312%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
