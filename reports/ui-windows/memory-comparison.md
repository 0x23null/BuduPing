# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 155.3 MiB | 142.5 MiB | -8.2% |
| Settled private bytes | 99.9 MiB | 91.1 MiB | -8.8% |
| Peak working set | 233.3 MiB | 221.5 MiB | -5.0% |
| Settled working set | 15.9 MiB | 21.5 MiB | +35.0% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.437%; candidate 0.561%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
