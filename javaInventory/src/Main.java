import javax.swing.*;
import javax.swing.table.DefaultTableModel;
import java.awt.*;
import java.io.*;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

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
    // BASE DE DATOS EN MEMORIA Y CONFIGURACIÓN
    // ==========================================
    public static List<Producto> listaProductos = new ArrayList<>();
    public static List<Movimiento> listaMovimientos = new ArrayList<>();
    public static List<String> listaDepartamentos = new ArrayList<>();

    static {
        // Datos iniciales de prueba
        listaDepartamentos.add("Electrónica");
        listaDepartamentos.add("Línea Blanca");
        listaDepartamentos.add("Abarrotes");

        listaProductos.add(new Producto("P001", "Laptop HP 15\"", 12500.00, "Electrónica", 8, 3));
        listaProductos.add(new Producto("P002", "Mouse Inalámbrico", 350.00, "Electrónica", 2, 5)); // Bajo inventario
        listaProductos.add(new Producto("P003", "Refrigerador LG", 9800.50, "Línea Blanca", 0, 2)); // Agotado
        listaProductos.add(new Producto("P004", "Cafetera de Grano", 1450.00, "Línea Blanca", 12, 4));
    }

    // ==========================================
    // MÉTODO PRINCIPAL (PUNTO DE ENTRADA)
    // ==========================================
    public static void main(String[] args) {
        // Establecer apariencia nativa de la plataforma o Nimbus
        try {
            for (UIManager.LookAndFeelInfo info : UIManager.getInstalledLookAndFeels()) {
                if ("Nimbus".equals(info.getName())) {
                    UIManager.setLookAndFeel(info.getClassName());
                    break;
                }
            }
        } catch (Exception ignored) {}

        SwingUtilities.invokeLater(() -> new VentanaLogin().setVisible(true));
    }

    // ==========================================
    // 1. VENTANA DE LOGIN
    // ==========================================
    public static class VentanaLogin extends JFrame {
        private JTextField txtUsuario;
        private JPasswordField txtContrasena;

        public VentanaLogin() {
            setTitle("Punto de venta - Inicio de sesión");
            setSize(450, 320);
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            setLocationRelativeTo(null);
            setResizable(false);

            JPanel panel = new JPanel(null);
            panel.setBackground(new Color(145, 128, 128)); // Tono similar a Figma

            JLabel lblTitulo = new JLabel("Ingrese su usuario y contraseña.");
            lblTitulo.setFont(new Font("SansSerif", Font.BOLD, 14));
            lblTitulo.setForeground(Color.WHITE);
            lblTitulo.setBounds(110, 30, 250, 25);
            panel.add(lblTitulo);

            JLabel lblUser = new JLabel("Usuario:");
            lblUser.setForeground(Color.WHITE);
            lblUser.setBounds(80, 80, 100, 25);
            panel.add(lblUser);

            txtUsuario = new JTextField("admin");
            txtUsuario.setBounds(80, 105, 280, 30);
            panel.add(txtUsuario);

            JLabel lblPass = new JLabel("Contraseña:");
            lblPass.setForeground(Color.WHITE);
            lblPass.setBounds(80, 145, 100, 25);
            panel.add(lblPass);

            txtContrasena = new JPasswordField("admin");
            txtContrasena.setBounds(80, 170, 280, 30);
            panel.add(txtContrasena);

            JButton btnIngresar = new JButton("Ingresar");
            btnIngresar.setBounds(160, 215, 120, 30);
            panel.add(btnIngresar);

            JButton btnNuevoUsuario = new JButton("Crear nuevo usuario");
            btnNuevoUsuario.setFont(new Font("SansSerif", Font.PLAIN, 10));
            btnNuevoUsuario.setBounds(150, 255, 140, 22);
            panel.add(btnNuevoUsuario);

            btnIngresar.addActionListener(e -> {
                String user = txtUsuario.getText();
                String pass = new String(txtContrasena.getPassword());
                if (user.equals("admin") && pass.equals("admin")) {
                    JOptionPane.showMessageDialog(this, "¡Bienvenido al Sistema!", "Acceso Exitoso", JOptionPane.INFORMATION_MESSAGE);
                    new VentanaPrincipal().setVisible(true);
                    dispose();
                } else {
                    JOptionPane.showMessageDialog(this, "Usuario o contraseña incorrectos.", "Error de Autenticación", JOptionPane.ERROR_MESSAGE);
                }
            });

            btnNuevoUsuario.addActionListener(e ->
                    JOptionPane.showMessageDialog(this, "Módulo de creación de usuarios deshabilitado en esta versión demo.", "Información", JOptionPane.INFORMATION_MESSAGE)
            );

            add(panel);
        }
    }

    // ==========================================
    // 2. VENTANA PRINCIPAL (SISTEMA CON PESTAÑAS)
    // ==========================================
    public static class VentanaPrincipal extends JFrame {
        private JTabbedPane pestanas;

        public VentanaPrincipal() {
            setTitle("Sistema Integral de Inventario y Ventas");
            setSize(950, 650);
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            setLocationRelativeTo(null);

            pestanas = new JTabbedPane();
            pestanas.setFont(new Font("SansSerif", Font.BOLD, 12));

            // Paneles basados en los diseños de Figma
            PanelCajaVenta panelCaja = new PanelCajaVenta();
            PanelAdministrar panelAdmin = new PanelAdministrar(panelCaja);
            PanelHistorialReportes panelHistorial = new PanelHistorialReportes();

            pestanas.addTab("Caja / Venta", panelCaja);
            pestanas.addTab("Administrar", panelAdmin);
            pestanas.addTab("Historial y Reportes", panelHistorial);

            add(pestanas);
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
            setBackground(new Color(245, 240, 235));

            // Panel Superior: Búsqueda y Código
            JPanel panelTop = new JPanel(new GridLayout(2, 2, 5, 5));
            panelTop.setBorder(BorderFactory.createTitledBorder("Procesamiento de Venta / Salida"));

            panelTop.add(new JLabel("Código del producto:"));
            txtCodigoScan = new JTextField();
            panelTop.add(txtCodigoScan);

            panelTop.add(new JLabel("Buscar producto por nombre:"));
            txtBusquedaNombre = new JTextField();
            panelTop.add(txtBusquedaNombre);

            add(panelTop, BorderLayout.NORTH);

            // Tabla central de venta
            String[] columnas = {"Código", "Producto", "Precio", "Cantidad a Vender", "Existencia Actual", "Categoría"};
            modeloTablaVenta = new DefaultTableModel(columnas, 0);
            tablaVenta = new JTable(modeloTablaVenta);
            add(new JScrollPane(tablaVenta), BorderLayout.CENTER);

            // Panel Inferior: Cobro y Acciones
            JPanel panelBottom = new JPanel(new BorderLayout());

            JPanel subPanelAcciones = new JPanel(new FlowLayout(FlowLayout.LEFT));
            subPanelAcciones.add(new JLabel("Cantidad:"));
            txtCantidadVenta = new JTextField("1", 5);
            subPanelAcciones.add(txtCantidadVenta);

            JButton btnAgregarCarrito = new JButton("Agregar a Venta");
            subPanelAcciones.add(btnAgregarCarrito);

            JButton btnCobrar = new JButton("Cobrar / Salida");
            btnCobrar.setFont(new Font("SansSerif", Font.BOLD, 14));
            btnCobrar.setBackground(new Color(220, 220, 220));
            subPanelAcciones.add(btnCobrar);

            panelBottom.add(subPanelAcciones, BorderLayout.WEST);

            lblTotalPagar = new JLabel("$00.00", JLabel.RIGHT);
            lblTotalPagar.setFont(new Font("SansSerif", Font.BOLD, 28));
            lblTotalPagar.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 20));
            panelBottom.add(lblTotalPagar, BorderLayout.EAST);

            add(panelBottom, BorderLayout.SOUTH);

            // Eventos
            btnAgregarCarrito.addActionListener(e -> agregarItem());
            txtCodigoScan.addActionListener(e -> agregarItem());
            btnCobrar.addActionListener(e -> procesarCobro());
        }

        private void agregarItem() {
            String codigo = txtCodigoScan.getText().trim();
            String nombreBusqueda = txtBusquedaNombre.getText().trim().toLowerCase();
            int cantidad;

            try {
                cantidad = Integer.parseInt(txtCantidadVenta.getText().trim());
            } catch (NumberFormatException e) {
                JOptionPane.showMessageDialog(this, "Ingrese una cantidad válida.", "Error", JOptionPane.ERROR_MESSAGE);
                return;
            }

            Producto productoEncontrado = null;
            for (Producto p : listaProductos) {
                if (!codigo.isEmpty() && p.getCodigo().equalsIgnoreCase(codigo)) {
                    productoEncontrado = p;
                    break;
                }
                if (!nombreBusqueda.isEmpty() && p.getNombre().toLowerCase().contains(nombreBusqueda)) {
                    productoEncontrado = p;
                    break;
                }
            }

            if (productoEncontrado == null) {
                JOptionPane.showMessageDialog(this, "Producto no encontrado.", "Aviso", JOptionPane.WARNING_MESSAGE);
                return;
            }

            // Validar existencia suficiente
            if (cantidad > productoEncontrado.getStock()) {
                JOptionPane.showMessageDialog(this, "⚠️ Alerta: ¡Cantidad mayor a la existente en stock!\nStock disponible: " + productoEncontrado.getStock(), "Alerta de Inventario", JOptionPane.WARNING_MESSAGE);
                return;
            }

            carrito.add(new ItemVenta(productoEncontrado, cantidad));
            modeloTablaVenta.addRow(new Object[]{
                    productoEncontrado.getCodigo(),
                    productoEncontrado.getNombre(),
                    "$" + productoEncontrado.getPrecio(),
                    cantidad,
                    productoEncontrado.getStock(),
                    productoEncontrado.getDepartamento()
            });

            recalcularTotal();
            txtCodigoScan.setText("");
            txtBusquedaNombre.setText("");
            txtCantidadVenta.setText("1");
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

            SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
            String fechaHora = sdf.format(new Date());

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

                // Verificar alerta de reabastecimiento
                if (item.producto.getStock() <= item.producto.getStockMinimo()) {
                    JOptionPane.showMessageDialog(this, "⚠️ Alerta de Reabastecimiento:\nEl producto [" + item.producto.getNombre() + "] está por agotarse (Stock actual: " + item.producto.getStock() + ").", "Alerta de Stock Bajo", JOptionPane.WARNING_MESSAGE);
                }
            }

            JOptionPane.showMessageDialog(this, "✅ ¡Registro exitoso! Venta procesada correctamente.", "Éxito", JOptionPane.INFORMATION_MESSAGE);
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
            JPanel panelRegistrar = new JPanel(new GridLayout(7, 2, 10, 10));
            panelRegistrar.setBorder(BorderFactory.createEmptyBorder(20, 20, 20, 20));

            txtRegCodigo = new JTextField();
            txtRegNombre = new JTextField();
            txtRegPrecio = new JTextField();
            cmbRegDepartamento = new JComboBox<>();
            actualizarComboDeptos();
            txtRegStock = new JTextField();
            txtRegMinimo = new JTextField("3");

            panelRegistrar.add(new JLabel("Código:")); panelRegistrar.add(txtRegCodigo);
            panelRegistrar.add(new JLabel("Nombre:")); panelRegistrar.add(txtRegNombre);
            panelRegistrar.add(new JLabel("Precio ($):")); panelRegistrar.add(txtRegPrecio);
            panelRegistrar.add(new JLabel("Departamento:")); panelRegistrar.add(cmbRegDepartamento);
            panelRegistrar.add(new JLabel("Cantidad Actual (Stock):")); panelRegistrar.add(txtRegStock);
            panelRegistrar.add(new JLabel("Stock Mínimo (Alerta):")); panelRegistrar.add(txtRegMinimo);

            JButton btnGuardar = new JButton("Guardar producto");
            panelRegistrar.add(new JLabel("")); panelRegistrar.add(btnGuardar);

            subPestanasAdmin.addTab("Registrar", panelRegistrar);

            // Pestaña 2: Listar / Modificar / Eliminar / Entradas
            JPanel panelGestion = new JPanel(new BorderLayout());
            String[] cols = {"Código", "Nombre", "Precio", "Stock", "Stock Mín.", "Departamento"};
            modeloTablaAdmin = new DefaultTableModel(cols, 0);
            tablaAdmin = new JTable(modeloTablaAdmin);
            actualizarTablaInventario();
            panelGestion.add(new JScrollPane(tablaAdmin), BorderLayout.CENTER);

            JPanel panelBotonesAccion = new JPanel();
            JButton btnModificar = new JButton("Modificar Stock / Precio");
            JButton btnEliminar = new JButton("Eliminar Producto");
            JButton btnEntradaMercancia = new JButton("Registrar Entrada de Mercancía");
            panelBotonesAccion.add(btnModificar);
            panelBotonesAccion.add(btnEliminar);
            panelBotonesAccion.add(btnEntradaMercancia);
            panelGestion.add(panelBotonesAccion, BorderLayout.SOUTH);

            subPestanasAdmin.addTab("Inventario & Modificaciones", panelGestion);

            // Pestaña 3: Departamentos
            JPanel panelDeptos = new JPanel(new BorderLayout());
            JTextField txtNuevoDepto = new JTextField(15);
            JButton btnGuardarDepto = new JButton("Guardar Departamento");
            JPanel pDeptoForm = new JPanel();
            pDeptoForm.add(new JLabel("Nombre del Departamento:"));
            pDeptoForm.add(txtNuevoDepto);
            pDeptoForm.add(btnGuardarDepto);
            panelDeptos.add(pDeptoForm, BorderLayout.NORTH);

            JTextArea txtAreaDeptos = new JTextArea();
            txtAreaDeptos.setEditable(false);
            actualizarListaDeptosText(txtAreaDeptos);
            panelDeptos.add(new JScrollPane(txtAreaDeptos), BorderLayout.CENTER);

            subPestanasAdmin.addTab("Departamentos", panelDeptos);

            // Pestaña 4: Importar / Exportar Datos
            JPanel panelIO = new JPanel(new GridBagLayout());
            JButton btnExportarJSON = new JButton("Exportar Datos a JSON / CSV");
            JButton btnImportarJSON = new JButton("Importar Datos de Inventario");
            panelIO.add(btnExportarJSON);
            panelIO.add(new JLabel("   "));
            panelIO.add(btnImportarJSON);
            subPestanasAdmin.addTab("Importar / Exportar", panelIO);

            add(subPestanasAdmin, BorderLayout.CENTER);

            // ACCIONES
            btnGuardar.addActionListener(e -> {
                try {
                    String codigo = txtRegCodigo.getText().trim();
                    String nombre = txtRegNombre.getText().trim();
                    double precio = Double.parseDouble(txtRegPrecio.getText().trim());
                    String depto = (String) cmbRegDepartamento.getSelectedItem();
                    int stock = Integer.parseInt(txtRegStock.getText().trim());
                    int min = Integer.parseInt(txtRegMinimo.getText().trim());

                    if (codigo.isEmpty() || nombre.isEmpty()) {
                        JOptionPane.showMessageDialog(this, "Complete todos los campos obligatorios.", "Error", JOptionPane.ERROR_MESSAGE);
                        return;
                    }

                    listaProductos.add(new Producto(codigo, nombre, precio, depto, stock, min));
                    actualizarTablaInventario();
                    JOptionPane.showMessageDialog(this, "✅ Producto registrado exitosamente.");
                    txtRegCodigo.setText(""); txtRegNombre.setText(""); txtRegPrecio.setText(""); txtRegStock.setText("");
                } catch (NumberFormatException ex) {
                    JOptionPane.showMessageDialog(this, "Verifique que precio y stock sean numéricos.", "Error", JOptionPane.ERROR_MESSAGE);
                }
            });

            btnGuardarDepto.addActionListener(e -> {
                String nuevo = txtNuevoDepto.getText().trim();
                if (!nuevo.isEmpty() && !listaDepartamentos.contains(nuevo)) {
                    listaDepartamentos.add(nuevo);
                    actualizarComboDeptos();
                    actualizarListaDeptosText(txtAreaDeptos);
                    txtNuevoDepto.setText("");
                    JOptionPane.showMessageDialog(this, "Departamento guardado con éxito.");
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
                    p.setPrecio(Double.parseDouble(nuevoPrecioStr));
                    actualizarTablaInventario();
                    JOptionPane.showMessageDialog(this, "Modificación guardada.");
                }
            });

            btnEliminar.addActionListener(e -> {
                int fila = tablaAdmin.getSelectedRow();
                if (fila < 0) {
                    JOptionPane.showMessageDialog(this, "Seleccione un producto a eliminar.");
                    return;
                }
                listaProductos.remove(fila);
                actualizarTablaInventario();
                JOptionPane.showMessageDialog(this, "Producto eliminado.");
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
                        int cantidadEntrada = Integer.parseInt(cantStr);
                        int stockAnt = p.getStock();
                        p.setStock(stockAnt + cantidadEntrada);

                        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
                        listaMovimientos.add(new Movimiento(
                                sdf.format(new Date()), p.getCodigo(), p.getNombre(), "ENTRADA (Compra)", stockAnt, cantidadEntrada, p.getDepartamento()
                        ));

                        actualizarTablaInventario();
                        JOptionPane.showMessageDialog(this, "✅ Entrada de mercancía registrada con éxito. Stock actual: " + p.getStock());
                    } catch (NumberFormatException ex) {
                        JOptionPane.showMessageDialog(this, "Cantidad inválida.", "Error", JOptionPane.ERROR_MESSAGE);
                    }
                }
            });

            btnExportarJSON.addActionListener(e -> exportarDatosCSV());
            btnImportarJSON.addActionListener(e -> JOptionPane.showMessageDialog(this, "Simulación de importación masiva completada con éxito."));
        }

        public void actualizarComboDeptos() {
            cmbRegDepartamento.removeAllItems();
            for (String d : listaDepartamentos) {
                cmbRegDepartamento.addItem(d);
            }
        }

        private void actualizarListaDeptosText(JTextArea area) {
            area.setText("--- Departamentos Registrados ---\n");
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
            try {
                File archivo = new File("inventario_exportado.csv");
                PrintWriter pw = new PrintWriter(archivo);
                pw.println("Codigo,Nombre,Precio,Stock,StockMinimo,Departamento");
                for (Producto p : listaProductos) {
                    pw.println(p.getCodigo() + "," + p.getNombre() + "," + p.getPrecio() + "," + p.getStock() + "," + p.getStockMinimo() + "," + p.getDepartamento());
                }
                pw.close();
                JOptionPane.showMessageDialog(this, "✅ Datos exportados exitosamente en: " + archivo.getAbsolutePath());
            } catch (Exception ex) {
                JOptionPane.showMessageDialog(this, "Error al exportar: " + ex.getMessage());
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
            modeloBajos = new DefaultTableModel(new String[]{"Código", "Nombre", "Precio", "Existencia", "Inv. Mínimo", "Departamento"}, 0);
            JTable tablaBajos = new JTable(modeloBajos);
            panelBajosInv.add(new JScrollPane(tablaBajos), BorderLayout.CENTER);
            JButton btnActualizarBajos = new JButton("Actualizar Reporte de Alertas");
            panelBajosInv.add(btnActualizarBajos, BorderLayout.SOUTH);
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
            panelInvGeneral.add(panelInfoValores, BorderLayout.NORTH);

            modeloReporteInv = new DefaultTableModel(new String[]{"Código", "Nombre", "Precio", "Existencia", "Valor en Inventario", "Departamento"}, 0);
            JTable tablaInvGen = new JTable(modeloReporteInv);
            panelInvGeneral.add(new JScrollPane(tablaInvGen), BorderLayout.CENTER);
            subPestanasHistorial.addTab("Reporte de Inventario", panelInvGeneral);

            // 3. Reporte de Movimientos
            JPanel panelMovs = new JPanel(new BorderLayout());
            modeloMovimientos = new DefaultTableModel(new String[]{"Fecha y Hora", "Código", "Producto", "Tipo Movimiento", "Stock Anterior", "Cantidad", "Departamento"}, 0);
            JTable tablaMovs = new JTable(modeloMovimientos);
            panelMovs.add(new JScrollPane(tablaMovs), BorderLayout.CENTER);
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
                modeloReporteInv.addRow(new Object[]{p.getCodigo(), p.getNombre(), "$" + p.getPrecio(), p.getStock(), "$" + valorProd, p.getDepartamento()});
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