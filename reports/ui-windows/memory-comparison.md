# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 161.1 MiB | 127.5 MiB | -20.8% |
| Settled private bytes | 115.7 MiB | 94.9 MiB | -18.0% |
| Peak working set | 236.5 MiB | 204.4 MiB | -13.6% |
| Settled working set | 22.1 MiB | 16.6 MiB | -24.8% |

Idle CPU (one logical core, periodic sampler stopped): baseline 1.437%; candidate 0.812%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
