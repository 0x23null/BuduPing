# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 154.9 MiB | 124.1 MiB | -19.9% |
| Settled private bytes | 101.3 MiB | 90.8 MiB | -10.4% |
| Peak working set | 229.5 MiB | 201.1 MiB | -12.4% |
| Settled working set | 20.6 MiB | 10.6 MiB | -48.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.623%; candidate 0.750%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
