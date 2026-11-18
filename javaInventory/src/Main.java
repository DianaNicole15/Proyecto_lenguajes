import javax.swing.*;
import javax.swing.table.DefaultTableModel;
import javax.swing.table.DefaultTableCellRenderer;
import java.awt.*;
import java.io.*;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

public class Main {

    // ==========================================
    // MODELOS DE DATOS
    // ==========================================
    public static class Producto {
        private String codigo;
        private String nombre;
        private double precio;
        private String departamento;
        private int stock;
        private int stockMinimo;

        public Producto(String codigo, String nombre, double precio, String departamento, int stock, int stockMinimo) {
            this.codigo = codigo;
            this.nombre = nombre;
            this.precio = precio;
            this.departamento = departamento;
            this.stock = stock;
            this.stockMinimo = stockMinimo;
        }

        public String getCodigo() { return codigo; }
        public void setCodigo(String codigo) { this.codigo = codigo; }
        public String getNombre() { return nombre; }
        public void setNombre(String nombre) { this.nombre = nombre; }
        public double getPrecio() { return precio; }
        public void setPrecio(double precio) { this.precio = precio; }
        public String getDepartamento() { return departamento; }
        public void setDepartamento(String departamento) { this.departamento = departamento; }
        public int getStock() { return stock; }
        public void setStock(int stock) { this.stock = stock; }
        public int getStockMinimo() { return stockMinimo; }
        public void setStockMinimo(int stockMinimo) { this.stockMinimo = stockMinimo; }
    }

    public static class Movimiento {
        private String fecha;
        private String codigoProducto;
        private String nombreProducto;
        private String tipo; // ENTRADA o SALIDA
        private int cantidadAnterior;
        private int cantidadMovimiento;
        private String departamento;

        public Movimiento(String fecha, String codigoProducto, String nombreProducto, String tipo, int cantidadAnterior, int cantidadMovimiento, String departamento) {
            this.fecha = fecha;
            this.codigoProducto = codigoProducto;
            this.nombreProducto = nombreProducto;
            this.tipo = tipo;
            this.cantidadAnterior = cantidadAnterior;
            this.cantidadMovimiento = cantidadMovimiento;
            this.departamento = departamento;
        }

        public String getFecha() { return fecha; }
        public String getCodigoProducto() { return codigoProducto; }
        public String getNombreProducto() { return nombreProducto; }
        public String getTipo() { return tipo; }
        public int getCantidadAnterior() { return cantidadAnterior; }
        public int getCantidadMovimiento() { return cantidadMovimiento; }
        public String getDepartamento() { return departamento; }
    }

    // ==========================================
    // VALIDACIÓN DE ENTRADAS
    // Cada método devuelve el valor ya limpio o lanza IllegalArgumentException
    // con un mensaje listo para mostrar al usuario.
    // ==========================================
    public static class Validador {
        public static final int MAX_TEXTO = 60;
        public static final int MAX_CANTIDAD = 1_000_000;
        public static final double MAX_PRECIO = 1_000_000;

        public static String codigo(String valor) {
            String c = valor == null ? "" : valor.strip().toUpperCase(Locale.ROOT);
            if (!c.matches("[A-Z0-9-]{1,20}")) {
                throw new IllegalArgumentException("El código solo admite letras, números y guion (máximo 20 caracteres).");
            }
            return c;
        }

        public static String texto(String valor, String campo) {
            String t = valor == null ? "" : valor.strip();
            if (t.isEmpty()) throw new IllegalArgumentException(campo + " es obligatorio.");
            if (t.length() > MAX_TEXTO) throw new IllegalArgumentException(campo + " no puede superar " + MAX_TEXTO + " caracteres.");
            if (t.codePoints().anyMatch(Character::isISOControl)) {
                throw new IllegalArgumentException(campo + " contiene caracteres no válidos.");
            }
            return t;
        }

        public static double precio(String valor) {
            String texto = valor == null ? "" : valor.strip();
            if (!texto.matches("\\$?(?:[0-9]+(?:[.,][0-9]{1,2})?|[.,][0-9]{1,2})")) {
                throw new IllegalArgumentException("Escriba un precio con hasta dos decimales (ejemplo: 125.50 o 125,50).");
            }
            double p = Double.parseDouble(texto.replace("$", "").replace(',', '.'));
            if (p <= 0 || p > MAX_PRECIO) {
                throw new IllegalArgumentException("El precio debe ser mayor a 0 y no superar " + (long) MAX_PRECIO + ".");
            }
            return p;
        }

        public static int entero(String valor, String campo, int minimo) {
            int n;
            try {
                n = Integer.parseInt(valor == null ? "" : valor.strip());
            } catch (NumberFormatException e) {
                throw new IllegalArgumentException(campo + " debe ser un número entero.");
            }
            if (n < minimo || n > MAX_CANTIDAD) {
                throw new IllegalArgumentException(campo + " debe estar entre " + minimo + " y " + MAX_CANTIDAD + ".");
            }
            return n;
        }
    }

    // ==========================================
    // BASE DE DATOS EN MEMORIA Y CONFIGURACIÓN
    // ==========================================
    public static List<Producto> listaProductos = new ArrayList<>();
    public static List<Movimiento> listaMovimientos = new ArrayList<>();
    public static List<String> listaDepartamentos = new ArrayList<>();

    static {
        listaDepartamentos.add("Cómics");
        listaDepartamentos.add("Libros");
        listaDepartamentos.add("Mangas");
        listaDepartamentos.add("Novelas");
        listaDepartamentos.add("Enciclopedias");

        // Los precios y las existencias son datos de prueba para la actividad.
        listaProductos.add(new Producto("P001", "Boku no Hero", 169.00, "Mangas", 9, 3));
        listaProductos.add(new Producto("P002", "Animal Human", 219.00, "Cómics", 6, 3));
        listaProductos.add(new Producto("P003", "Civil War", 399.00, "Cómics", 2, 3));
        listaProductos.add(new Producto("P004", "Batman: The Killing Joke", 329.00, "Cómics", 5, 2));
        listaProductos.add(new Producto("P005", "Hábitos atómicos", 399.00, "Libros", 10, 3));
        listaProductos.add(new Producto("P006", "La psicología del dinero", 349.00, "Libros", 7, 3));
        listaProductos.add(new Producto("P007", "Sapiens: De animales a dioses", 459.00, "Libros", 6, 2));
        listaProductos.add(new Producto("P008", "One Piece", 159.00, "Mangas", 12, 4));
        listaProductos.add(new Producto("P009", "Dragon Ball", 179.00, "Mangas", 8, 3));
        listaProductos.add(new Producto("P010", "Demon Slayer: Kimetsu no Yaiba", 169.00, "Mangas", 0, 3));
        listaProductos.add(new Producto("P011", "Harry Potter y la piedra filosofal", 429.00, "Novelas", 7, 3));
        listaProductos.add(new Producto("P012", "Cien años de soledad", 329.00, "Novelas", 5, 2));
        listaProductos.add(new Producto("P013", "Orgullo y prejuicio", 249.00, "Novelas", 9, 3));
        listaProductos.add(new Producto("P014", "El libro de la ciencia", 579.00, "Enciclopedias", 4, 2));
        listaProductos.add(new Producto("P015", "Enciclopedia de los dinosaurios", 499.00, "Enciclopedias", 6, 2));
    }

    static Producto buscarPorCodigo(String codigo) {
        for (Producto p : listaProductos) {
            if (p.getCodigo().equalsIgnoreCase(codigo)) return p;
        }
        return null;
    }

    /** Entrecomilla los campos que contengan comas o comillas para no romper el CSV. */
    static String csv(String campo) {
        if (campo.contains(",") || campo.contains("\"") || campo.contains("\n")) {
            return "\"" + campo.replace("\"", "\"\"") + "\"";
        }
        return campo;
    }

    /** Exporta únicamente los movimientos correspondientes a ventas a un archivo CSV. */
    static void exportarVentasCSV(Component parent) {
        List<Movimiento> ventas = new ArrayList<>();
        for (Movimiento m : listaMovimientos) {
            if (m.getTipo().startsWith("SALIDA")) {
                ventas.add(m);
            }
        }

        if (ventas.isEmpty()) {
            JOptionPane.showMessageDialog(parent, "Aún no hay ventas registradas para exportar.", "Sin registros", JOptionPane.WARNING_MESSAGE);
            return;
        }

        File archivo = new File("registro_ventas.csv");
        try (PrintWriter pw = new PrintWriter(archivo, "UTF-8")) {
            pw.println("FechaHora,Codigo,Producto,CantidadVendida,PrecioUnitario,TotalVenta,Categoria");
            for (Movimiento m : ventas) {
                Producto p = buscarPorCodigo(m.getCodigoProducto());
                double precioUnit = (p != null) ? p.getPrecio() : 0.0;
                double totalVenta = precioUnit * m.getCantidadMovimiento();
                pw.println(
                        csv(m.getFecha()) + "," +
                                csv(m.getCodigoProducto()) + "," +
                                csv(m.getNombreProducto()) + "," +
                                m.getCantidadMovimiento() + "," +
                                String.format(Locale.US, "%.2f", precioUnit) + "," +
                                String.format(Locale.US, "%.2f", totalVenta) + "," +
                                csv(m.getDepartamento())
                );
            }
            JOptionPane.showMessageDialog(parent, "✅ Registro de ventas exportado exitosamente en:\n" + archivo.getAbsolutePath());
        } catch (IOException ex) {
            JOptionPane.showMessageDialog(parent, "Error al exportar ventas: " + ex.getMessage(), "Error", JOptionPane.ERROR_MESSAGE);
        }
    }

    // ==========================================
    // MÉTODO PRINCIPAL (PUNTO DE ENTRADA)
    // ==========================================
    private static final Color FONDO = new Color(0xF2F2F2);
    private static final Color BLANCO = Color.WHITE;
    private static final Color CREMA = new Color(0xF3EEEA);
    private static final Color CAMPO = new Color(0xE5E5E5);
    private static final Color BORDE = new Color(0xA9AAAC);
    private static final Color AZUL_CLARO = new Color(0xC4D6E1);
    private static final Color AZUL_TABLA = new Color(0xB6C5CF);
    private static final Color PETROLEO = new Color(0x4E7281);
    private static final Color VINO = new Color(0x690832);
    private static final Color TEXTO = new Color(0x171717);
    private static final Font FUENTE = new Font("SansSerif", Font.PLAIN, 14);

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            aplicarTema();
            new VentanaLogin().setVisible(true);
        });
    }

    private static void aplicarTema() {
        UIManager.put("Panel.background", BLANCO);
        UIManager.put("Label.font", FUENTE);
        UIManager.put("Label.foreground", TEXTO);
        UIManager.put("Button.font", FUENTE.deriveFont(Font.BOLD));
        UIManager.put("Button.background", CAMPO);
        UIManager.put("TextField.font", FUENTE);
        UIManager.put("PasswordField.font", FUENTE);
        UIManager.put("TextField.background", CAMPO);
        UIManager.put("ComboBox.background", CAMPO);
        UIManager.put("ComboBox.font", FUENTE);
        UIManager.put("TabbedPane.font", FUENTE.deriveFont(Font.BOLD));
        UIManager.put("TabbedPane.background", CREMA);
        UIManager.put("TabbedPane.selected", AZUL_CLARO);
        UIManager.put("TabbedPane.contentAreaColor", BLANCO);
        UIManager.put("TabbedPane.tabInsets", new Insets(6, 12, 6, 12));
    }

    private static JButton boton(String texto) {
        JButton boton = new JButton(texto);
        boton.setFont(FUENTE.deriveFont(Font.BOLD));
        boton.setContentAreaFilled(false);
        boton.setOpaque(true);
        boton.setForeground(TEXTO);
        boton.setBackground(CAMPO);
        boton.setBorder(BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(BORDE),
                BorderFactory.createEmptyBorder(6, 10, 6, 10)));
        return boton;
    }

    private static JLabel franja(String texto) {
        JLabel etiqueta = new JLabel(texto);
        etiqueta.setFont(FUENTE.deriveFont(Font.BOLD));
        etiqueta.setOpaque(true);
        etiqueta.setBackground(AZUL_CLARO);
        etiqueta.setBorder(BorderFactory.createEmptyBorder(8, 10, 8, 10));
        return etiqueta;
    }

    private static DefaultTableModel modeloTabla(String[] columnas) {
        return new DefaultTableModel(columnas, 0) {
            @Override
            public boolean isCellEditable(int fila, int columna) {
                return false;
            }
        };
    }

    private static JScrollPane tablaEstilizada(JTable tabla, int... anchos) {
        tabla.setFont(FUENTE);
        tabla.setBackground(AZUL_TABLA);
        tabla.setForeground(TEXTO);
        tabla.setSelectionBackground(AZUL_CLARO);
        tabla.setSelectionForeground(TEXTO);
        tabla.setGridColor(new Color(0xD6E0E5));
        tabla.setRowHeight(29);
        tabla.setFillsViewportHeight(true);
        for (int i = 0; i < anchos.length; i++) {
            tabla.getColumnModel().getColumn(i).setPreferredWidth(anchos[i]);
        }
        DefaultTableCellRenderer encabezado = new DefaultTableCellRenderer() {
            @Override
            public Component getTableCellRendererComponent(JTable tabla, Object valor,
                                                           boolean seleccionado, boolean foco, int fila, int columna) {
                super.getTableCellRendererComponent(tabla, valor, false, false, fila, columna);
                setFont(FUENTE.deriveFont(Font.BOLD));
                setBackground(PETROLEO);
                setForeground(BLANCO);
                setHorizontalAlignment(SwingConstants.CENTER);
                setBorder(BorderFactory.createMatteBorder(0, 0, 0, 1, AZUL_TABLA));
                return this;
            }
        };
        tabla.getTableHeader().setDefaultRenderer(encabezado);
        tabla.getTableHeader().setPreferredSize(new Dimension(tabla.getPreferredSize().width, 34));
        JScrollPane scroll = new JScrollPane(tabla);
        scroll.setColumnHeaderView(tabla.getTableHeader());
        scroll.setBorder(BorderFactory.createEmptyBorder());
        scroll.getViewport().setBackground(AZUL_TABLA);
        return scroll;
    }

    // ==========================================
    // 1. VENTANA DE LOGIN
    // ==========================================
    public static class VentanaLogin extends JFrame {
        private JTextField txtUsuario;
        private JPasswordField txtContrasena;
        private int intentosFallidos = 0;

        public VentanaLogin() {
            setTitle("Biblioteca - Inicio de sesión");
            setSize(470, 320);
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            setLocationRelativeTo(null);
            setResizable(false);

            JPanel panel = new JPanel(null);
            panel.setBackground(new Color(0x93827A));

            JLabel lblTitulo = new JLabel("Ingrese su usuario y contraseña.");
            lblTitulo.setFont(new Font("SansSerif", Font.BOLD, 14));
            lblTitulo.setForeground(TEXTO);
            lblTitulo.setHorizontalAlignment(SwingConstants.CENTER);
            lblTitulo.setBounds(0, 22, 455, 25);
            panel.add(lblTitulo);

            JLabel lblUser = new JLabel("Usuario:");
            lblUser.setForeground(TEXTO);
            lblUser.setBounds(88, 78, 100, 25);
            panel.add(lblUser);

            txtUsuario = new JTextField("admin");
            txtUsuario.setBounds(88, 103, 280, 28);
            txtUsuario.setBackground(BLANCO);
            lblUser.setLabelFor(txtUsuario);
            panel.add(txtUsuario);

            JLabel lblPass = new JLabel("Contraseña:");
            lblPass.setForeground(TEXTO);
            lblPass.setBounds(88, 143, 100, 25);
            panel.add(lblPass);

            txtContrasena = new JPasswordField("admin");
            txtContrasena.setBounds(88, 168, 280, 28);
            txtContrasena.setBackground(BLANCO);
            lblPass.setLabelFor(txtContrasena);
            panel.add(txtContrasena);

            JButton btnIngresar = boton("Ingresar");
            btnIngresar.setBounds(168, 216, 120, 31);
            btnIngresar.setBackground(AZUL_CLARO);
            panel.add(btnIngresar);

            btnIngresar.addActionListener(e -> iniciarSesion());
            txtContrasena.addActionListener(e -> iniciarSesion());
            getRootPane().setDefaultButton(btnIngresar);

            add(panel);
        }

        private void iniciarSesion() {
            String user = txtUsuario.getText().strip();
            String pass = new String(txtContrasena.getPassword());

            if (user.isEmpty() || pass.isBlank()) {
                JOptionPane.showMessageDialog(this, "Escriba su usuario y contraseña.", "Datos incompletos", JOptionPane.WARNING_MESSAGE);
                return;
            }
            if (user.length() > 30 || pass.length() > 50) {
                JOptionPane.showMessageDialog(this, "Usuario o contraseña demasiado largos.", "Datos inválidos", JOptionPane.WARNING_MESSAGE);
                return;
            }

            if (user.equals("admin") && pass.equals("admin")) {
                JOptionPane.showMessageDialog(this, "¡Bienvenido al Sistema!", "Acceso Exitoso", JOptionPane.INFORMATION_MESSAGE);
                new VentanaPrincipal().setVisible(true);
                dispose();
            } else {
                intentosFallidos++;
                txtContrasena.setText("");
                if (intentosFallidos >= 3) {
                    JOptionPane.showMessageDialog(this, "Demasiados intentos fallidos. El programa se cerrará.", "Acceso bloqueado", JOptionPane.ERROR_MESSAGE);
                    System.exit(0);
                }
                JOptionPane.showMessageDialog(this, "Usuario o contraseña incorrectos. Intento " + intentosFallidos + " de 3.", "Error de Autenticación", JOptionPane.ERROR_MESSAGE);
            }
        }
    }

    // ==========================================
    // 2. VENTANA PRINCIPAL (SISTEMA CON PESTAÑAS)
    // ==========================================
    public static class VentanaPrincipal extends JFrame {
        private JTabbedPane pestanas;

        public VentanaPrincipal() {
            setTitle("Biblioteca - Inventario y ventas");
            setSize(1000, 650);
            setMinimumSize(new Dimension(800, 510));
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            setLocationRelativeTo(null);

            pestanas = new JTabbedPane();
            pestanas.setBackground(CREMA);

            // Paneles basados en los diseños de Figma
            PanelCajaVenta panelCaja = new PanelCajaVenta();
            PanelAdministrar panelAdmin = new PanelAdministrar(panelCaja);
            PanelHistorialReportes panelHistorial = new PanelHistorialReportes();

            pestanas.addTab("Caja / Venta", panelCaja);
            pestanas.addTab("Administrar", panelAdmin);
            pestanas.addTab("Historial y Reportes", panelHistorial);

            // Al abrir la pestaña de reportes se muestran los datos más recientes
            pestanas.addChangeListener(e -> {
                if (pestanas.getSelectedComponent() == panelHistorial) panelHistorial.actualizarReportes();
            });

            // Barra superior con el botón de cerrar sesión
            JPanel barraSuperior = new JPanel(new BorderLayout());
            JLabel titulo = new JLabel("Biblioteca · Punto de venta");
            titulo.setFont(FUENTE.deriveFont(Font.BOLD));
            titulo.setBorder(BorderFactory.createEmptyBorder(0, 9, 0, 0));
            barraSuperior.add(titulo, BorderLayout.CENTER);
            JButton btnCerrarSesion = boton("Cerrar sesión");
            btnCerrarSesion.setBackground(VINO);
            btnCerrarSesion.setForeground(BLANCO);
            btnCerrarSesion.addActionListener(e -> cerrarSesion());
            barraSuperior.add(btnCerrarSesion, BorderLayout.EAST);

            JPanel contenido = new JPanel(new BorderLayout());
            contenido.setBackground(FONDO);
            contenido.setBorder(BorderFactory.createEmptyBorder(14, 16, 16, 16));
            contenido.add(barraSuperior, BorderLayout.NORTH);
            contenido.add(pestanas, BorderLayout.CENTER);
            setContentPane(contenido);
        }

        private void cerrarSesion() {
            int opcion = JOptionPane.showConfirmDialog(this,
                    "¿Desea cerrar sesión?\nLa venta que esté en curso sin cobrar se perderá.",
                    "Cerrar sesión", JOptionPane.YES_NO_OPTION, JOptionPane.QUESTION_MESSAGE);
            if (opcion == JOptionPane.YES_OPTION) {
                dispose();
                new VentanaLogin().setVisible(true);
            }
        }
    }

    // ==========================================
    // PANEL 1: CAJA / VENTA
    // ==========================================
    public static class PanelCajaVenta extends JPanel {
        private JTextField txtCodigoScan, txtBusquedaNombre, txtCantidadVenta;
        private DefaultTableModel modeloTablaVenta;
        private JTable tablaVenta;
        private JLabel lblTotalPagar;
        private List<ItemVenta> carrito = new ArrayList<>();

        public static class ItemVenta {
            Producto producto;
            int cantidad;
            public ItemVenta(Producto p, int c) { this.producto = p; this.cantidad = c; }
            public double getSubtotal() { return producto.getPrecio() * cantidad; }
        }

        public PanelCajaVenta() {
            setLayout(new BorderLayout());
            setBackground(BLANCO);

            // Panel Superior: Búsqueda y Código
            JPanel panelTop = new JPanel(new BorderLayout());
            JPanel busqueda = new JPanel(new GridLayout(2, 1, 0, 6));
            busqueda.setBorder(BorderFactory.createEmptyBorder(8, 8, 8, 8));
            JPanel campos = new JPanel(new GridLayout(2, 1, 0, 6));
            campos.setBorder(BorderFactory.createEmptyBorder(8, 0, 8, 8));

            JLabel lblCodigo = new JLabel("Código del producto");
            JLabel lblBusqueda = new JLabel("Buscar por nombre");
            txtCodigoScan = new JTextField(27);
            txtBusquedaNombre = new JTextField(27);
            lblCodigo.setLabelFor(txtCodigoScan);
            lblBusqueda.setLabelFor(txtBusquedaNombre);
            busqueda.add(lblCodigo);
            busqueda.add(lblBusqueda);
            campos.add(txtCodigoScan);
            campos.add(txtBusquedaNombre);

            JPanel filaBusqueda = new JPanel(new BorderLayout());
            filaBusqueda.add(busqueda, BorderLayout.WEST);
            filaBusqueda.add(campos, BorderLayout.CENTER);
            panelTop.add(filaBusqueda, BorderLayout.WEST);
            add(panelTop, BorderLayout.NORTH);

            // Tabla central de venta
            String[] columnas = {"Código", "Producto", "Precio", "Cantidad", "Existencia", "Categoría"};
            modeloTablaVenta = modeloTabla(columnas);
            tablaVenta = new JTable(modeloTablaVenta);
            add(tablaEstilizada(tablaVenta, 76, 280, 90, 90, 95, 120), BorderLayout.CENTER);

            // Panel Inferior: Cobro y Acciones
            JPanel panelBottom = new JPanel(new BorderLayout());

            JPanel subPanelAcciones = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 13));
            subPanelAcciones.add(new JLabel("Cantidad:"));
            txtCantidadVenta = new JTextField("1", 5);
            subPanelAcciones.add(txtCantidadVenta);

            JButton btnAgregarCarrito = boton("Agregar a Venta");
            subPanelAcciones.add(btnAgregarCarrito);

            JButton btnQuitar = boton("Quitar seleccionado");
            subPanelAcciones.add(btnQuitar);

            JButton btnCobrar = boton("Cobrar");
            btnCobrar.setFont(FUENTE.deriveFont(Font.BOLD, 16f));
            panelBottom.add(subPanelAcciones, BorderLayout.WEST);

            lblTotalPagar = new JLabel("$00.00", JLabel.CENTER);
            lblTotalPagar.setFont(FUENTE.deriveFont(Font.BOLD, 28f));
            lblTotalPagar.setPreferredSize(new Dimension(175, 45));
            lblTotalPagar.setBorder(BorderFactory.createLineBorder(BORDE));
            JPanel cobro = new JPanel(new FlowLayout(FlowLayout.RIGHT, 10, 9));
            cobro.add(btnCobrar);
            cobro.add(lblTotalPagar);
            panelBottom.add(cobro, BorderLayout.EAST);

            add(panelBottom, BorderLayout.SOUTH);

            // Eventos
            btnAgregarCarrito.addActionListener(e -> agregarItem());
            txtCodigoScan.addActionListener(e -> agregarItem());
            txtBusquedaNombre.addActionListener(e -> agregarItem());
            btnQuitar.addActionListener(e -> quitarSeleccionado());
            btnCobrar.addActionListener(e -> procesarCobro());
        }

        private void agregarItem() {
            String codigo = txtCodigoScan.getText().strip();
            String nombreBusqueda = txtBusquedaNombre.getText().strip();

            if (codigo.isEmpty() && nombreBusqueda.isEmpty()) {
                JOptionPane.showMessageDialog(this, "Escriba un código o parte del nombre del producto.", "Aviso", JOptionPane.WARNING_MESSAGE);
                return;
            }

            int cantidad;
            try {
                if (!codigo.isEmpty()) {
                    codigo = Validador.codigo(codigo);
                } else {
                    nombreBusqueda = Validador.texto(nombreBusqueda, "La búsqueda").toLowerCase(Locale.ROOT);
                }
                cantidad = Validador.entero(txtCantidadVenta.getText(), "La cantidad", 1);
            } catch (IllegalArgumentException e) {
                JOptionPane.showMessageDialog(this, e.getMessage(), "Datos de venta inválidos", JOptionPane.ERROR_MESSAGE);
                return;
            }

            // Si se escribe código se busca por código; si no, por nombre
            Producto productoEncontrado = null;
            if (!codigo.isEmpty()) {
                productoEncontrado = buscarPorCodigo(codigo);
            } else {
                for (Producto p : listaProductos) {
                    if (p.getNombre().toLowerCase(Locale.ROOT).contains(nombreBusqueda)) {
                        productoEncontrado = p;
                        break;
                    }
                }
            }

            if (productoEncontrado == null) {
                JOptionPane.showMessageDialog(this, "Producto no encontrado.", "Aviso", JOptionPane.WARNING_MESSAGE);
                return;
            }

            if (productoEncontrado.getStock() == 0) {
                JOptionPane.showMessageDialog(this, "El producto [" + productoEncontrado.getNombre() + "] está agotado.", "Producto agotado", JOptionPane.WARNING_MESSAGE);
                return;
            }

            // Validar existencia suficiente, contando lo que ya está en la venta actual
            int yaEnVenta = 0;
            for (ItemVenta item : carrito) {
                if (item.producto == productoEncontrado) yaEnVenta += item.cantidad;
            }
            if (yaEnVenta + cantidad > productoEncontrado.getStock()) {
                JOptionPane.showMessageDialog(this, "⚠️ Alerta: ¡Cantidad mayor a la existente en stock!\nStock disponible: " + productoEncontrado.getStock()
                        + (yaEnVenta > 0 ? "\nYa agregó en esta venta: " + yaEnVenta : ""), "Alerta de Inventario", JOptionPane.WARNING_MESSAGE);
                return;
            }

            carrito.add(new ItemVenta(productoEncontrado, cantidad));
            modeloTablaVenta.addRow(new Object[]{
                    productoEncontrado.getCodigo(),
                    productoEncontrado.getNombre(),
                    String.format("$%.2f", productoEncontrado.getPrecio()),
                    cantidad,
                    productoEncontrado.getStock(),
                    productoEncontrado.getDepartamento()
            });

            recalcularTotal();
            txtCodigoScan.setText("");
            txtBusquedaNombre.setText("");
            txtCantidadVenta.setText("1");
        }

        private void quitarSeleccionado() {
            int fila = tablaVenta.getSelectedRow();
            if (fila < 0) {
                JOptionPane.showMessageDialog(this, "Seleccione la línea de la venta que desea quitar.", "Aviso", JOptionPane.WARNING_MESSAGE);
                return;
            }
            carrito.remove(fila);
            modeloTablaVenta.removeRow(fila);
            recalcularTotal();
        }

        private void recalcularTotal() {
            double total = 0;
            for (ItemVenta item : carrito) {
                total += item.getSubtotal();
            }
            lblTotalPagar.setText(String.format("$%.2f", total));
        }

        private void procesarCobro() {
            if (carrito.isEmpty()) {
                JOptionPane.showMessageDialog(this, "No hay productos en la venta actual.", "Aviso", JOptionPane.WARNING_MESSAGE);
                return;
            }

            // Se revisa todo ANTES de descontar: si algo falla, no se modifica nada.
            Map<Producto, Integer> porProducto = new LinkedHashMap<>();
            for (ItemVenta item : carrito) {
                porProducto.merge(item.producto, item.cantidad, Integer::sum);
            }
            for (Map.Entry<Producto, Integer> e : porProducto.entrySet()) {
                Producto p = e.getKey();
                if (!listaProductos.contains(p)) {
                    JOptionPane.showMessageDialog(this, "El producto [" + p.getNombre() + "] ya no existe en el inventario.\nQuítelo de la venta.", "Venta no válida", JOptionPane.ERROR_MESSAGE);
                    return;
                }
                if (e.getValue() > p.getStock()) {
                    JOptionPane.showMessageDialog(this, "⚠️ Alerta: ¡Cantidad mayor a la existente en stock!\n[" + p.getNombre() + "] tiene " + p.getStock() + " y se piden " + e.getValue() + ".", "Alerta de Inventario", JOptionPane.WARNING_MESSAGE);
                    return;
                }
            }

            SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
            String fechaHora = sdf.format(new Date());
            List<String> alertas = new ArrayList<>();

            for (ItemVenta item : carrito) {
                int stockAnterior = item.producto.getStock();
                item.producto.setStock(stockAnterior - item.cantidad);

                // Registrar en el historial de movimientos
                listaMovimientos.add(new Movimiento(
                        fechaHora,
                        item.producto.getCodigo(),
                        item.producto.getNombre(),
                        "SALIDA (Venta)",
                        stockAnterior,
                        item.cantidad,
                        item.producto.getDepartamento()
                ));
            }

            // Alerta de reabastecimiento, una sola vez por producto
            for (Producto p : porProducto.keySet()) {
                if (p.getStock() <= p.getStockMinimo()) {
                    alertas.add("• " + p.getNombre() + " (stock actual: " + p.getStock() + ")");
                }
            }

            JOptionPane.showMessageDialog(this, "✅ ¡Registro exitoso! Venta procesada correctamente.", "Éxito", JOptionPane.INFORMATION_MESSAGE);
            if (!alertas.isEmpty()) {
                JOptionPane.showMessageDialog(this, "⚠️ Alerta de Reabastecimiento. Por agotarse:\n" + String.join("\n", alertas), "Alerta de Stock Bajo", JOptionPane.WARNING_MESSAGE);
            }
            carrito.clear();
            modeloTablaVenta.setRowCount(0);
            lblTotalPagar.setText("$00.00");
        }
    }

    // ==========================================
    // PANEL 2: ADMINISTRAR PRODUCTOS Y DEPARTAMENTOS
    // ==========================================
    public static class PanelAdministrar extends JPanel {
        private JTabbedPane subPestanasAdmin;
        private JTextField txtRegCodigo, txtRegNombre, txtRegPrecio, txtRegStock, txtRegMinimo;
        private JComboBox<String> cmbRegDepartamento;
        private DefaultTableModel modeloTablaAdmin;
        private JTable tablaAdmin;
        private PanelCajaVenta panelCajaRef;

        public PanelAdministrar(PanelCajaVenta panelCaja) {
            this.panelCajaRef = panelCaja;
            setLayout(new BorderLayout());

            subPestanasAdmin = new JTabbedPane();

            // Pestaña 1: Registrar Producto
            JPanel vistaRegistro = new JPanel(new BorderLayout());
            vistaRegistro.add(franja("Registrar producto"), BorderLayout.NORTH);
            JPanel zonaRegistro = new JPanel(new FlowLayout(FlowLayout.LEFT, 14, 12));
            vistaRegistro.add(zonaRegistro, BorderLayout.CENTER);
            JPanel panelRegistrar = new JPanel(new GridBagLayout());
            zonaRegistro.add(panelRegistrar);

            txtRegCodigo = new JTextField(23);
            txtRegNombre = new JTextField(23);
            txtRegPrecio = new JTextField(23);
            cmbRegDepartamento = new JComboBox<>();
            actualizarComboDeptos();
            txtRegStock = new JTextField();
            txtRegMinimo = new JTextField("3");

            campoRegistro(panelRegistrar, 0, "Código", txtRegCodigo);
            campoRegistro(panelRegistrar, 1, "Nombre", txtRegNombre);
            campoRegistro(panelRegistrar, 2, "Precio ($)", txtRegPrecio);
            campoRegistro(panelRegistrar, 3, "Categoría", cmbRegDepartamento);
            campoRegistro(panelRegistrar, 4, "", franja("Inventario"));
            campoRegistro(panelRegistrar, 5, "Cantidad actual", txtRegStock);
            campoRegistro(panelRegistrar, 6, "Inventario mínimo", txtRegMinimo);
            JButton btnGuardar = boton("Guardar producto");
            campoRegistro(panelRegistrar, 7, "", btnGuardar);

            subPestanasAdmin.addTab("Registrar", vistaRegistro);

            // Pestaña 2: Listar / Modificar / Eliminar / Entradas
            JPanel panelGestion = new JPanel(new BorderLayout());
            String[] cols = {"Código", "Nombre", "Precio", "Stock", "Stock Mín.", "Categoría"};
            modeloTablaAdmin = modeloTabla(cols);
            tablaAdmin = new JTable(modeloTablaAdmin);
            actualizarTablaInventario();
            panelGestion.add(franja("Inventario"), BorderLayout.NORTH);
            panelGestion.add(tablaEstilizada(tablaAdmin, 80, 225, 90, 95, 100, 150), BorderLayout.CENTER);

            JPanel panelBotonesAccion = new JPanel();
            JButton btnModificar = boton("Modificar Stock / Precio");
            JButton btnEliminar = boton("Eliminar Producto");
            JButton btnEntradaMercancia = boton("Registrar Entrada de Mercancía");
            panelBotonesAccion.add(btnModificar);
            panelBotonesAccion.add(btnEliminar);
            panelBotonesAccion.add(btnEntradaMercancia);
            panelGestion.add(panelBotonesAccion, BorderLayout.SOUTH);

            subPestanasAdmin.addTab("Inventario & Modificaciones", panelGestion);

            // Pestaña 3: Categorías
            JPanel panelDeptos = new JPanel(new BorderLayout());
            JTextField txtNuevoDepto = new JTextField(15);
            JButton btnGuardarDepto = boton("Guardar categoría");
            JPanel pDeptoForm = new JPanel(new GridBagLayout());
            campoRegistro(pDeptoForm, 0, "", new JLabel("Nueva categoría"));
            campoRegistro(pDeptoForm, 1, "Nombre", txtNuevoDepto);
            campoRegistro(pDeptoForm, 2, "", btnGuardarDepto);
            panelDeptos.add(franja("Categorías"), BorderLayout.NORTH);
            JPanel zonaDeptos = new JPanel(new FlowLayout(FlowLayout.LEFT, 12, 9));
            zonaDeptos.add(pDeptoForm);
            panelDeptos.add(zonaDeptos, BorderLayout.CENTER);

            JTextArea txtAreaDeptos = new JTextArea();
            txtAreaDeptos.setEditable(false);
            actualizarListaDeptosText(txtAreaDeptos);
            txtAreaDeptos.setBackground(AZUL_TABLA);
            txtAreaDeptos.setFont(FUENTE);
            txtAreaDeptos.setBorder(BorderFactory.createEmptyBorder(7, 8, 7, 8));
            JScrollPane listaDeptos = new JScrollPane(txtAreaDeptos);
            listaDeptos.setPreferredSize(new Dimension(225, 0));
            listaDeptos.setBorder(BorderFactory.createEmptyBorder());
            panelDeptos.add(listaDeptos, BorderLayout.WEST);

            subPestanasAdmin.addTab("Categorías", panelDeptos);

            // Pestaña 4: Importar / Exportar Datos
            JPanel panelIO = new JPanel(new GridBagLayout());
            JButton btnExportarJSON = boton("Exportar inventario a CSV");
            JButton btnExportarVentasCSV = boton("Exportar ventas a CSV");
            JButton btnImportarJSON = boton("Importar Datos de Inventario");
            panelIO.add(btnExportarJSON);
            panelIO.add(new JLabel("   "));
            panelIO.add(btnExportarVentasCSV);
            panelIO.add(new JLabel("   "));
            panelIO.add(btnImportarJSON);
            subPestanasAdmin.addTab("Importar / Exportar", panelIO);

            add(subPestanasAdmin, BorderLayout.CENTER);

            // ACCIONES
            btnGuardar.addActionListener(e -> {
                try {
                    String codigo = Validador.codigo(txtRegCodigo.getText());
                    String nombre = Validador.texto(txtRegNombre.getText(), "El nombre");
                    double precio = Validador.precio(txtRegPrecio.getText());
                    String depto = (String) cmbRegDepartamento.getSelectedItem();
                    if (depto == null) throw new IllegalArgumentException("Registre primero una categoría.");
                    int stock = Validador.entero(txtRegStock.getText(), "La cantidad actual", 0);
                    int min = Validador.entero(txtRegMinimo.getText(), "El stock mínimo", 0);

                    if (buscarPorCodigo(codigo) != null) {
                        throw new IllegalArgumentException("Ya existe un producto con el código " + codigo + ".");
                    }

                    listaProductos.add(new Producto(codigo, nombre, precio, depto, stock, min));
                    actualizarTablaInventario();
                    JOptionPane.showMessageDialog(this, "✅ Producto registrado exitosamente.");
                    txtRegCodigo.setText(""); txtRegNombre.setText(""); txtRegPrecio.setText(""); txtRegStock.setText("");
                } catch (IllegalArgumentException ex) {
                    JOptionPane.showMessageDialog(this, ex.getMessage(), "Datos inválidos", JOptionPane.ERROR_MESSAGE);
                }
            });

            btnGuardarDepto.addActionListener(e -> {
                try {
                    String nuevo = Validador.texto(txtNuevoDepto.getText(), "El nombre de la categoría");
                    for (String d : listaDepartamentos) {
                        if (d.equalsIgnoreCase(nuevo)) throw new IllegalArgumentException("La categoría \"" + d + "\" ya existe.");
                    }
                    listaDepartamentos.add(nuevo);
                    actualizarComboDeptos();
                    actualizarListaDeptosText(txtAreaDeptos);
                    txtNuevoDepto.setText("");
                    JOptionPane.showMessageDialog(this, "Categoría guardada con éxito.");
                } catch (IllegalArgumentException ex) {
                    JOptionPane.showMessageDialog(this, ex.getMessage(), "Datos inválidos", JOptionPane.ERROR_MESSAGE);
                }
            });

            btnModificar.addActionListener(e -> {
                int fila = tablaAdmin.getSelectedRow();
                if (fila < 0) {
                    JOptionPane.showMessageDialog(this, "Seleccione un producto de la tabla para modificar.");
                    return;
                }
                Producto p = listaProductos.get(fila);
                String nuevoPrecioStr = JOptionPane.showInputDialog(this, "Nuevo precio para [" + p.getNombre() + "]:", p.getPrecio());
                if (nuevoPrecioStr != null) {
                    try {
                        p.setPrecio(Validador.precio(nuevoPrecioStr));
                        actualizarTablaInventario();
                        JOptionPane.showMessageDialog(this, "Modificación guardada.");
                    } catch (IllegalArgumentException ex) {
                        JOptionPane.showMessageDialog(this, ex.getMessage(), "Datos inválidos", JOptionPane.ERROR_MESSAGE);
                    }
                }
            });

            btnEliminar.addActionListener(e -> {
                int fila = tablaAdmin.getSelectedRow();
                if (fila < 0) {
                    JOptionPane.showMessageDialog(this, "Seleccione un producto a eliminar.");
                    return;
                }
                Producto p = listaProductos.get(fila);
                int opcion = JOptionPane.showConfirmDialog(this, "¿Eliminar [" + p.getCodigo() + " - " + p.getNombre() + "]?",
                        "Confirmar eliminación", JOptionPane.YES_NO_OPTION, JOptionPane.WARNING_MESSAGE);
                if (opcion == JOptionPane.YES_OPTION) {
                    listaProductos.remove(fila);
                    actualizarTablaInventario();
                    JOptionPane.showMessageDialog(this, "Producto eliminado.");
                }
            });

            btnEntradaMercancia.addActionListener(e -> {
                int fila = tablaAdmin.getSelectedRow();
                if (fila < 0) {
                    JOptionPane.showMessageDialog(this, "Seleccione el producto al que desea registrar entrada de mercancía.");
                    return;
                }
                Producto p = listaProductos.get(fila);
                String cantStr = JOptionPane.showInputDialog(this, "Cantidad de mercancía a ingresar para [" + p.getNombre() + "]:");
                if (cantStr != null) {
                    try {
                        int cantidadEntrada = Validador.entero(cantStr, "La cantidad", 1);
                        int stockAnt = p.getStock();
                        if ((long) stockAnt + cantidadEntrada > Validador.MAX_CANTIDAD) {
                            throw new IllegalArgumentException("La existencia resultante no puede superar " + Validador.MAX_CANTIDAD + ".");
                        }
                        p.setStock(stockAnt + cantidadEntrada);

                        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
                        listaMovimientos.add(new Movimiento(
                                sdf.format(new Date()), p.getCodigo(), p.getNombre(), "ENTRADA (Compra)", stockAnt, cantidadEntrada, p.getDepartamento()
                        ));

                        actualizarTablaInventario();
                        JOptionPane.showMessageDialog(this, "✅ Entrada de mercancía registrada con éxito. Stock actual: " + p.getStock());
                    } catch (IllegalArgumentException ex) {
                        JOptionPane.showMessageDialog(this, ex.getMessage(), "Datos inválidos", JOptionPane.ERROR_MESSAGE);
                    }
                }
            });

            btnExportarJSON.addActionListener(e -> exportarDatosCSV());
            btnExportarVentasCSV.addActionListener(e -> exportarVentasCSV(this));
            btnImportarJSON.addActionListener(e -> JOptionPane.showMessageDialog(this, "Simulación de importación masiva completada con éxito."));
        }

        private void campoRegistro(JPanel panel, int fila, String titulo, Component campo) {
            GridBagConstraints c = new GridBagConstraints();
            c.gridx = 0;
            c.gridy = fila;
            c.anchor = GridBagConstraints.WEST;
            c.insets = new Insets(4, 5, 4, 5);
            if (!titulo.isEmpty()) {
                JLabel etiqueta = new JLabel(titulo);
                etiqueta.setLabelFor(campo);
                panel.add(etiqueta, c);
            }
            c.gridx = 1;
            c.fill = GridBagConstraints.HORIZONTAL;
            panel.add(campo, c);
        }

        public void actualizarComboDeptos() {
            cmbRegDepartamento.removeAllItems();
            for (String d : listaDepartamentos) {
                cmbRegDepartamento.addItem(d);
            }
        }

        private void actualizarListaDeptosText(JTextArea area) {
            area.setText("--- Categorías registradas ---\n");
            for (String d : listaDepartamentos) {
                area.append("• " + d + "\n");
            }
        }

        public void actualizarTablaInventario() {
            modeloTablaAdmin.setRowCount(0);
            for (Producto p : listaProductos) {
                modeloTablaAdmin.addRow(new Object[]{p.getCodigo(), p.getNombre(), "$" + p.getPrecio(), p.getStock(), p.getStockMinimo(), p.getDepartamento()});
            }
        }

        private void exportarDatosCSV() {
            File archivo = new File("inventario_exportado.csv");
            try (PrintWriter pw = new PrintWriter(archivo, "UTF-8")) {
                pw.println("Codigo,Nombre,Precio,Stock,StockMinimo,Departamento");
                for (Producto p : listaProductos) {
                    pw.println(csv(p.getCodigo()) + "," + csv(p.getNombre()) + "," + p.getPrecio() + "," + p.getStock() + "," + p.getStockMinimo() + "," + csv(p.getDepartamento()));
                }
                JOptionPane.showMessageDialog(this, "✅ Datos exportados exitosamente en: " + archivo.getAbsolutePath());
            } catch (IOException ex) {
                JOptionPane.showMessageDialog(this, "Error al exportar: " + ex.getMessage(), "Error", JOptionPane.ERROR_MESSAGE);
            }
        }
    }

    // ==========================================
    // PANEL 3: HISTORIAL Y REPORTES
    // ==========================================
    public static class PanelHistorialReportes extends JPanel {
        private JTabbedPane subPestanasHistorial;
        private DefaultTableModel modeloBajos, modeloReporteInv, modeloMovimientos;
        private JLabel lblCostoTotalInv, lblCantidadTotalProd;

        public PanelHistorialReportes() {
            setLayout(new BorderLayout());

            subPestanasHistorial = new JTabbedPane();

            // 1. Reporte: Productos Bajos en Inventario / Agotados
            JPanel panelBajosInv = new JPanel(new BorderLayout());
            modeloBajos = modeloTabla(new String[]{"Código", "Nombre", "Precio", "Existencia", "Inv. Mínimo", "Categoría"});
            JTable tablaBajos = new JTable(modeloBajos);
            panelBajosInv.add(franja("Productos bajos en inventario"), BorderLayout.NORTH);
            panelBajosInv.add(tablaEstilizada(tablaBajos, 75, 210, 85, 100, 105, 130), BorderLayout.CENTER);
            JButton btnActualizarBajos = boton("Actualizar Reporte de Alertas");
            JPanel accionesBajos = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 7));
            accionesBajos.add(btnActualizarBajos);
            panelBajosInv.add(accionesBajos, BorderLayout.SOUTH);
            subPestanasHistorial.addTab("Productos Bajos / Agotados", panelBajosInv);

            // 2. Reporte: Inventario General y Valor Total
            JPanel panelInvGeneral = new JPanel(new BorderLayout());
            JPanel panelInfoValores = new JPanel(new GridLayout(1, 2));
            lblCostoTotalInv = new JLabel("Costo Total: $0.00", JLabel.CENTER);
            lblCostoTotalInv.setFont(new Font("SansSerif", Font.BOLD, 16));
            lblCantidadTotalProd = new JLabel("Total Productos: 0", JLabel.CENTER);
            lblCantidadTotalProd.setFont(new Font("SansSerif", Font.BOLD, 16));
            panelInfoValores.add(lblCostoTotalInv);
            panelInfoValores.add(lblCantidadTotalProd);
            panelInfoValores.setBorder(BorderFactory.createEmptyBorder(9, 8, 9, 8));
            JPanel cabeceraReporte = new JPanel(new BorderLayout());
            cabeceraReporte.add(franja("Reporte de inventario"), BorderLayout.NORTH);
            cabeceraReporte.add(panelInfoValores, BorderLayout.CENTER);
            panelInvGeneral.add(cabeceraReporte, BorderLayout.NORTH);

            modeloReporteInv = modeloTabla(new String[]{"Código", "Nombre", "Precio", "Existencia", "Valor en Inventario", "Categoría"});
            JTable tablaInvGen = new JTable(modeloReporteInv);
            panelInvGeneral.add(tablaEstilizada(tablaInvGen, 75, 195, 90, 90, 145, 130), BorderLayout.CENTER);
            subPestanasHistorial.addTab("Reporte de Inventario", panelInvGeneral);

            // 3. Reporte de Movimientos
            JPanel panelMovs = new JPanel(new BorderLayout());
            modeloMovimientos = modeloTabla(new String[]{"Fecha y Hora", "Código", "Producto", "Tipo Movimiento", "Stock Anterior", "Cantidad", "Categoría"});
            JTable tablaMovs = new JTable(modeloMovimientos);
            panelMovs.add(franja("Reporte de movimientos"), BorderLayout.NORTH);
            panelMovs.add(tablaEstilizada(tablaMovs, 145, 75, 180, 145, 115, 85, 130), BorderLayout.CENTER);
            JButton btnExportarVentas = boton("Exportar ventas a CSV");
            btnExportarVentas.addActionListener(e -> exportarVentasCSV(this));
            JPanel accionesMovs = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 7));
            accionesMovs.add(btnExportarVentas);
            panelMovs.add(accionesMovs, BorderLayout.SOUTH);
            subPestanasHistorial.addTab("Reporte de Movimientos", panelMovs);

            add(subPestanasHistorial, BorderLayout.CENTER);

            // Cargar datos iniciales en reportes
            actualizarReportes();

            btnActualizarBajos.addActionListener(e -> actualizarReportes());
        }

        public void actualizarReportes() {
            // Bajos o agotados
            modeloBajos.setRowCount(0);
            for (Producto p : listaProductos) {
                if (p.getStock() <= p.getStockMinimo()) {
                    modeloBajos.addRow(new Object[]{p.getCodigo(), p.getNombre(), "$" + p.getPrecio(), p.getStock(), p.getStockMinimo(), p.getDepartamento()});
                }
            }

            // General y Valor total
            modeloReporteInv.setRowCount(0);
            double costoTotal = 0;
            int totalUnidades = 0;
            for (Producto p : listaProductos) {
                double valorProd = p.getPrecio() * p.getStock();
                costoTotal += valorProd;
                totalUnidades += p.getStock();
                modeloReporteInv.addRow(new Object[]{p.getCodigo(), p.getNombre(), "$" + p.getPrecio(), p.getStock(), String.format("$%.2f", valorProd), p.getDepartamento()});
            }
            lblCostoTotalInv.setText(String.format("Costo Total Almacenado: $%.2f", costoTotal));
            lblCantidadTotalProd.setText("Total Unidades en Existencia: " + totalUnidades);

            // Movimientos
            modeloMovimientos.setRowCount(0);
            for (Movimiento m : listaMovimientos) {
                modeloMovimientos.addRow(new Object[]{m.getFecha(), m.getCodigoProducto(), m.getNombreProducto(), m.getTipo(), m.getCantidadAnterior(), m.getCantidadMovimiento(), m.getDepartamento()});
            }
        }
    }
}