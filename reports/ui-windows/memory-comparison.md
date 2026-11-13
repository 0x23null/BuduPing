# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 158.1 MiB | 119.3 MiB | -24.6% |
| Settled private bytes | 106.9 MiB | 73.1 MiB | -31.7% |
| Peak working set | 234.1 MiB | 194.8 MiB | -16.8% |
| Settled working set | 20.4 MiB | 16.7 MiB | -18.1% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.312%; candidate 0.312%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
