# ============================================================
# RESETEAR MIGRACIONES Y BASE DE DATOS EF CORE (.NET)
# Autor: Paul Mamani
# Proyecto: RoboticsFixture
# ============================================================

Write-Host "🔹 Iniciando reinicio de base y migraciones..." -ForegroundColor Cyan

# 1️⃣ Eliminar la base de datos actual
Write-Host "🧱 Eliminando base de datos actual..."
dotnet ef database drop --force

# 2️⃣ Borrar carpeta de migraciones si existe
$MigrationsPath = "Migrations"
if (Test-Path $MigrationsPath) {
    Write-Host "🗑️ Eliminando carpeta de migraciones..."
    Remove-Item $MigrationsPath -Recurse -Force
} else {
    Write-Host "✅ No hay carpeta de migraciones existente."
}

# 3️⃣ Crear nueva migración inicial
Write-Host "📦 Creando nueva migración 'InitialCreate'..."
dotnet ef migrations add InitialCreate

# 4️⃣ Aplicar la migración a la base de datos
Write-Host "🚀 Aplicando migración a la base de datos..."
dotnet ef database update

Write-Host "✅ Reinicio completado con éxito."
Write-Host "------------------------------------------"
Write-Host "✔ Nueva base sincronizada con los modelos."
Write-Host "✔ Migración inicial creada correctamente."
Write-Host "------------------------------------------"
