# Native offline memory comparison

Same Windows runner and harness, fixed six-mood sequence and 20-tap combo (runtime variant rotation can select different GIFs); both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 179.3 MiB | 121.5 MiB | -32.2% |
| Settled private bytes | 103.7 MiB | 82.4 MiB | -20.5% |
| Peak working set | 256.0 MiB | 198.6 MiB | -22.4% |
| Settled working set | 22.1 MiB | 10.9 MiB | -50.3% |

Idle CPU (one logical core, periodic sampler stopped): baseline 0.562%; candidate 0.312%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
