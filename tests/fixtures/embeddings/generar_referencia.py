"""Genera referencia.json: los ids de tokens y los vectores de unos textos de
prueba calculados con las bibliotecas originales de Python, para comprobar que
el servicio de embeddings en C# (src/Concilia.Core/Embeddings) da lo mismo.

- Ids de tokens: el tokenizador de Hugging Face (transformers).
- Vectores: sentence-transformers con los pesos originales del modelo, es
  decir, el resultado «oficial» de paraphrase-multilingual-MiniLM-L12-v2.

Solo hace falta volver a ejecutarlo si cambian los textos o el modelo.
Necesita: pip install sentence-transformers

Uso, desde la raíz del repositorio:
    python tests/fixtures/embeddings/generar_referencia.py
"""

import json
from pathlib import Path

from sentence_transformers import SentenceTransformer

MODELO = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
# El mismo commit que el Dockerfile y scripts/descargar-modelo-embeddings.ps1.
REVISION = "e8f8c211226b894fcb81acc59f3b34ba3efd5f42"

TEXTOS = [
    # Requisitos y frases del dominio, en español y en inglés.
    "El sistema permitirá reservar una sala de reuniones.",
    "The system shall allow users to book a meeting room.",
    "El usuario puede cancelar una reserva hasta una hora antes.",
    "Cuando se confirme una reserva, el sistema enviará un correo al organizador.",
    "When a booking is confirmed, the system shall email the organizer.",
    "El informe mensual de ocupación se exporta en PDF.",
    "El tiempo de respuesta de la búsqueda será inferior a 2 segundos.",
    # Casos difíciles para el tokenizador: espacios, tildes, mayúsculas,
    # caracteres de ancho completo, símbolos, emojis e ids de la plataforma.
    "  espacios   múltiples\ty\ttabulaciones\n y saltos de línea  ",
    "ÁÉÍÓÚ ñandú Über façade CAMIÓN",
    "ＦＵＬＬ　ｗｉｄｔｈ ① ﬁ ²",
    "Precio ≥ 10 € y ≤ 20 $ — «comillas» 😀",
    "RF-001 [ERROR] DOC-01#F003: P-012",
    "",
    # Más largo que los 128 tokens del modelo: se corta por el final.
    " ".join(["La plataforma registrará cada cambio del catálogo con su fecha y su autor."] * 12),
]


def main() -> None:
    modelo = SentenceTransformer(MODELO, revision=REVISION, device="cpu")
    vectores = modelo.encode(TEXTOS, batch_size=32, convert_to_numpy=True)
    casos = []
    for texto, vector in zip(TEXTOS, vectores):
        ids = modelo.tokenizer(texto, truncation=True, max_length=modelo.max_seq_length)["input_ids"]
        casos.append({"texto": texto, "ids": ids, "vector": [round(float(v), 6) for v in vector]})

    salida = Path(__file__).with_name("referencia.json")
    salida.write_text(
        json.dumps({"modelo": MODELO, "revision": REVISION, "casos": casos}, ensure_ascii=False, indent=1),
        encoding="utf-8",
    )
    print(f"{len(casos)} casos escritos en {salida}")


if __name__ == "__main__":
    main()
