# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 178.4 MiB | 120.4 MiB | -32.5% |
| Settled private bytes | 102.2 MiB | 85.0 MiB | -16.8% |
| Peak working set | 255.4 MiB | 199.0 MiB | -22.1% |
| Settled working set | 18.1 MiB | 10.4 MiB | -42.6% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.680%; candidate 0.684%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
