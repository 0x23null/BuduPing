# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 163.2 MiB | 120.4 MiB | -26.2% |
| Settled private bytes | 109.7 MiB | 77.7 MiB | -29.2% |
| Peak working set | 237.5 MiB | 197.8 MiB | -16.7% |
| Settled working set | 20.7 MiB | 20.8 MiB | +0.5% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.562%; candidate 0.750%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
