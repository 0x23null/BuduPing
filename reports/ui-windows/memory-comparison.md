# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 155.6 MiB | 186.2 MiB | +19.7% |
| Settled private bytes | 112.4 MiB | 136.5 MiB | +21.5% |
| Peak working set | 231.9 MiB | 295.4 MiB | +27.4% |
| Settled working set | 10.2 MiB | 19.4 MiB | +90.5% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.437%; candidate 0.187%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
