using Microsoft.EntityFrameworkCore;
using NetGuardGT.Data;
using NetGuardGT.Models;
using NetGuardGT.Services;
using Xunit;

namespace NetGuardGT.Tests;

public class IncidenteServiceTests
{
    private AppDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Tecnicos.AddRange(
            new Tecnico { Id = 1, Nombre = "Carlos", Especialidad = "fibra optica" },
            new Tecnico { Id = 2, Nombre = "Maria", Especialidad = "microondas" }
        );
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task RegistrarIncidente_DebeCrearConEstadoRegistrado()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);

        var inc = await service.RegistrarAsync("fibra optica", "Corte en nodo 5", "Critico", "Sitio-01");

        Assert.NotNull(inc);
        Assert.Equal("Registrado", inc.Estado);
        Assert.Equal("Critico", inc.Severidad);
        Assert.False(inc.Escalado);
    }

    [Fact]
    public async Task AsignarTecnico_DebeActualizarEstadoAAsignado()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("fibra optica", "Falla enlace", "Normal", "Sitio-02");

        var (ok, error, resultado) = await service.AsignarAsync(inc.Id, 1, "supervisor");

        Assert.True(ok);
        Assert.Equal("Asignado", resultado!.Estado);
        Assert.Equal(1, resultado.TecnicoId);
    }

    [Fact]
    public async Task AsignarTecnico_ConEspecialidadIncompatible_DebeRetornarError()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("sistemas electricos", "Falla electrica", "Urgente", "Sitio-03");

        var (ok, error, _) = await service.AsignarAsync(inc.Id, 1, "supervisor");

        Assert.False(ok);
        Assert.Contains("especialidad", error);
    }

    [Fact]
    public async Task AsignarTecnico_ConMasDe3Activos_DebeRetornarError()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);

        for (int i = 0; i < 3; i++)
        {
            var inc = await service.RegistrarAsync("fibra optica", $"Falla {i}", "Normal", "Sitio-0" + i);
            await service.AsignarAsync(inc.Id, 1, "supervisor");
        }

        var nuevo = await service.RegistrarAsync("fibra optica", "Falla extra", "Normal", "Sitio-99");
        var (ok, error, _) = await service.AsignarAsync(nuevo.Id, 1, "supervisor");

        Assert.False(ok);
        Assert.Contains("3 incidentes activos", error);
    }

    [Fact]
    public async Task CambiarEstado_FlujoValido_DebeActualizar()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("fibra optica", "Test", "Normal", "Sitio-01");
        await service.AsignarAsync(inc.Id, 1, "supervisor");

        var (ok, error, resultado) = await service.CambiarEstadoAsync(inc.Id, "En progreso", "tecnico");

        Assert.True(ok);
        Assert.Equal("En progreso", resultado!.Estado);
    }

    [Fact]
    public async Task CambiarEstado_SaltandoEstado_DebeRetornarError()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("fibra optica", "Test", "Normal", "Sitio-01");

        var (ok, error, _) = await service.CambiarEstadoAsync(inc.Id, "Resuelto", "tecnico");

        Assert.False(ok);
        Assert.NotEmpty(error);
    }

    [Fact]
    public async Task CerrarIncidente_SinEstarResuelto_DebeRetornarError()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("fibra optica", "Test", "Normal", "Sitio-01");

        var (ok, error, _) = await service.CerrarAsync(inc.Id, "Cerrado ok", "supervisor");

        Assert.False(ok);
        Assert.Contains("Resuelto", error);
    }

    [Fact]
    public async Task CerrarIncidente_SinObservacion_DebeRetornarError()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("fibra optica", "Test", "Normal", "Sitio-01");
        await service.AsignarAsync(inc.Id, 1, "supervisor");
        await service.CambiarEstadoAsync(inc.Id, "En progreso", "tecnico");
        await service.CambiarEstadoAsync(inc.Id, "Resuelto", "tecnico");

        var (ok, error, _) = await service.CerrarAsync(inc.Id, "", "supervisor");

        Assert.False(ok);
        Assert.Contains("obligatoria", error);
    }

    [Fact]
    public async Task EscalarIncidentes_CriticoMasDe2Horas_DebeEscalar()
    {
        var db = CrearDb();
        var inc = new Incidente
        {
            Tipo = "fibra optica", Descripcion = "Test", Severidad = "Critico",
            Sitio = "Sitio-01", Estado = "Registrado",
            FechaRegistro = DateTime.UtcNow.AddHours(-3)
        };
        db.Incidentes.Add(inc);
        await db.SaveChangesAsync();

        var service = new IncidenteService(db);
        await service.EscalarIncidentesAsync();

        var actualizado = await db.Incidentes.FindAsync(inc.Id);
        Assert.True(actualizado!.Escalado);
    }

    [Fact]
    public async Task RegistrarIncidente_DebeCrearHistorial()
    {
        var db = CrearDb();
        var service = new IncidenteService(db);

        var inc = await service.RegistrarAsync("fibra optica", "Test historial", "Normal", "Sitio-01");
        var historial = db.Historial.Where(h => h.IncidenteId == inc.Id).ToList();

        Assert.Single(historial);
        Assert.Equal("Registrado", historial[0].EstadoNuevo);
    }

    [Fact]
    public async Task ReasignarIncidente_DebeActualizarTecnico()
    {
        var db = CrearDb();
        db.Tecnicos.Add(new Tecnico { Id = 3, Nombre = "Luis", Especialidad = "fibra optica" });
        await db.SaveChangesAsync();

        var service = new IncidenteService(db);
        var inc = await service.RegistrarAsync("fibra optica", "Test", "Normal", "Sitio-01");
        await service.AsignarAsync(inc.Id, 1, "supervisor");

        var (ok, error, resultado) = await service.ReasignarAsync(inc.Id, 3, "supervisor", "Tecnico original no disponible");

        Assert.True(ok);
        Assert.Equal(3, resultado!.TecnicoId);
    }
}
