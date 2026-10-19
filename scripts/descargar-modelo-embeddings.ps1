# Descarga el modelo de embeddings (hito 17) en models/, para ejecutar la API
# y los tests en local. La imagen de Docker lo descarga por su cuenta.
#
# Modelo: sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
# (Apache 2.0), exportado a ONNX en fp32 por sus autores. Se fija el commit de
# Hugging Face y se comprueba el SHA-256 de cada fichero, los mismos que en
# src/Concilia.Api/Dockerfile.
#
# Uso, desde la raíz del repositorio: .\scripts\descargar-modelo-embeddings.ps1

$ErrorActionPreference = 'Stop'

$revision = 'e8f8c211226b894fcb81acc59f3b34ba3efd5f42'
$base = "https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/resolve/$revision"
$folder = Join-Path $PSScriptRoot '..\models\paraphrase-multilingual-MiniLM-L12-v2'

$files = @(
    @{ Name = 'model.onnx'; Source = 'onnx/model.onnx'; Sha256 = '10f7a088420252b26caf819236ca2c9d2987afd0fc06fec7553b542a5655a05a' },
    @{ Name = 'sentencepiece.bpe.model'; Source = 'sentencepiece.bpe.model'; Sha256 = 'cfc8146abe2a0488e9e2a0c56de7952f7c11ab059eca145a0a727afce0db2865' }
)

New-Item -ItemType Directory -Force $folder | Out-Null

foreach ($file in $files) {
    $path = Join-Path $folder $file.Name
    if ((Test-Path $path) -and ((Get-FileHash $path -Algorithm SHA256).Hash -eq $file.Sha256)) {
        Write-Host "$($file.Name): ya está descargado."
        continue
    }
    Write-Host "$($file.Name): descargando..."
    # Sin la barra de progreso, Invoke-WebRequest es mucho más rápido con un fichero de 470 MB.
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri "$base/$($file.Source)" -OutFile $path
    $hash = (Get-FileHash $path -Algorithm SHA256).Hash
    if ($hash -ne $file.Sha256) {
        Remove-Item $path
        throw "$($file.Name): el SHA-256 no coincide ($hash). Se ha borrado el fichero."
    }
    Write-Host "$($file.Name): descargado y comprobado."
}

Write-Host "Modelo listo en $((Resolve-Path $folder).Path)"
