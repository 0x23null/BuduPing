# Native offline memory comparison

Same Windows runner, same harness, GIFs, mood sequence and 20-tap combo; both retain the shipped idle GC/working-set trim. The harness itself forces neither.

| Metric | v0.2.1 | Candidate | Change |
| --- | ---: | ---: | ---: |
| Peak private bytes | 158.2 MiB | 124.3 MiB | -21.4% |
| Settled private bytes | 85.1 MiB | 77.8 MiB | -8.6% |
| Peak working set | 234.0 MiB | 201.0 MiB | -14.1% |
| Settled working set | 20.9 MiB | 19.6 MiB | -6.0% |

Idle CPU (one logical core): baseline 0.559%; candidate 0.935%.

Private bytes and working set are different metrics. This CI result is not a guarantee for a physical PC, GPU, audio device or mixed-DPI setup. Confirm on the user machine with the same --memory-review command.
