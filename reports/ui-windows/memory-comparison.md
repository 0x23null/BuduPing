# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 158.2 MiB | 136.6 MiB | -13.7% |
| Settled private bytes | 114.3 MiB | 98.1 MiB | -14.2% |
| Peak working set | 233.5 MiB | 210.1 MiB | -10.0% |
| Settled working set | 10.4 MiB | 11.2 MiB | +8.0% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.437%; candidate 0.500%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
