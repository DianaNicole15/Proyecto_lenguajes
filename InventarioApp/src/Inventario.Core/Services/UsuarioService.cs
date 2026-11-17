using System.Security.Cryptography;
using Inventario.Core.Data;
using Inventario.Core.Exceptions;

namespace Inventario.Core.Services;

public class Usuario
{
    public string Nombre { get; set; } = "";
    public string Sal { get; set; } = "";
    public string Hash { get; set; } = "";
}

/// <summary>
/// Usuarios del sistema. Las contraseñas NUNCA se guardan: solo un hash PBKDF2 con sal.
/// El archivo vive fuera del repositorio (en AppData), así que no hay credenciales en Git.
/// </summary>
public class UsuarioService
{
    private const int Iteraciones = 100_000;
    private const int BytesSal = 16;
    private const int BytesHash = 32;

    private readonly string _ruta;
    private readonly List<Usuario> _usuarios = new();
    private readonly object _candado = new();

    public UsuarioService(string ruta)
    {
        _ruta = ruta;
        if (!File.Exists(ruta)) return;
        try
        {
            _usuarios.AddRange(ArchivoJson.Cargar<List<Usuario>>(ruta));
        }
        catch (Exception ex) when (ex is InventarioException or IOException)
        {
            // Archivo dañado: se empieza sin usuarios (no se sobrescribe hasta crear uno).
        }
    }

    public bool HayUsuarios
    {
        get { lock (_candado) { return _usuarios.Count > 0; } }
    }

    public void Crear(string nombre, string contrasena)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length < 3)
            throw new ValidacionException("El usuario debe tener al menos 3 caracteres.");
        if ((contrasena ?? "").Length < 6)
            throw new ValidacionException("La contraseña debe tener al menos 6 caracteres.");

        lock (_candado)
        {
            if (_usuarios.Any(u => u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                throw new ValidacionException("Ese usuario ya existe.");

            var sal = RandomNumberGenerator.GetBytes(BytesSal);
            _usuarios.Add(new Usuario
            {
                Nombre = nombre,
                Sal = Convert.ToHexString(sal),
                Hash = Convert.ToHexString(Calcular(contrasena!, sal))
            });
            ArchivoJson.Guardar(_ruta, _usuarios);
        }
    }

    public bool Autenticar(string nombre, string contrasena)
    {
        Usuario? usuario;
        lock (_candado)
        {
            usuario = _usuarios.FirstOrDefault(u =>
                u.Nombre.Equals((nombre ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        }
        if (usuario is null) return false;

        var calculado = Calcular(contrasena ?? "", Convert.FromHexString(usuario.Sal));
        return CryptographicOperations.FixedTimeEquals(calculado, Convert.FromHexString(usuario.Hash));
    }

    private static byte[] Calcular(string contrasena, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
}
