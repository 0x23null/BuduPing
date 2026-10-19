"""Inject only the common offline measurement harness into an untouched v0.2.1 checkout."""
from pathlib import Path
import shutil
import sys

root = Path(__file__).resolve().parents[1]
baseline = Path(sys.argv[1]).resolve()
project = baseline / 'windows/BuduPing'
shutil.copyfile(root / 'windows/BuduPing/App.MemoryReview.cs', project / 'App.MemoryReview.cs')
app = project / 'App.xaml.cs'
source = app.read_text(encoding='utf-8-sig')
anchor = '        base.OnStartup(e);'
if source.count(anchor) != 1:
    raise SystemExit('Baseline startup changed; inspect before injecting the measurement hook')
source = source.replace(anchor, anchor + '\n        if (e.Args.Length == 2 && e.Args[0] == "--memory-review") { StartMemoryReview(e.Args[1]); return; }', 1)
app.write_text(source, encoding='utf-8')
print('Prepared v0.2.1 with the identical offline workload; application/rendering code is unchanged.')
