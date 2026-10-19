# Calibración de los umbrales: resultados y decisión (paso 87, 19-10-2026)

Resumen de [`resultados.md`](resultados.md), que genera `scripts/calibracion-embeddings/calibrar.cs`, y de la decisión del autor. Material para la memoria.

## Decisión del autor (19-10-2026)

**«Lo mismo» desde 0,90 y «parecido» desde 0,70.** Por debajo de 0,70, «distinto»: el elemento solo está en su propuesta.

- La calibración proponía 0,90 y 0,71: los centros de los intervalos con más aciertos, 0,89–0,92 y 0,70–0,72.
- Se elige 0,70 porque está dentro del intervalo y es más fácil de explicar. En la muestra no hay ningún par entre 0,70 y 0,71, así que los resultados no cambian.
- Están en la configuración de la API: `Embeddings:SameThreshold` y `Embeddings:SimilarThreshold`.

## En palabras sencillas

- **Qué se calibra.** La plataforma compara por su cuenta lo que proponen los dos modelos, con un número de 0 a 1 que mide cuánto se parecen dos textos. Había que decidir desde qué número dos textos «dicen lo mismo» y desde cuál «se parecen».
- **Cómo se hizo.**
  - El autor clasificó a mano 48 parejas de textos de los proyectos ficticios como «mismo», «parecido» o «distinto», sin ver el número ni lo que hizo el árbitro.
  - Se probaron todos los umbrales posibles y se eligieron los que más coinciden con su criterio.
  - Después se comprobó que esos umbrales coinciden con lo que deciden casi todos los árbitros en el proyecto de clínicas, que tiene varios arbitrajes de las mismas propuestas.
- **Por qué no se calibró con el árbitro.** Los embeddings sirven para comprobar al árbitro. Calibrarlos con él sería circular, como hacer la plantilla de un examen con las respuestas del alumno al que se va a corregir.

## Resultados

| Comparación | Resultado |
| --- | --- |
| Embeddings frente al autor, con las tres etiquetas | 37 de 48 pares (77 %): preguntas 17 de 18, historias 12 de 16 y requisitos 8 de 14 |
| «Mismo» frente al resto | 85 % de aciertos con 0,90 |
| «Distinto» frente al resto | 88 % de aciertos con 0,70 |
| Lo que juntan casi todos los árbitros (≥ 80 %), visto por los embeddings como «mismo o parecido» | 99 % en el catálogo (248 pares) y 82 % en las preguntas (11 pares) |
| Lo que casi ningún árbitro junta (≤ 20 %), visto por los embeddings como «distinto» | 90 % en las preguntas; 11 % en el catálogo, donde casi todo cae en la franja «parecido» |
| El árbitro frente al autor | Juntó 9 de 9 «mismo», 12 de 17 «parecido» y 2 de 22 «distinto» |
| Acuerdo por elemento con el árbitro, en los 9 trabajos originales | 89 % |

## Qué enseñan los datos

1. **El árbitro también junta lo «parecido»,** conservando el detalle de cada uno. Por eso, para comparar con él, un elemento cuenta como presente en la otra propuesta si su par es «mismo» o «parecido». Es un cambio de criterio respecto a la primera versión del emparejamiento, que solo contaba «mismo».
2. **La franja «parecido» (0,70–0,90) es la dudosa:** en el catálogo, los árbitros a veces juntan esos pares y a veces no. Es la candidata a «discrepancia grave» del paso 88.
3. **El árbitro decide bien qué es lo mismo:** no se dejó ningún par que el autor ve igual y casi no juntó los que ve distintos. Es la primera medida del árbitro frente a un criterio humano.
4. **Dónde fallan los embeddings:**
   - textos que comparten casi todas las palabras pero dicen otra cosa (dos requisitos de integración con LabVet, similitud de 0,91, que el autor vio distintos);
   - lo mismo dicho con estilos muy diferentes (una frase completa frente a otra telegráfica, 0,69, que el autor vio iguales).
   Los requisitos son la fase más difícil.

## Límites

- **Muestra pequeña:** 48 pares, suficientes para elegir los umbrales pero no para afinar más.
- **Un solo etiquetador:** el propio autor.
- **Consenso de árbitros solo en un proyecto** (clínicas) y en dos fases (preguntas y catálogo).
- **Solo pares candidatos:** se miran los pares en que un elemento es el más parecido del otro.
