# Writes, for each mutation fixture, the complete outcome that the Python
# reference (validar_catalogo.py v1.3) gives: lot and repair decision, codes by
# level, rejected elements, the accepted catalog, the rendered texts, the id
# map and the metrics. The C# port must give the same (CatalogValidatorTests).
#
# Usage, from the repository root:
#   python tests/fixtures/catalog-mutations/export_outcomes.py prompts/generacion_requisitos tests/fixtures/catalog-mutations
import json
import sys
from pathlib import Path

ref = Path(sys.argv[1])
fixtures = Path(sys.argv[2])
sys.path.insert(0, str(ref))
from validar_catalogo import SCHEMA_FILE, check_text, load_fragments  # noqa: E402
from test_validador import lot_codes, repair_codes, warning_codes  # noqa: E402

schema = json.loads((ref / SCHEMA_FILE).read_text(encoding="utf-8"))
out = {}
for m in json.loads((fixtures / "manifest.json").read_text(encoding="utf-8")):
    raw = (fixtures / m["file"]).read_text(encoding="utf-8")
    r = check_text(raw, m["sistema"], load_fragments(str(ref / m["example"] / "fragmentos.json")), schema)
    out[m["file"]] = {
        "valid": r["valid"],
        "needs_repair": r["needs_repair"],
        "lot": sorted(lot_codes(r)),
        "rejected": {x["id"]: sorted({e.split()[0] for e in x["errors"]}) for x in r["rejected"]},
        "warnings": sorted(warning_codes(r)),
        "repair": sorted(repair_codes(r)),
        "catalog": r["catalog"],
        "texts": r["texts"],
        "ids": r["ids"],
        "metrics": r["metrics"],
    }
(fixtures / "expected-outcomes.json").write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n",
                                                 encoding="utf-8", newline="\n")
print(len(out), "ficheros")
