# 🤖 Robotics Fixture System

Un sistema avanzado de **fixture para torneos de robótica** con soporte para **dos modos de combate oficiales**, desarrollado con **.NET Core MVC**, **SQL Server**, **Entity Framework Core** y **Bootstrap 5**, con un diseño visual **futurista y tecnológico**.

---

## 🚀 Descripción general

**Robotics Fixture System** permite registrar competidores, generar enfrentamientos (fixtures) de manera automática o manual según el reglamento, y administrar rondas y resultados hasta la final del torneo.

El sistema soporta:
- **Modo Autónomo**: Basado en el reglamento de Sumo de la Liga Nacional de Robótica (LNR)
- **Modo Radiocontrol**: Basado en el reglamento "Batalla Robot" de la sede Dinámica

Cuenta con una **interfaz moderna con estilo neón**, inspirada en la estética robótica, e integra **animaciones interactivas** para el sorteo y la premiación final.

---

## 🎮 Modos de Combate

### 🤖 Modo Autónomo (Sumo LNR)

Basado en el reglamento de la **Liga Nacional de Robótica (LNR)** para competencias de Sumo.

**Características:**
- ✅ Robots completamente autónomos (sin control directo del operador)
- ✅ Combates al mejor de **3 asaltos** de hasta 3 minutos cada uno
- ✅ El ganador es el primero en ganar **2 asaltos**
- ✅ Resultados determinados **automáticamente** por simulación
- ✅ Basado en el **RatingSeed** (nivel de habilidad) de cada robot
- ✅ Avance automático de rondas
- ✅ Resultados reproducibles (usa semilla aleatoria del torneo)

**Flujo de simulación:**
1. El sistema genera automáticamente los enfrentamientos
2. Cada combate se simula al mejor de 3 asaltos
3. La probabilidad de ganar cada asalto se calcula: `P(A gana) = rating_A / (rating_A + rating_B)`
4. Se registran los resultados de cada asalto individual
5. El ganador avanza automáticamente a la siguiente ronda

### 🎮 Modo Radiocontrol (Batalla Robot)

Basado en el reglamento **"Batalla Robot"** de la sede Dinámica.

**Características:**
- ✅ Robots controlados a distancia (radiocontrol)
- ✅ Combate de **un solo round** de 3 minutos
- ✅ Resultados registrados **manualmente** por el juez
- ✅ Múltiples tipos de victoria:
  - 🚫 **3 Outs**: Sacar al oponente del cuadrilátero 3 veces
  - ⏱️ **Inmovilización**: Dejar al oponente inmovilizado durante un conteo de 10 segundos
  - 🔄 **Volcado**: Voltear completamente al robot oponente
  - ❌ **Descalificación**: Por violación de reglas
  - ⚖️ **Decisión de jueces**: Por puntos al final del tiempo
- ✅ Registro de juez y descripción detallada del combate
- ✅ Control total sobre los resultados

**Flujo de combate:**
1. El sistema genera los enfrentamientos
2. Los combates quedan pendientes de resultado manual
3. El juez presencia el combate físico
4. El juez registra el ganador, tipo de victoria y observaciones
5. El sistema valida y guarda el resultado
6. El ganador avanza a la siguiente ronda

---

## 🧠 Tecnologías utilizadas

- **Backend:** .NET Core 8 MVC (C#)
- **Frontend:** Bootstrap 5, CSS personalizado, JavaScript
- **Base de datos:** SQL Server
- **ORM:** Entity Framework Core
- **Diseño:** Estilo futurista, colores neón y fondo oscuro
- **Animaciones:** CSS3 (`@keyframes`, `transition`)
- **API REST:** Para registro de resultados

---

## ⚙️ Funcionalidades principales

### 🧩 Gestión de competidores
- CRUD completo (crear, editar, eliminar, listar)
- Validaciones de campos (nombre, equipo, categoría)
- **RatingSeed** (nivel de habilidad 1-100) para simulaciones
- Visualización clara con íconos y colores funcionales

### 🏆 Gestión de torneos
- **Crear torneos** con selección de modo de combate
- Generación de **fixtures aleatorios**
- Avance automático o manual de rondas según el modo
- Si hay **número impar de competidores**, el sistema gestiona **repechajes automáticos**
- Visualización del estado de cada combate

### 🎯 Sistema de combates

#### Modo Autónomo
- Simulación automática al mejor de 3 asaltos
- Registro detallado de resultados por asalto
- Reproducibilidad mediante semilla aleatoria
- Avance automático de todas las rondas

#### Modo Radiocontrol
- Interfaz para registro manual de resultados
- Selección de tipo de victoria
- Campo para descripción del combate
- Registro del nombre del juez
- API REST para integración con sistemas externos

### 🎉 Pantalla final de premiación
- Al finalizar el torneo, se muestra una **pantalla animada** con:
  - 🥇 Primer lugar (trofeo dorado animado)
  - 🥈 Segundo lugar (trofeo plateado)
  - 🥉 Tercer lugar (trofeo bronce)
- Efectos de **confeti, luces y texto animado**
- Botones para **reiniciar torneo** o **volver al inicio**

---

## 🎨 Diseño visual

El sistema tiene una estética **robótica y moderna**, con colores neón y fondo oscuro dinámico.

### Paleta de colores
- **Azul Cian** (`#00e5ff`): Principal / navegación
- **Verde** (`#00ff99`): Guardar / confirmar / ganador
- **Naranja** (`#ff9f00`): Advertencias / torneos / VS
- **Rojo** (`#ff4757`): Eliminar / reiniciar
- **Morado** (`#a855f7`): Acentos especiales

### Características visuales
1. **Tema claro/oscuro**: Botón de alternancia en la navegación
2. **Animaciones suaves**: Transiciones y efectos de hover
3. **Iconos consistentes**: Font Awesome 6
4. **Cards interactivas**: Con efectos de elevación y brillo
5. **Tipografía**: Poppins (general) y Orbitron (títulos tecnológicos)

---

## 📁 Estructura del proyecto

```
RoboticsFixture/
├── Controllers/
│   ├── CompetitorsController.cs
│   ├── TournamentController.cs
│   ├── HomeController.cs
│   └── Api/
│       └── MatchesApiController.cs
├── Models/
│   ├── Competitor.cs (con RatingSeed)
│   ├── Match.cs (extendido con modos de combate)
│   ├── Tournament.cs (con CombatMode)
│   ├── Enums/
│   │   ├── CombatMode.cs
│   │   ├── DecisionMethod.cs
│   │   └── OutcomeType.cs
│   └── DTOs/
│       ├── MatchResultDto.cs
│       └── CreateTournamentDto.cs
├── Services/
│   ├── ICombatSimulationService.cs
│   └── CombatSimulationService.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Views/
│   ├── Home/
│   ├── Competitors/
│   ├── Tournament/
│   │   ├── Index.cshtml
│   │   ├── CreateTournament.cshtml (NUEVO)
│   │   ├── Fixture.cshtml (ACTUALIZADO)
│   │   ├── RecordResult.cshtml (NUEVO)
│   │   ├── Podium.cshtml
│   │   └── ShowRepechaje.cshtml
│   └── Shared/
└── wwwroot/
    ├── css/
    │   └── robotics-styles.css
    └── js/
```

---

## 🚀 Instalación y configuración

### Requisitos previos
- .NET Core 8 SDK
- SQL Server 2019 o superior (o SQL Server Express)
- Visual Studio 2022 o VS Code

### Pasos de instalación

1. **Clonar el repositorio**
```bash
git clone https://github.com/PaulGerman23/Robotics_Fixture.git
cd Robotics_Fixture
```

2. **Configurar la cadena de conexión**

Editar `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RoboticsFixtureDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

3. **Aplicar las migraciones**
```bash
dotnet ef database update
```

O desde la Package Manager Console en Visual Studio:
```powershell
Update-Database
```

4. **Ejecutar el proyecto**
```bash
dotnet run
```

O presionar `F5` en Visual Studio.

5. **Acceder a la aplicación**
```
https://localhost:7141
```

---

## 📖 Guía de uso

### Crear competidores

1. Ir a **"Competidores"** en el menú
2. Clic en **"Nuevo Competidor"**
3. Completar:
   - Nombre del robot
   - Equipo
   - Categoría
   - **RatingSeed** (1-100, nivel de habilidad)
4. Guardar

### Crear un torneo

1. Ir a **"Torneos"**
2. Seleccionar una categoría
3. Clic en **"Crear Torneo"**
4. Completar:
   - Nombre del torneo
   - **Seleccionar modo de combate**:
     - 🤖 Autónomo (Sumo LNR)
     - 🎮 Radiocontrol (Batalla Robot)
   - Descripción (opcional)
5. Clic en **"Crear Torneo y Generar Fixture"**

### Gestionar combates

#### Modo Autónomo
- El sistema simula automáticamente todos los combates
- Los resultados se muestran en tiempo real
- El avance de rondas es automático
- Ver el detalle de asaltos en cada combate

#### Modo Radiocontrol
1. Ir al fixture del torneo
2. Localizar el combate pendiente
3. Clic en **"Registrar Resultado"**
4. Seleccionar el ganador
5. Elegir el tipo de victoria
6. Agregar descripción (opcional)
7. Ingresar nombre del juez (opcional)
8. Guardar

### Ver el podio

Al finalizar el torneo:
1. Aparecerá automáticamente un enlace **"Ver Podio de Ganadores"**
2. Se muestra una pantalla animada con los 3 primeros lugares
3. Opciones para reiniciar o volver al inicio

---

## 🔌 API REST

El sistema incluye una API REST para integración externa.

### Endpoints disponibles

#### POST /api/matches/{id}/result
Registrar resultado manual de un combate.

**Request:**
```json
{
  "matchId": 1,
  "winnerId": 5,
  "outcomeType": 0,
  "description": "Victoria por 3 outs consecutivos en el minuto 2:30",
  "judgeName": "Juan Pérez"
}
```

**Response:**
```json
{
  "message": "Resultado registrado exitosamente",
  "match": {
    "id": 1,
    "winnerId": 5,
    "winnerName": "RoboWarrior",
    "outcomeDescription": "Victoria por 3 outs consecutivos en el minuto 2:30",
    "completedDate": "2024-11-09T15:30:00"
  }
}
```

#### GET /api/matches/{id}
Obtener detalles de un combate específico.

#### GET /api/matches/pending?category=Senior
Obtener todos los combates pendientes de resultado manual.

---

## 🧪 Testing

### Casos de prueba recomendados

#### Modo Autónomo
- [ ] Crear torneo en modo autónomo con 4 competidores
- [ ] Verificar simulación automática de combates
- [ ] Verificar que los resultados son reproducibles
- [ ] Verificar registro de asaltos individuales
- [ ] Verificar avance automático de rondas
- [ ] Verificar podio final

#### Modo Radiocontrol
- [ ] Crear torneo en modo radiocontrol con 4 competidores
- [ ] Verificar que NO se simulan automáticamente
- [ ] Registrar resultado manual desde la interfaz
- [ ] Registrar resultado manual desde la API
- [ ] Verificar todos los tipos de victoria
- [ ] Verificar avance manual de rondas
- [ ] Verificar podio final

#### Casos especiales
- [ ] Torneo con número impar de competidores
- [ ] Múltiples torneos en paralelo con diferentes modos
- [ ] Reinicio de torneos
- [ ] Edición de competidores durante un torneo activo

---

## 🔧 Configuración avanzada

### Ajustar probabilidades de simulación

Editar `Services/CombatSimulationService.cs`:

```csharp
// Fórmula actual: P(A gana) = rating_A / (rating_A + rating_B)
// Para cambiar la fórmula, modificar el método SimulateRound()
```

### Personalizar tipos de victoria

Agregar valores al enum en `Models/Enums/OutcomeType.cs`

### Cambiar número de asaltos en modo autónomo

Editar `Services/CombatSimulationService.cs`:

```csharp
// Cambiar el límite de asaltos (actualmente 3)
while (roundsWonP1 < 2 && roundsWonP2 < 2 && roundNumber <= 3)
```

---

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

---

## 📝 Licencia

Este proyecto es de código abierto para uso educativo y en competencias de robótica locales.

---

## 🛠️ Autor

**Paul Germán Mamani**  
Desarrollador de software y sistemas robóticos ⚙️  
📍 Salta, Argentina  
💻 Proyecto para torneos de robótica de la Liga Nacional de Robótica

---

## 📞 Soporte

Si tienes preguntas o problemas:
- Abre un [Issue](https://github.com/PaulGerman23/Robotics_Fixture/issues) en GitHub
- Contacta al autor

---

## 🎯 Roadmap futuro

- [ ] Sistema de estadísticas por competidor
- [ ] Histórico de torneos
- [ ] Exportación de resultados a PDF
- [ ] Sistema de brackets doble eliminación
- [ ] Transmisión en vivo del estado de combates
- [ ] App móvil para jueces (modo radiocontrol)
- [ ] Integración con hardware (sensores, cronómetros)

---

**¡Que comience la batalla de robots! 🤖⚡**