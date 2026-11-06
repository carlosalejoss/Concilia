# Paso 124: el backlog en GitHub, prueba real

Prueba del 06-11-2026 con el backlog validado de ReservaSalas del paso 106 (proyecto 41) en un repositorio privado y personal del autor, con GitHub CLI 2.103.3 en Git Bash sobre Windows. El `.zip` se generó con `BacklogGitHub` desde un script, porque la API en Docker seguía con el código anterior: el botón de la web no se ha probado.

## En palabras sencillas

Se comprobó que un backlog generado por Concilia acaba en GitHub con su estructura completa: cada tarea dentro de su historia y cada historia dentro de su épica. Volver a lanzar el script no duplica nada. Si el backlog cambia, crea solo lo nuevo y avisa del resto sin tocarlo, para no pisar lo que se haya hecho en GitHub. Demuestra que el backlog encaja en GitHub sin construir una integración completa: basta con generar un archivo y que el usuario lo lance con su propia cuenta.

## Resultados

| Comprobación | Resultado |
| --- | --- |
| Issues creados | 67 de 67: 6 épicas, 12 historias y 49 tareas |
| Jerarquía, leída de la API de GitHub | las 12 historias bajo su épica y las 49 tareas bajo su historia; ningún issue suelto |
| Tiempo | 318 segundos para 63 issues, unos 5 por issue, sin cortes por los límites de GitHub |
| Repetir el script | 0 creados y 67 sin cambios, en 4 segundos |
| Primero 4 issues y después todos | la segunda ejecución crea los 63 que faltan |
| Texto en español | acentos correctos en títulos y cuerpos |

Con el mismo ritmo, un backlog de unos 300 issues (estimación para clínicas) tardaría unos 25 minutos.

## Segunda iteración

Se hizo con `--simular` y un backlog retocado (una historia modificada, una tarea eliminada y una nueva), para no dejar datos inventados en el repositorio:

| Comprobación | Resultado |
| --- | --- |
| Lo nuevo | detecta `T-050` y solo esa |
| Lo modificado | avisa de `HU-002`, con su comando, sin tocarla |
| Lo eliminado | avisa de `T-012`, con su comando, sin cerrarla |
| Lo demás | 65 sin cambios |
| Comando impreso para `HU-002` | funciona: después, el script deja de avisar de ella. Se deshizo al terminar |
| Otra generación desde cero | se para sin crear nada |

## Límites

- La segunda iteración se simuló: no se creó `T-050` ni se cerró `T-012` en GitHub, aunque crear un issue bajo un padre existente es lo mismo que hizo con los 67.
- Se probó con un proyecto pequeño y en un solo sistema operativo. No se probó en Linux ni en macOS.
- El corte por los límites de GitHub solo se probó con un `gh` simulado: en esta prueba no hubo ninguno.
- El botón de la web falta por verse tras reconstruir Docker, que hace el autor; el endpoint lo cubre `FullFlowTests`.

## Después de la prueba

El mismo 06-11-2026, a petición del autor, Claude borró los 67 issues del repositorio, tras comprobar que todos llevaban la marca de Concilia y que no había ninguno más. Quedan las etiquetas (`epica`, `historia`, `tarea` e `iteracion-1`), que reutiliza cualquier importación nueva, y GitHub no reutiliza los números: el siguiente issue será el 68. Las cifras de arriba quedan como registro de la prueba, porque en el repositorio ya no se pueden ver. Al autor le convence cómo se veían los issues; importará el backlog del proyecto en el que trabaja.
