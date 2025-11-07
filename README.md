# 🤖 Robotics Fixture System

Un sistema avanzado de **fixture para torneos de robótica**, desarrollado con **.NET Core MVC**, **SQL Server**, **Entity Framework Core** y **Bootstrap 5**, con un diseño visual **futurista y tecnológico**.

---

## 🚀 Descripción general

**Robotics Fixture System** permite registrar competidores, generar enfrentamientos (fixtures) de manera automática, y administrar rondas y resultados hasta la final del torneo.

Cuenta con una **interfaz moderna con estilo neón**, inspirada en la estética robótica, e integra **animaciones interactivas** para el sorteo y la premiación final.

---

## 🧠 Tecnologías utilizadas

- **Backend:** .NET Core MVC (C#)
- **Frontend:** Bootstrap 5, CSS personalizado, JavaScript
- **Base de datos:** SQL Server
- **ORM:** Entity Framework Core
- **Diseño:** Estilo futurista, colores neón y fondo oscuro
- **Animaciones:** CSS3 (`@keyframes`, `transition`) y JS opcional (GSAP, Animate.css)

---

## ⚙️ Funcionalidades principales

### 🧩 Gestión de competidores
- CRUD completo (crear, editar, eliminar, listar)
- Validaciones de campos (nombre, equipo, categoría)
- Visualización clara con íconos y colores funcionales

### 🏆 Gestión de torneos
- Generación de **fixtures aleatorios**
- Avance automático por rondas
- Si hay **número impar de competidores**, el sistema elige automáticamente quién pasa mediante **sorteo visual animado**
- Registro del participante con **pase automático (BYE)**

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

### Mejores visuales aplicadas
1. **Colores y contraste**
   - Se mejoró la visibilidad de textos, íconos y botones.
   - Se usó una paleta equilibrada con variables CSS (`:root {}`):
     - Azul (`#00e0ff`) → principal / navegación
     - Verde (`#00ff99`) → guardar / confirmar
     - Naranja (`#ff9f00`) → advertencias / torneos
     - Rojo (`#ff4b5c`) → eliminar / reiniciar
   - Fondo animado con gradiente o partículas.

2. **Botones e íconos**
   - Hover con brillo suave y sombras tipo neón.
   - Íconos rediseñados para mantener contraste y visibilidad.

3. **Formularios**
   - Campos más claros, placeholders visibles y etiquetas legibles.
   - Botones “Guardar” y “Volver” con alto contraste y transiciones suaves.

4. **Cards y alineación**
   - Si hay un solo card (por ejemplo, en torneos), se **centra automáticamente**.
   - Márgenes y espaciados uniformes para mantener equilibrio visual.

5. **Navegación**
   - Barra superior con los enlaces:
     - 🏠 **Home**
     - 👥 **Competidores**
     - 🏆 **Torneos**
   - Estilo neón coherente con el resto de la interfaz.

6. **Fondo y efectos**
   - Fondo animado con movimiento sutil.
   - Efectos glow y sombras difuminadas.
   - Tipografía: *Poppins* o *Inter*.

---

## 🧩 Lógica de torneos (detallada)

### 🔹 Sorteo automático de impar
- Si el total de competidores es impar:
  - Se elige automáticamente un participante al azar usando `Random()` en C#.
  - Se muestra una **pantalla o modal de sorteo** con efecto visual (ruleta o selección animada).
  - El participante se marca como **“Pase automático (BYE)”** en la base de datos.

### 🔹 Avance de rondas
- Los ganadores de cada cruce avanzan automáticamente.
- El fixture se actualiza dinámicamente con cada ronda.

### 🔹 Final del torneo
- Cuando se determina el ganador:
  - Aparece una **pantalla de premiación** con animaciones y trofeos.
  - Se muestran los 3 primeros puestos.
  - Incluye efectos visuales y opción para reiniciar el torneo.

---

## 🧰 Estructura del proyecto

RoboticsFixture/
├── Controllers/
│ ├── CompetitorsController.cs
│ ├── TournamentsController.cs
│ └── HomeController.cs
├── Models/
│ ├── Competitor.cs
│ ├── Match.cs
│ └── Tournament.cs
├── Views/
│ ├── Home/
│ ├── Competitors/
│ ├── Tournaments/
│ └── Shared/
├── wwwroot/
│ ├── css/
│ │ └── site.css
│ ├── js/
│ └── img/
└── appsettings.json

yaml
Copiar código

---

## 🧠 Prompt unificado para Claude.ai

> Quiero que mejores y expandas mi sistema web **“Robotics Fixture System”**, desarrollado en **.NET Core MVC + Bootstrap 5 + CSS personalizado**.  
>  
> El sistema tiene un estilo **futurista, tecnológico y de robótica** con colores **neón azules y fondo oscuro**, pero necesito una mejora integral tanto **visual como funcional**, manteniendo su estilo moderno y profesional.  
>  
> ### 🎨 Mejora visual
> - Optimizar colores y contraste con una paleta balanceada.
> - Rediseñar botones, íconos y formularios con visibilidad óptima.
> - Alinear cards y centrar el card único de torneos.
> - Agregar un botón **🏠 Home** en la barra de navegación.
> - Mantener estética robótica con fondo animado, glow controlado y tipografía moderna.
>
> ### ⚙️ Lógica del torneo
> - Implementar pase automático por número impar con **sorteo aleatorio** y animación visual.
> - Registrar al participante como “BYE”.
>
> ### 🏆 Pantalla final
> - Mostrar animación de premiación con trofeos y efectos (oro, plata, bronce).
> - Efectos de confeti, luces y texto animado.
> - Botones para **reiniciar torneo** o **volver al inicio**.
>
> ### 🧱 Entrega esperada
> - Código completo en **C#, Razor, JS y CSS** con la lógica del sorteo, la premiación y el rediseño visual.  
> - No dar explicaciones: entregar código final limpio y funcional.

---

## 🎯 Objetivo final

Crear una experiencia **automática, interactiva y visualmente impactante**, con:
- Fixtures dinámicos  
- Sorteos automáticos  
- Premios animados  
- Interfaz profesional y moderna  

Todo dentro de un entorno coherente con el universo robótico y tecnológico que caracteriza al sistema.

---

## 🛠️ Autor

**Paul Germán Mamani**  
Desarrollador de software y sistemas robóticos ⚙️  
📍 Argentina  
💻 Proyecto académico y visual para torneos de robótica locales.

---
