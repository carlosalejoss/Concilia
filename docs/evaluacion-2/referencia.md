# Paso 118: la referencia del autor

Hito 27 (evaluación 2, SOL ‖ Kimi). Hecho por el autor el 26-11-2026, sin coste y sin modelos. Es la «hoja de respuestas correctas» con la que se miden la cobertura del catálogo y del backlog y el trato de las trampas en el paso 120. No depende del modelo B.

## Qué se hizo

**Quién hizo qué.** El autor leyó los 21 documentos de `ejemplos/grande-clinicas-veterinarias/` y los 6 de `iteracion-2/`, sin mirar ningún catálogo, backlog ni pregunta generados por los modelos. Después, a petición del autor, Claude comprobó cada línea contra los documentos originales, extraídos a texto. Tampoco miró ninguna salida de los modelos, y no añadió ni quitó funcionalidades. Claude propuso cuatro retoques y el autor aprobó los cuatro.

**1. La lista de funcionalidades** (`referencia-clinicas.md`): 82 líneas, más de las 40–60 previstas, porque el proyecto es grande. Los criterios de cada agrupación y lo que se dejó como detalle están en las notas del autor.

| | Funcionalidades |
| --- | --- |
| En la lista | 82 |
| Marcadas «(fuera de alcance)»: F35, F53, F63, F64, F65, F68 y F78 | 7 |
| **Cuentan en la cobertura** | **75** |

**2. Las 26 trampas** (G1–G26, `ejemplos/README.md`). El autor corrigió tres filas y Claude propuso cambiar una cuarta:

| Trampa | Antes | Después | Por qué |
| --- | --- | --- | --- |
| G13, fotos en el informe de hospitalización | «Solo en la reunión con veterinarios»; hueco | El protocolo de urgencias y CU-06 las dan por buenas como opcionales; **resuelta** | La tabla original se equivocaba: también están en esos dos documentos (autor) |
| G15, prioridad en la lista de espera | «Solo en el correo de gerencia»; hueco | CU-02 la convierte en huecos preferentes en la app y CU-03 no la menciona; sigue como hueco | Descripción más exacta (autor; «no la menciona», retoque de Claude) |
| G17, funcionar sin conexión | «No»; abierta | **Hueco** | No hay contradicción que preguntar: solo el documento de sistemas lo pide, con claridad, como G23 a G25. El autor ya lo tenía como funcionalidad, F82 (propuesta de Claude, aprobada por el autor) |
| G25, depósito del 50 % al ingresar | «Solo en el protocolo de urgencias»; hueco | También en CU-07 (pago desde la app); sigue como hueco | Descripción completa (autor) |

| Tipo de trampa | Antes | Después | Qué se espera |
| --- | --- | --- | --- |
| Abierta | 5 | 4 | Que se pregunte |
| Resuelta | 14 | 15 | Que no se pregunte y que el catálogo recoja la decisión final |
| Hueco | 6 | 6 | Que esté en el catálogo |
| Fuera de alcance | 1 | 1 | Que no esté en el catálogo |

**3. Los 18 cambios de la segunda iteración** (I2-1 a I2-18). El autor corrigió tres filas, con hallazgos que la tabla original no tenía:
- **I2-1 (teleconsulta):** choca con la videoconsulta, que la especificación deja fuera de alcance.
- **I2-2 (pedidos desde la app):** choca con la tienda en línea, que la especificación deja fuera de alcance.
- **I2-3 (Huellas Puerto):** tendrá peluquería, que hasta ahora era solo de Montaña.

Esta tabla no la usan los scripts del paso 120: es la referencia de las rondas centradas y las actualizaciones.

## Los cuatro retoques de la revisión

1. **G15:** «CU-03 no da prioridad» pasa a «CU-03 no la menciona». CU-03 solo dice que se avisa a recepción si hay alguien compatible en la lista de espera.
2. **F09:** «(prioridad para clientes con plan)» pasa a «(la prioridad para clientes con plan, sin decidir)», para que la lista no contradiga a G15.
3. **Las funcionalidades fuera de alcance no cuentan en la cobertura.** Un catálogo que deja fuera la tienda en línea hace bien, y si esas líneas contaran, saldría castigado por «no cubrir» F63. Siguen en la lista, porque son correctas. Lo más importante de lo que queda fuera ya lo miden las trampas G3, G14 y G26.
   - `un-solo-modelo.cs` las quita de la hoja de cobertura: son las líneas que terminan en «(fuera de alcance)».
   - Es un cambio en un script de evaluación, no en Concilia ni en sus prompts: la versión congelada `evaluacion-2` no cambia.
4. **G17:** de «No» (abierta) a «—» (hueco), por lo que explica la tabla de las trampas.

**Comprobado:** el script lee 82 funcionalidades (75 sin las de fuera de alcance) y 26 trampas (15 resueltas, 4 abiertas, 6 huecos y 1 fuera de alcance), y compila.

## Qué supone para el paso 120

- **Hoja de cobertura:** 75 funcionalidades × catálogo y backlog × SOL solo, Kimi solo y Concilia = **450 filas**. Unas 2–3 horas de revisión del autor.
- **Hoja de trampas de las preguntas:** 19 trampas (abiertas y resueltas) × 3 = 57 filas.
- **Hoja de trampas del catálogo:** 22 trampas (resueltas, huecos y fuera de alcance) × 3 = 66 filas.

## En palabras sencillas

Para saber si un catálogo está completo hace falta una lista de lo que debería tener. La ha hecho el autor a mano, leyendo los documentos del proyecto ficticio, sin ver lo que generan los modelos: si la hubiera hecho una IA, se mediría a los modelos con una regla hecha por otro modelo.

Al repasar las contradicciones puestas a propósito, el autor encontró errores en la tabla original. Claude comprobó después, contra los documentos, que todo lo escrito es cierto y que los scripts lo leen bien. Propuso cuatro retoques, que decidió el autor.

**Qué demuestra:** que la regla con la que se mide la evaluación es fiable y es del autor, y que se puede comprobar línea a línea contra los documentos.
