# Writes the full error and warning code sets that the Python reference gives
# for each mutation fixture and for the two example catalogs.
import json, sys
from pathlib import Path
ref = Path(sys.argv[1]); fixtures = Path(sys.argv[2])
sys.path.insert(0, str(ref))
from validar_catalogo import check, load_fragments
from test_validador import error_codes, warning_codes, EXAMPLES
schema = json.loads((ref / "requirements.schema.json").read_text(encoding="utf-8"))
out = {}
manifest = json.loads((fixtures / "manifest.json").read_text(encoding="utf-8"))
for m in manifest:
    cat = json.loads((fixtures / m["file"]).read_text(encoding="utf-8"))
    r = check(cat, m["sistema"], load_fragments(str(ref / m["example"] / "fragmentos.json")), schema)
    out[m["file"]] = {"valid": r["valid"], "errors": sorted(error_codes(r)), "warnings": sorted(warning_codes(r))}
(fixtures / "expected-codes.json").write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
print(len(out), "ficheros")
