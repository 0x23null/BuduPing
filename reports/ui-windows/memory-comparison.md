# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 161.3 MiB | 130.4 MiB | -19.1% |
| Settled private bytes | 100.0 MiB | 96.7 MiB | -3.3% |
| Peak working set | 237.6 MiB | 206.9 MiB | -12.9% |
| Settled working set | 20.4 MiB | 16.5 MiB | -19.4% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.437%; candidate 0.999%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
