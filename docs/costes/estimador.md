# Estimador del consumo de las ejecuciones sin dato (paso 95)

Generado por `scripts/evaluacion/ajustar-estimador.cs` el 19-10-2026 12:53 con 125 intentos que tienen consumo real y consola guardada.

| Grupo | Intentos | Entrada = a·turnos + b·caracteres | Salida = c·turnos + d·caracteres escritos | Error por ejecución, mediana (entrada / salida) | Error del total (entrada / salida) |
| --- | --- | --- | --- | --- | --- |
| Fases | 28 | a = 8.587, b = 1 / 4,42 | c = 871, d = 0,320 | 9 % / 20 % | -3,5 % / -5,1 % |
| Mockup | 97 | a = 1.636, b = 1 / 3,30 | c = 1.395, d = 0,387 | 5 % / 9 % | -0,7 % / -1,0 % |

Los coeficientes se copian en `src/Concilia.Core/Runs/UsageEstimate.cs` (`UsageEstimator.Mockup` y `UsageEstimator.Phases`).
