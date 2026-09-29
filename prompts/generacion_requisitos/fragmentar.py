#!/usr/bin/env python3
"""
Fragmentador de referencia para el prompt de requisitos v1.1.

Divide cada documento de entrada en fragmentos (frases) con un id estable, y las dudas
resueltas en un fragmento por duda. El modelo cita fragmentos por su id y el validador
comprueba que cada cita es literal dentro de su fragmento y que ningún fragmento del
documento queda sin tratar (C10).

Es la implementación de referencia: la versión en C# del runner debe producir exactamente
la misma salida para los mismos ficheros (los tests comparan con ejemplo/fragmentos.json).

Uso:
    python fragmentar.py --docs DIR [--dudas dudas.json] --salida fragmentos.json
                         [--bloque-doc documentacion.xml] [--bloque-dudas dudas.xml]

--docs          carpeta con un .txt o .md por documento; el nombre sin extensión es su id (DOC-01).
--dudas         JSON con una lista [{"id": "D-001", "pregunta": "...", "respuesta": "..."}].
--salida        fragmentos.json (lo lee validar_catalogo.py).
--bloque-doc    texto que sustituye a {{DOCUMENTACION}} en el prompt.
--bloque-dudas  texto que sustituye a {{DUDAS_RESUELTAS}} en el prompt.
"""
import argparse
import json
import re
import unicodedata
from pathlib import Path

# Etiqueta de sección al principio de una línea: "§2.1", "2.1", "2.1.", "3." (seguida de espacio).
SECTION_RE = re.compile(r"^(§\s?\d+(?:\.\d+)*\.?|\d+(?:\.\d+)+\.?|\d+\.)\s+")
MD_HEADING_RE = re.compile(r"^#{1,6}\s+(.*)$")
BULLET_RE = re.compile(r"^([-*•·]|[a-z]\)|\(?[ivx]+\))\s+", re.IGNORECASE)

# Corte de frase: signo de cierre + espacio + inicio de frase en mayúscula, dígito o apertura.
SENTENCE_SPLIT_RE = re.compile(r"(?<=[.!?…])\s+(?=[«\"“(¿¡]?[A-ZÁÉÍÓÚÑÜ0-9])")

# Abreviaturas tras las que no se corta: se compara la última palabra del trozo anterior.
# "etc." no está: seguida de mayúscula suele cerrar frase.
ABBREVIATIONS = {
    "p.", "ej.", "sr.", "sra.", "srta.", "dr.", "dra.", "núm.", "nº.", "art.", "arts.", "pág.", "págs.",
    "cap.", "aprox.", "vol.", "fig.", "tel.", "ee.", "ud.", "uds.", "máx.", "mín.", "e.g.", "i.e.", "vs.",
    "approx.", "max.", "min.",
}
INITIAL_RE = re.compile(r"^[A-ZÁÉÍÓÚÑ]\.$")


def norm(s: str) -> str:
    """NFC y espacios colapsados. El texto de un fragmento se guarda ya normalizado."""
    return re.sub(r"\s+", " ", unicodedata.normalize("NFC", s)).strip()


def safe_text(s: str) -> str:
    """Evita que el texto cierre las etiquetas del prompt. Se aplica igual al JSON y al prompt,
    así que las citas siguen siendo literales respecto a lo que el modelo ve."""
    return s.replace("<", "＜").replace(">", "＞")


def safe_attr(s: str) -> str:
    return safe_text(s).replace('"', "'")


def split_sentences(paragraph: str) -> list[str]:
    parts = SENTENCE_SPLIT_RE.split(paragraph)
    out: list[str] = []
    for p in parts:
        last = out[-1].split()[-1] if out else ""
        if out and (last.lower() in ABBREVIATIONS or INITIAL_RE.match(last)):
            out[-1] = f"{out[-1]} {p}"
        else:
            out.append(p)
    return [norm(p) for p in out if norm(p)]


def paragraphs(text: str):
    """Genera (sección, párrafo, es_título). Una línea con etiqueta de sección, un título markdown o una viñeta
    abre párrafo nuevo; las líneas que continúan un párrafo (texto extraído de PDF) se unen."""
    section = ""
    current: list[str] = []
    current_section = ""

    def flush():
        nonlocal current
        if current:
            yield current_section, norm(" ".join(current)), False
            current = []

    for raw in text.splitlines():
        line = raw.strip()
        if not line:
            yield from flush()
            continue
        m_head = MD_HEADING_RE.match(line)
        m_sec = SECTION_RE.match(line)
        if m_head:
            yield from flush()
            section = norm(m_head.group(1))
            current_section = section
            yield section, section, True  # un título es un único fragmento
            continue
        if m_sec:
            yield from flush()
            section = m_sec.group(1).replace(" ", "")
            current_section = section
            rest = line[m_sec.end():]
            current = [rest] if rest else []
            continue
        m_bullet = BULLET_RE.match(line)
        if m_bullet:
            yield from flush()
            current_section = section
            current = [line[m_bullet.end():]]  # el marcador de viñeta no forma parte del texto
            continue
        if not current:
            current_section = section
        current.append(line)
    yield from flush()


def fragment_document(doc_id: str, text: str) -> list[dict]:
    frags = []
    n = 0
    for section, par, is_heading in paragraphs(text):
        for sentence in ([norm(par)] if is_heading else split_sentences(par)):
            n += 1
            frags.append({"id": f"{doc_id}#F{n:03d}", "source": doc_id, "kind": "document",
                          "section": safe_attr(section), "text": safe_text(sentence)})
    return frags


def fragment_dudas(dudas: list[dict]) -> list[dict]:
    return [{"id": f"DUDAS#{d['id']}", "source": "DUDAS", "kind": "clarification",
             "section": safe_attr(norm(d.get("pregunta", ""))), "text": safe_text(norm(d["respuesta"]))}
            for d in dudas]


def doc_block(frags: list[dict], names: dict[str, str]) -> str:
    out, current = [], None
    for f in frags:
        if f["kind"] != "document":
            continue
        if f["source"] != current:
            if current is not None:
                out.append("</documento>")
            current = f["source"]
            out.append(f'<documento id="{current}" nombre="{safe_attr(names.get(current, current))}">')
        out.append(f'<f id="{f["id"]}" sec="{f["section"]}">{f["text"]}</f>')
    if current is not None:
        out.append("</documento>")
    return "\n".join(out)


def dudas_block(frags: list[dict]) -> str:
    return "\n".join(f'<f id="{f["id"]}" pregunta="{f["section"]}">{f["text"]}</f>'
                     for f in frags if f["kind"] == "clarification")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--docs", required=True)
    ap.add_argument("--dudas")
    ap.add_argument("--salida", required=True)
    ap.add_argument("--bloque-doc")
    ap.add_argument("--bloque-dudas")
    a = ap.parse_args()

    frags, names = [], {}
    for p in sorted(Path(a.docs).iterdir()):
        if p.is_file() and p.suffix.lower() in (".txt", ".md"):
            names[p.stem] = p.name
            frags += fragment_document(p.stem, p.read_text(encoding="utf-8"))
    if a.dudas:
        frags += fragment_dudas(json.loads(Path(a.dudas).read_text(encoding="utf-8")))

    Path(a.salida).write_text(json.dumps({"version": "1.1", "fragments": frags}, ensure_ascii=False, indent=2) + "\n",
                              encoding="utf-8")
    if a.bloque_doc:
        Path(a.bloque_doc).write_text(doc_block(frags, names) + "\n", encoding="utf-8")
    if a.bloque_dudas:
        Path(a.bloque_dudas).write_text(dudas_block(frags) + "\n", encoding="utf-8")
    print(f"{len(frags)} fragmentos")


if __name__ == "__main__":
    main()
