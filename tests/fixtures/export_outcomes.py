# Writes, for each mutation fixture, the complete outcome that the Python
# reference gives, in expected-outcomes.json next to the fixtures. The C# port
# must give the same:
# - catalog-mutations (validar_catalogo.py, CatalogValidatorTests): lot and
#   repair decision, codes by level, rejected elements, the accepted catalog,
#   the rendered texts, the id map and the metrics.
# - inventory-mutations (validar_inventario.py, InventoryValidatorTests): the
#   same, with the normalized inventory instead of the catalog.
#
# Usage, from the repository root, after test_validador.py --exportar tests/fixtures:
#   python tests/fixtures/export_outcomes.py prompts/generacion_requisitos tests/fixtures
import json
import sys
from pathlib import Path

ref = Path(sys.argv[1])
root = Path(sys.argv[2])
sys.path.insert(0, str(ref))
import validar_catalogo  # noqa: E402
import validar_inventario  # noqa: E402
from test_validador import lot_codes, repair_codes, warning_codes  # noqa: E402


def load_schema(name):
    return json.loads((ref / name).read_text(encoding="utf-8"))


def codes(r):
    return {
        "valid": r["valid"],
        "needs_repair": r["needs_repair"],
        "lot": sorted(lot_codes(r)),
        "rejected": {x["id"]: sorted({e.split()[0] for e in x["errors"]}) for x in r["rejected"]},
        "warnings": sorted(warning_codes(r)),
        "repair": sorted(repair_codes(r)),
    }


def catalog_outcome(m, raw, schema):
    fragments = validar_catalogo.load_fragments(str(ref / m["example"] / "fragmentos.json"))
    r = validar_catalogo.check_text(raw, m["sistema"], fragments, schema)
    return {**codes(r), "catalog": r["catalog"], "texts": r["texts"], "ids": r["ids"], "metrics": r["metrics"]}


def inventory_outcome(m, raw, schema):
    fragments = validar_catalogo.load_fragments(str(ref / m["example"] / m["fragments"]))
    r = validar_inventario.check_text(raw, fragments, schema)
    return {**codes(r), "inventory": r["inventory"], "ids": r["ids"], "metrics": r["metrics"]}


SUITES = [
    ("catalog-mutations", catalog_outcome, load_schema(validar_catalogo.SCHEMA_FILE)),
    ("inventory-mutations", inventory_outcome, load_schema(validar_inventario.SCHEMA_FILE)),
]
for folder, outcome, schema in SUITES:
    fixtures = root / folder
    out = {}
    for m in json.loads((fixtures / "manifest.json").read_text(encoding="utf-8")):
        out[m["file"]] = outcome(m, (fixtures / m["file"]).read_text(encoding="utf-8"), schema)
    (fixtures / "expected-outcomes.json").write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n",
                                                     encoding="utf-8", newline="\n")
    print(folder, len(out), "ficheros")
