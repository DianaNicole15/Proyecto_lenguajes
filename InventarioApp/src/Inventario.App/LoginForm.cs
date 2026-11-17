using Inventario.Core.Services;

namespace Inventario.App;

/// <summary>Pantalla de inicio de sesión según el diseño (y la misma pantalla para crear un usuario).</summary>
public class LoginForm : Form
{
    private static readonly Color Campo = ColorTranslator.FromHtml("#FFFBFF");

    private readonly UsuarioService _usuarios;
    private readonly Label _lblTitulo = new();
    private readonly Label _lblConfirmar;
    private readonly TextBox _txtUsuario = new();
    private readonly TextBox _txtClave = new() { UseSystemPasswordChar = true };
    private readonly TextBox _txtConfirmar = new() { UseSystemPasswordChar = true };
    private readonly Panel _cajaConfirmar;
    private readonly Button _btnPrincipal;
    private readonly Button _btnSecundario;
    private bool _registro;

    public string UsuarioActual { get; private set; } = "";

    public LoginForm(UsuarioService usuarios)
    {
        _usuarios = usuarios;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(724, 432);
        BackColor = Estilo.Marron;
        Text = "Punto de venta - Inicio de sesión";

        _lblTitulo.SetBounds(0, 54, 724, 40);
        _lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
        _lblTitulo.Font = new Font("Segoe UI", 17f, FontStyle.Bold);

        _lblConfirmar = Estilo.Etiqueta("Confirmar contraseña:", 178, 270, 12f);
        _cajaConfirmar = Estilo.Caja(_txtConfirmar, 178, 298, 368, 32, Campo);

        _btnPrincipal = Estilo.Boton("", (s, e) => { if (_registro) Crear(); else Ingresar(); }, Estilo.AzulBanner, Estilo.Pizarra, 13f);
        _btnSecundario = Estilo.Boton("", (s, e) => CambiarModo(!_registro), Estilo.AzulBanner, Estilo.Pizarra, 11f);

        Controls.Add(_lblTitulo);
        Controls.Add(Estilo.Etiqueta("Usuario:", 178, 112, 12f));
        Controls.Add(Estilo.Caja(_txtUsuario, 178, 142, 368, 32, Campo));
        Controls.Add(Estilo.Etiqueta("Contraseña:", 178, 202, 12f));
        Controls.Add(Estilo.Caja(_txtClave, 178, 232, 368, 32, Campo));
        Controls.Add(_lblConfirmar);
        Controls.Add(_cajaConfirmar);
        Controls.Add(_btnPrincipal);
        Controls.Add(_btnSecundario);
        Controls.Add(new BarraTitulo(this, "Punto de venta - Inicio de sesión", Estilo.Gris, 33, 12f));
        AcceptButton = _btnPrincipal;

        CambiarModo(!_usuarios.HayUsuarios); // la primera vez no hay usuarios: se crea el primero
    }

    private void CambiarModo(bool registro)
    {
        _registro = registro;
        _lblConfirmar.Visible = registro;
        _cajaConfirmar.Visible = registro;
        _txtConfirmar.Clear();

        _lblTitulo.Text = registro
            ? (_usuarios.HayUsuarios ? "Crear nuevo usuario" : "Crea tu primer usuario")
            : "Ingrese su usuario y contraseña.";

        _btnPrincipal.Text = registro ? "Crear usuario" : "Ingresar";
        _btnSecundario.Text = registro ? "Volver" : "Crear nuevo usuario";
        _btnSecundario.Visible = !registro || _usuarios.HayUsuarios;

        if (registro)
        {
            _btnPrincipal.SetBounds(271, 346, 182, 38);
            _btnSecundario.SetBounds(271, 388, 182, 34);
        }
        else
        {
            _btnPrincipal.SetBounds(303, 289, 117, 42);
            _btnSecundario.SetBounds(270, 347, 182, 42);
        }
    }

    private void Ingresar()
    {
        if (_usuarios.Autenticar(_txtUsuario.Text, _txtClave.Text))
        {
            UsuarioActual = _txtUsuario.Text.Trim();
            DialogResult = DialogResult.OK;
        }
        else
        {
            Aviso.Advertencia("Usuario o contraseña incorrectos.");
        }
    }

    private void Crear()
    {
        if (_txtClave.Text != _txtConfirmar.Text)
        {
            Aviso.Advertencia("Las contraseñas no coinciden.");
            return;
        }
        if (Estilo.Ejecutar(() => _usuarios.Crear(_txtUsuario.Text, _txtClave.Text)))
        {
            Aviso.Exito("¡Registro exitoso!");
            _txtClave.Clear();
            CambiarModo(false);
        }
    }
}
