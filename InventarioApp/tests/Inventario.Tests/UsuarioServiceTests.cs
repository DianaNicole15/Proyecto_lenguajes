using Inventario.Core.Exceptions;
using Inventario.Core.Services;
using Xunit;

namespace Inventario.Tests;

public class UsuarioServiceTests
{
    private static void ConArchivoTemporal(Action<string> prueba)
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"usuarios_{Guid.NewGuid():N}.json");
        try { prueba(ruta); }
        finally { if (File.Exists(ruta)) File.Delete(ruta); }
    }

    [Fact]
    public void CrearYAutenticar_FuncionaConLaContrasenaCorrecta()
    {
        ConArchivoTemporal(ruta =>
        {
            var servicio = new UsuarioService(ruta);
            Assert.False(servicio.HayUsuarios);

            servicio.Crear("maria", "secreto123");

            Assert.True(servicio.HayUsuarios);
            Assert.True(servicio.Autenticar("MARIA", "secreto123")); // el usuario no distingue mayúsculas
            Assert.False(servicio.Autenticar("maria", "otra-clave"));
            Assert.False(servicio.Autenticar("nadie", "secreto123"));
        });
    }

    [Fact]
    public void Crear_RechazaUsuarioRepetidoYDatosCortos()
    {
        ConArchivoTemporal(ruta =>
        {
            var servicio = new UsuarioService(ruta);
            servicio.Crear("maria", "secreto123");

            Assert.Throws<ValidacionException>(() => servicio.Crear("Maria", "otraclave1"));
            Assert.Throws<ValidacionException>(() => servicio.Crear("ab", "secreto123"));
            Assert.Throws<ValidacionException>(() => servicio.Crear("pedro", "123"));
        });
    }

    [Fact]
    public void LosUsuariosPersistenYLaContrasenaNoSeGuardaEnTexto()
    {
        ConArchivoTemporal(ruta =>
        {
            new UsuarioService(ruta).Crear("maria", "secreto123");

            Assert.True(new UsuarioService(ruta).Autenticar("maria", "secreto123"));
            Assert.DoesNotContain("secreto123", File.ReadAllText(ruta));
        });
    }
}
