# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 158.3 MiB | 134.7 MiB | -14.9% |
| Settled private bytes | 103.0 MiB | 100.1 MiB | -2.9% |
| Peak working set | 236.0 MiB | 214.4 MiB | -9.2% |
| Settled working set | 17.7 MiB | 10.4 MiB | -41.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.500%; candidate 0.740%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
