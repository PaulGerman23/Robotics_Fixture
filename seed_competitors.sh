#!/bin/bash

# Nombre de la base de datos SQLite (cámbialo si tu archivo se llama diferente)
DB_NAME="RoboticsFixture.db"

# Verificar si sqlite3 está instalado
if ! command -v sqlite3 &> /dev/null; then
    echo "❌ Error: sqlite3 no está instalado. Instálalo con: sudo apt install sqlite3"
    exit 1
fi

echo "🤖 Conectando a la base de datos '$DB_NAME' e insertando competidores..."

# Ejecutar sentencias SQL
sqlite3 "$DB_NAME" <<EOF
BEGIN TRANSACTION;

-- Asegurarse de que la tabla exista (basado en tu modelo Competitor.cs)
CREATE TABLE IF NOT EXISTS Competitors (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Team TEXT NOT NULL,
    Category TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    RatingSeed INTEGER NOT NULL DEFAULT 50,
    Description TEXT
);

-- Insertar los 24 competidores para la categoría Sumo
INSERT INTO Competitors (Name, Team, Category, IsActive, RatingSeed, Description) VALUES
('Titanium Crusher', 'UTN Robotics', 'Sumo', 1, 85, 'Robot defensivo con pala de titanio.'),
('Black Mamba', 'Team Cobra', 'Sumo', 1, 92, 'Alta velocidad y sensores de línea avanzados.'),
('Iron Golem', 'Mech Warriors', 'Sumo', 1, 78, 'Estructura pesada y tracción 4x4.'),
('Cyber Sumo X', 'Tech University', 'Sumo', 1, 65, 'Prototipo experimental con IA.'),
('Doomsday', 'Destruction Bros', 'Sumo', 1, 88, 'Campeón regional 2023.'),
('Wall-E 2.0', 'Pixar Fans', 'Sumo', 1, 45, 'Pequeño pero valiente.'),
('Red Dragon', 'Asian Tigers', 'Sumo', 1, 95, 'Agresividad pura en el dohyo.'),
('Shadow Hunter', 'Night Squad', 'Sumo', 1, 70, 'Estrategia de sigilo y ataque lateral.'),
('Big Boy', 'Heavy Metal Club', 'Sumo', 1, 98, 'Usa el peso máximo permitido.'),
('Nano Bot', 'Micro Systems', 'Sumo', 1, 55, 'Compacto y rápido.'),
('Thunderbolt', 'Storm Chasers', 'Sumo', 1, 82, 'Ataques eléctricos rápidos.'),
('Steel Wall', 'Defense Force', 'Sumo', 1, 90, 'Casi imposible de mover.'),
('Vortex', 'Spin Masters', 'Sumo', 1, 60, 'Diseño rotativo experimental.'),
('Alpha Prime', 'Transformers Team', 'Sumo', 1, 89, 'Líder de los autobots locales.'),
('Omega Supreme', 'Transformers Team', 'Sumo', 1, 87, 'Fuerza bruta.'),
('Quantum Leap', 'Physics Dept', 'Sumo', 1, 75, 'Cálculos de trayectoria en tiempo real.'),
('Bulldozer', 'Construction Crew', 'Sumo', 1, 91, 'Pala frontal de alta potencia.'),
('The Rock', 'Wrestling Bots', 'Sumo', 1, 84, 'Nadie lo mueve.'),
('Speedy Gonzales', 'Mexico Lindo', 'Sumo', 1, 96, 'El más rápido del oeste.'),
('Tank T-100', 'Military Grade', 'Sumo', 1, 79, 'Orugas tipo tanque.'),
('Phoenix', 'Rising Stars', 'Sumo', 1, 68, 'Resurge de las cenizas si es volteado.'),
('Gladiator', 'Rome Robotics', 'Sumo', 1, 72, 'Estilo de combate antiguo.'),
('Sumo King', 'Royal Team', 'Sumo', 1, 93, 'El rey de la colina.'),
('Zero Gravity', 'Space Cadets', 'Sumo', 1, 50, 'Intenta usar aerodinámica (dudoso).');

COMMIT;
EOF

# Verificar el resultado
if [ $? -eq 0 ]; then
    echo "✅ Competidores insertados exitosamente."
    echo "📊 Total de competidores en categoría Sumo:"
    sqlite3 "$DB_NAME" "SELECT COUNT(*) FROM Competitors WHERE Category='Sumo';"
else
    echo "❌ Hubo un error al insertar los datos."
fi
