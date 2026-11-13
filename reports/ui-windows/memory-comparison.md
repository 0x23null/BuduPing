# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 156.3 MiB | 134.9 MiB | -13.7% |
| Settled private bytes | 103.8 MiB | 86.8 MiB | -16.4% |
| Peak working set | 231.4 MiB | 208.9 MiB | -9.7% |
| Settled working set | 21.2 MiB | 19.6 MiB | -7.6% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.312%; candidate 0.312%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
