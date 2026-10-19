# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 160.4 MiB | 124.7 MiB | -22.3% |
| Settled private bytes | 105.4 MiB | 80.2 MiB | -23.9% |
| Peak working set | 234.8 MiB | 203.3 MiB | -13.4% |
| Settled working set | 20.9 MiB | 160.1 MiB | +665.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 1.062%; candidate 0.998%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
