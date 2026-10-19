# Native offline memory comparison

Same Windows runner, same harness, GIFs, mood sequence and 20-tap combo; both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 149.4 MiB | 132.3 MiB | -11.5% |
| Settled private bytes | 78.9 MiB | 93.3 MiB | +18.3% |
| Peak working set | 227.1 MiB | 204.1 MiB | -10.2% |
| Settled working set | 21.7 MiB | 10.9 MiB | -49.6% |

Idle CPU (one logical core): baseline 0.999%; candidate 1.311%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
