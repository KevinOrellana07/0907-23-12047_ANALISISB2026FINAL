# NetGuardGT - API REST Gestión de Incidentes

API REST para gestión de incidentes de red de NetGuard GT, desarrollada en C# con ASP.NET Core y SQLite.

## Tecnologías

- .NET 8 / ASP.NET Core
- Entity Framework Core + SQLite
- xUnit (pruebas unitarias)
- Swagger (documentación)
- Render.com (despliegue)

## Estructura del proyecto

```
NetGuardGT/
├── Controllers/
│   ├── IncidentesController.cs   # Endpoints de incidentes
│   └── TecnicosController.cs     # Endpoints de técnicos
├── Models/
│   ├── Incidente.cs
│   ├── Tecnico.cs
│   └── HistorialIncidente.cs
├── Data/
│   └── AppDbContext.cs           # Contexto SQLite + seed data
├── Services/
│   └── IncidenteService.cs       # Toda la lógica de negocio
└── Program.cs

NetGuardGT.Tests/
└── IncidenteServiceTests.cs      # 10 pruebas unitarias con xUnit
```

## Cómo correr el proyecto localmente

```bash
cd NetGuardGT
dotnet restore
dotnet run
```

Swagger disponible en: `http://localhost:5000/swagger`

## Cómo correr las pruebas

```bash
cd NetGuardGT.Tests
dotnet test
```

## Endpoints disponibles

### Incidentes

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | /api/incidentes | Listar todos (filtros: estado, sitio) |
| GET | /api/incidentes/{id} | Obtener por ID con historial |
| POST | /api/incidentes | Registrar nuevo incidente |
| POST | /api/incidentes/{id}/asignar | Asignar técnico |
| PUT | /api/incidentes/{id}/estado | Cambiar estado |
| PUT | /api/incidentes/{id}/reasignar | Reasignar técnico |
| PUT | /api/incidentes/{id}/cerrar | Cerrar incidente |
| GET | /api/incidentes/{id}/historial | Ver historial de cambios |
| GET | /api/incidentes/reporte/sla | Reporte de cumplimiento SLA |
| POST | /api/incidentes/escalado/verificar | Verificar escalado automático |

### Técnicos

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | /api/tecnicos | Listar técnicos |
| GET | /api/tecnicos/{id} | Obtener técnico por ID |
| GET | /api/tecnicos/{id}/incidentes | Ver incidentes activos del técnico |
| POST | /api/tecnicos | Crear técnico |

## Ejemplos de uso

### Registrar incidente
```json
POST /api/incidentes
{
  "tipo": "fibra optica",
  "descripcion": "Corte en nodo principal sitio 5",
  "severidad": "Critico",
  "sitio": "Sitio-05"
}
```

### Asignar técnico
```json
POST /api/incidentes/1/asignar
{
  "tecnicoId": 1,
  "usuario": "supervisor"
}
```

### Cambiar estado
```json
PUT /api/incidentes/1/estado
{
  "nuevoEstado": "En progreso",
  "usuario": "tecnico"
}
```

### Cerrar incidente
```json
PUT /api/incidentes/1/cerrar
{
  "observacion": "Enlace restaurado, nodo reemplazado",
  "usuario": "supervisor"
}
```

## Reglas de negocio implementadas

1. Tiempo máximo de resolución según severidad (SLA): Critico=1h, Urgente=4h, Normal=24h
2. Un técnico no puede tener más de 3 incidentes activos simultáneamente
3. Los estados solo avanzan en una dirección: Registrado → Asignado → En progreso → Resuelto → Cerrado
4. Un incidente puede reasignarse en cualquier momento liberando al técnico anterior
5. Incidentes Críticos o Urgentes sin atención por más de 2 horas se marcan como Escalado automáticamente
6. Solo técnicos con especialidad coincidente pueden ser asignados a un incidente
7. Historial completo de cambios de estado por incidente
8. Reporte de cumplimiento SLA con porcentaje global

## Despliegue en Render.com

1. Subir el proyecto a GitHub en el repositorio `CARNET_ANALISISB2026FINAL`
2. Crear cuenta en [render.com](https://render.com)
3. New > Web Service > conectar repositorio de GitHub
4. Configurar:
   - **Build Command:** `dotnet publish -c Release -o out`
   - **Start Command:** `dotnet out/NetGuardGT.dll`
   - **Environment:** Docker o .NET
5. Deploy y copiar la URL pública generada
