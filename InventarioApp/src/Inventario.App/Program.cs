using Inventario.Core.Services;

namespace Inventario.App;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Los usuarios se guardan fuera del proyecto (AppData), nunca dentro del repositorio.
        var ruta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InventarioApp", "usuarios.json");

        string usuario;
        using (var login = new LoginForm(new UsuarioService(ruta)))
        {
            if (login.ShowDialog() != DialogResult.OK) return;
            usuario = login.UsuarioActual;
        }

        Application.Run(new MainForm(usuario));
    }
}
