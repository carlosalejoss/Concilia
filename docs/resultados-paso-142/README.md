# Prueba real del paso 142 (en curso)

Prueba del script `importar.sh` del backlog en GitHub, con los tipos de issue de una organización, el tablero de GitHub Projects y los comentarios de una iteración (nivel 3). Empezada el 13-11-2026 por Claude, con lo que preparó el autor; **sin terminar**: el autor tuvo que parar.

## En palabras sencillas

Se subió de verdad a GitHub el backlog de un proyecto de prueba, primero en una organización y luego en una cuenta personal, y después se subió una segunda versión con una historia cambiada, otra eliminada y una nueva. La prueba encontró tres fallos que los tests no podían ver, porque en ellos GitHub se imita con un programa falso. Los tres están arreglados, y cada uno tiene ahora su test.

## Qué se usó

- **Lo preparado por el autor:** una organización gratuita con un repositorio privado y vacío y el tipo de issue «Epic» creado (la organización trae Task, Bug y Feature), otro repositorio privado y vacío en su cuenta personal, y `gh` 2.103.3 con el permiso `project`.
- **El backlog:** el del proyecto «ReservaSalas analista» (paso 106), el más pequeño: 6 épicas, 12 historias y 49 tareas, 67 issues. El primer `.zip` se descargó de la API (`GET /projects/41/backlog-github.zip`, generación `4edfe632`).
- **La segunda iteración, simulada:** ningún proyecto tiene un backlog con historias modificadas por una actualización (el de clínicas v0.11 tiene dos iteraciones, pero sin historial de cambios, y es demasiado grande para los límites de GitHub). La genera [`iteracion2.cs`](iteracion2.cs) con el exportador de la plataforma, a partir del backlog real del proyecto 41 y con la misma generación, sin tocar el proyecto: HU-004 modificada (un requisito nuevo, RF-024, y un criterio más, con su motivo), HU-008 eliminada con su motivo, y HU-013 nueva con una tarea (RF-025). Escribe también el `.zip` de la primera iteración con el script actual (mismos issues y mismos hashes que el de la API).

  ```bash
  curl -s -o backlog-raw.json http://127.0.0.1:8080/projects/41/deliverables/backlog
  # backlog41.json = el campo content de backlog-raw.json, en UTF-8
  dotnet run docs/resultados-paso-142/iteracion2.cs -- <carpeta con backlog41.json> 4edfe632
  ```

## Resultados hasta ahora

**Organización** (épica → Epic, historia → Feature, tarea → Task), terminado:

| Ejecución | Resultado |
| --- | --- |
| Primera iteración | 67 issues: 6 Epic, 12 Feature y 49 Task, sin etiquetas de nivel; solo las 6 épicas sin padre. Tablero «Backlog de ReservaSalas analista» con los 67, en *Todo*, con su épica y sus iteraciones. Hicieron falta varios intentos por los cortes de red (fallo 1 y 2) |
| Repetición | Creados 0, sin cambios 67, añadidos al tablero 0 |
| Segunda iteración | Creados 2 (HU-013 Feature y T-050 Task, con `iteracion-2`); 1 modificado y 4 eliminados (HU-008 y sus 3 tareas), cada uno con su comando, y 5 comentarios: los de HU-004 y HU-008 con el motivo de la iteración. En el tablero, 69 elementos, y EP-02, HU-004, T-013, T-014 y EP-04 pasan a «1, 2» |
| Repetición | Ningún comentario nuevo; ningún fichero temporal suelto |

**Cuenta personal** (etiquetas): parada a mitad de la primera iteración, con 49 de los 67 issues creados. Falta relanzarla (sigue por donde iba), repetirla, la segunda iteración y su repetición.

## Los fallos que encontró

1. **Un corte de red al preguntar si el propietario es una organización** hacía que el script la tomara por una cuenta personal: creó T-014 con la etiqueta `tarea` en lugar del tipo Task. La consulta iba dentro de `[[ "$(…)" == … ]]`, donde un fallo no para el script aunque tenga `set -e`, y la de los tipos llevaba `|| true`. Ahora las dos van en una asignación propia y, si fallan, el script se para con su mensaje de «vuelve a ejecutarlo». El issue mal creado se borró y el script lo creó de nuevo con su tipo.
2. **Issues en el tablero sin épica ni iteraciones:** 7, y luego T-014. GitHub añade solo al tablero los sub-issues de un issue que ya está en él (un flujo automático de GitHub Projects), y el script solo rellenaba los campos de los que añadía él, así que repetirlo no los completaba. Ahora, en cada ejecución, escribe la épica y las iteraciones de todo issue nuevo en el tablero o cuyas iteraciones no son las de Concilia (así también se ve una iteración nueva), y pone *Todo* solo donde no hay estado, para respetar el que le haya dado el equipo. El resumen dice cuántos ha completado.
3. **Los comentarios no se podían publicar desde Git Bash:** el texto iba a un fichero de `mktemp`, en `/tmp/…`, una ruta que `gh.exe`, un programa de Windows, no encuentra. Ahora el fichero se crea en la carpeta del script, con ruta relativa. El `gh` falso de los tests rechaza desde ahora cualquier ruta absoluta.

Dos detalles más: el script escribía «Cada epica» (ahora «épica») y el comentario ponía dos puntos seguidos cuando el motivo acaba en punto («…reserva.).»).

**Tests:** `GitHubScriptTests` gana 5 casos (los dos cortes de red, el tablero sin campos con y sin estado, y la ruta relativa en el de los comentarios); cada uno falla sin su arreglo. 1.083 en verde.

**La red:** durante la prueba se cortó varias veces la conexión con `api.github.com` (y, al principio, con el registro de imágenes de Docker), sin relación con el script.

## Pendiente

- Terminar la cuenta personal: relanzar la primera iteración, repetirla, la segunda iteración y su repetición.
- Reconstruir la imagen de la API y comprobar que el `.zip` de la web lleva los arreglos.
- Borrar los issues, comentarios y tableros de prueba (o que el autor borre los repositorios y la organización).
- Completar este documento, el plan, la trazabilidad y la memoria.
