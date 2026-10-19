# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 153.0 MiB | 125.3 MiB | -18.1% |
| Settled private bytes | 112.1 MiB | 88.9 MiB | -20.7% |
| Peak working set | 231.2 MiB | 204.1 MiB | -11.7% |
| Settled working set | 19.3 MiB | 11.7 MiB | -39.4% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.437%; candidate 0.248%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
