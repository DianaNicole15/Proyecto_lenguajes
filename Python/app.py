import csv
import math
from datetime import datetime
from pathlib import Path


class Inventario:
    def __init__(self, carpeta='datos'):
        self.carpeta = Path(carpeta)
        self.carpeta.mkdir(parents=True, exist_ok=True)
        self.productos = {}
        self.movimientos = []
        self.departamentos = ['General', 'Abarrotes', 'Bebidas', 'Limpieza']
        self.cargar()

    COLUMNAS_PRODUCTOS = ['codigo', 'nombre', 'precio', 'depto', 'cantidad', 'minimo']
    COLUMNAS_MOVIMIENTOS = ['fecha', 'codigo', 'nombre', 'tipo', 'cantidad',
                            'antes', 'despues', 'precio', 'depto', 'detalle']

    @staticmethod
    def escribir_csv(destino, columnas, filas):
        destino = Path(destino)
        temporal = destino.with_suffix('.tmp')
        try:
            with temporal.open('w', newline='', encoding='utf-8-sig') as archivo:
                writer = csv.DictWriter(archivo, fieldnames=columnas, extrasaction='ignore')
                writer.writeheader()
                writer.writerows(filas)
            temporal.replace(destino)
        finally:
            if temporal.exists():
                temporal.unlink()

    @staticmethod
    def cargar_csv(ruta):
        ruta = Path(ruta)
        if not ruta.exists():
            return []
        with ruta.open('r', newline='', encoding='utf-8-sig') as archivo:
            lector = csv.DictReader(archivo)
            if not lector.fieldnames:
                raise ValueError(f'Archivo CSV sin encabezados: {ruta.name}')
            return list(lector)

    def cargar(self):
        productos = self.cargar_csv(self.carpeta / 'productos.csv')
        self.productos = {}
        for fila in productos:
            producto = self.validar_producto(fila)
            if producto['codigo'] in self.productos:
                raise ValueError('Código duplicado en productos.csv')
            self.productos[producto['codigo']] = producto
        movimientos = self.cargar_csv(self.carpeta / 'movimientos.csv')
        self.movimientos = []
        for fila in movimientos:
            registro = dict(fila)
            for campo in ('cantidad', 'antes', 'despues'):
                registro[campo] = int(registro[campo])
            registro['precio'] = float(registro['precio'])
            self.movimientos.append(registro)
        departamentos = self.cargar_csv(self.carpeta / 'departamentos.csv')
        nombres = [fila.get('nombre', '').strip() for fila in departamentos]
        nombres.extend(p['depto'] for p in self.productos.values())
        self.departamentos = sorted(set(['General'] + [n for n in nombres if n]))

    def guardar(self):
        self.escribir_csv(self.carpeta / 'productos.csv', self.COLUMNAS_PRODUCTOS,
                          self.productos.values())
        self.escribir_csv(self.carpeta / 'movimientos.csv', self.COLUMNAS_MOVIMIENTOS,
                          self.movimientos)
        self.escribir_csv(self.carpeta / 'departamentos.csv', ['nombre'],
                          [{'nombre': nombre} for nombre in self.departamentos])

    @staticmethod
    def validar_producto(p):
        if not isinstance(p, dict):
            raise ValueError('El producto debe ser un objeto')
        codigo = str(p.get('codigo', '')).strip()
        nombre = str(p.get('nombre', '')).strip()
        depto = str(p.get('depto', p.get('categoria', 'General'))).strip()
        if not codigo or not nombre or not depto:
            raise ValueError('Código, nombre y departamento son obligatorios')
        try:
            precio = float(p.get('precio', 0))
            cantidad = int(p.get('cantidad', p.get('existencia', 0)))
            minimo = int(p.get('minimo', 5))
        except (TypeError, ValueError) as exc:
            raise ValueError('Precio, existencia y mínimo deben ser números válidos') from exc
        if not math.isfinite(precio) or precio < 0 or cantidad < 0 or minimo < 0:
            raise ValueError('Precio, existencia y mínimo deben ser no negativos y finitos')
        for clave in ('cantidad', 'existencia', 'minimo'):
            if clave in p and isinstance(p[clave], (float, str)):
                try:
                    n = float(p[clave])
                except (ValueError, TypeError) as exc:
                    raise ValueError('Cantidad inválida') from exc
                if not n.is_integer():
                    raise ValueError('Las existencias deben ser enteras')
        return {'codigo': codigo, 'nombre': nombre, 'precio': round(precio, 2),
                'depto': depto, 'cantidad': cantidad, 'minimo': minimo}

    def registrar(self, codigo, nombre, precio, depto, cantidad, minimo=5):
        p = self.validar_producto(dict(codigo=codigo, nombre=nombre, precio=precio,
                                       depto=depto, cantidad=cantidad, minimo=minimo))
        if p['codigo'] in self.productos:
            raise ValueError('Ya existe un producto con este código')
        self.productos[p['codigo']] = p
        self.agregar_departamento(p['depto'], persistir=False)
        self.guardar()
        return p

    def modificar(self, codigo, nombre, precio, depto, minimo, cantidad=None):
        if codigo not in self.productos:
            raise ValueError('Producto inexistente')
        anterior = self.productos[codigo]
        cantidad_anterior = anterior['cantidad']
        cantidad_nueva = cantidad_anterior if cantidad is None else cantidad
        p = self.validar_producto(dict(codigo=codigo, nombre=nombre, precio=precio,
                                       depto=depto, cantidad=cantidad_nueva, minimo=minimo))
        # Registrar el ajuste en el historial, incluso si la existencia llega a cero.
        diferencia = p['cantidad'] - cantidad_anterior
        p['cantidad'] = cantidad_anterior
        self.productos[codigo] = p
        self.agregar_departamento(p['depto'], persistir=False)
        if diferencia:
            self.movimiento(codigo, 'entrada' if diferencia > 0 else 'salida',
                            abs(diferencia), 'Ajuste desde Modificar producto', persistir=False)
        self.guardar()
        return p

    def eliminar(self, codigo):
        if codigo not in self.productos:
            raise ValueError('Producto inexistente')
        if self.productos[codigo]['cantidad'] > 0:
            raise ValueError('Primero deje las existencias en cero antes de eliminarlo')
        del self.productos[codigo]
        self.guardar()

    def agregar_departamento(self, nombre, persistir=True):
        nombre = str(nombre).strip()
        if not nombre:
            raise ValueError('Escribe un departamento')
        if nombre not in self.departamentos:
            self.departamentos.append(nombre)
            self.departamentos.sort()
            if persistir:
                self.guardar()

    def buscar(self, texto='', departamento='Todos', estado='Todos', ordenar='codigo'):
        filtro = texto.casefold().strip()
        items = [p for p in self.productos.values()
                 if (filtro in p['codigo'].casefold() or filtro in p['nombre'].casefold()
                     or filtro in p['depto'].casefold())
                 and (departamento == 'Todos' or p['depto'] == departamento)
                 and (estado == 'Todos' or
                      estado == 'Disponibles' and p['cantidad'] > 0 or
                      estado == 'Agotados' and p['cantidad'] == 0 or
                      estado == 'Bajo inventario' and 0 < p['cantidad'] <= p['minimo'])]
        opciones = {'codigo': lambda x: x['codigo'], 'nombre': lambda x: x['nombre'].casefold(),
                    'precio': lambda x: x['precio'], 'existencia': lambda x: x['cantidad'],
                    'categoria': lambda x: x['depto'].casefold(),
                    'valor': lambda x: x['precio'] * x['cantidad']}
        if ordenar not in opciones:
            raise ValueError('Orden inválido')
        return sorted(items, key=opciones[ordenar], reverse=ordenar in ('precio', 'existencia', 'valor'))

    def movimiento(self, codigo, tipo, cantidad, detalle='Manual', persistir=True):
        if codigo not in self.productos:
            raise ValueError('Producto inexistente')
        if tipo not in ('entrada', 'salida', 'venta'):
            raise ValueError('Tipo de movimiento inválido')
        if isinstance(cantidad, bool):
            raise ValueError('Cantidad inválida')
        try:
            n = int(cantidad)
            if float(cantidad) != n:
                raise ValueError
        except (TypeError, ValueError, OverflowError) as exc:
            raise ValueError('La cantidad debe ser un número entero') from exc
        if n <= 0:
            raise ValueError('La cantidad debe ser mayor que cero')
        p = self.productos[codigo]
        if tipo != 'entrada' and n > p['cantidad']:
            raise ValueError(f'Inventario insuficiente: disponibles {p["cantidad"]} unidades')
        antes = p['cantidad']
        p['cantidad'] += n if tipo == 'entrada' else -n
        registro = dict(fecha=datetime.now().isoformat(timespec='seconds'), codigo=codigo,
                        nombre=p['nombre'], tipo=tipo, cantidad=n, antes=antes,
                        despues=p['cantidad'], precio=p['precio'], depto=p['depto'], detalle=detalle)
        self.movimientos.append(registro)
        if persistir:
            self.guardar()
        return registro

    def procesar_lote(self, movimientos):
        if not isinstance(movimientos, list):
            raise ValueError('Se esperaba una lista de movimientos')
        resultados = []
        for i, m in enumerate(movimientos, 1):
            try:
                r = self.movimiento(m['codigo'], m['tipo'], m['cantidad'],
                                    m.get('detalle', 'Importado'), persistir=False)
                resultados.append({'fila': i, 'estado': 'Aceptado', 'detalle': r['codigo']})
            except (ValueError, KeyError, TypeError) as exc:
                resultados.append({'fila': i, 'estado': 'Rechazado', 'detalle': str(exc)})
        self.guardar()
        return resultados

    def total(self):
        return round(sum(p['precio'] * p['cantidad'] for p in self.productos.values()), 2)

    def exportar(self, ruta, tipo='productos'):
        ruta = Path(ruta)
        if ruta.suffix.lower() != '.csv':
            raise ValueError('Solo se permiten archivos CSV')
        if tipo == 'productos':
            columnas, filas = self.COLUMNAS_PRODUCTOS, self.productos.values()
        elif tipo == 'movimientos':
            columnas, filas = self.COLUMNAS_MOVIMIENTOS, self.movimientos
        else:
            raise ValueError('Tipo de exportación inválido')
        self.escribir_csv(ruta, columnas, filas)

    @staticmethod
    def leer_registros(ruta):
        ruta = Path(ruta)
        if ruta.suffix.lower() != '.csv':
            raise ValueError('Solo puedes importar archivos CSV')
        with ruta.open('r', newline='', encoding='utf-8-sig') as archivo:
            lector = csv.DictReader(archivo)
            if not lector.fieldnames:
                raise ValueError('El CSV necesita una fila de encabezados')
            return list(lector)

    def importar_productos(self, ruta):
        registros = self.leer_registros(ruta)
        nuevos = [self.validar_producto(x) for x in registros]
        codigos = [x['codigo'] for x in nuevos]
        if len(codigos) != len(set(codigos)) or any(c in self.productos for c in codigos):
            raise ValueError('Existen códigos duplicados en el archivo o inventario')
        for p in nuevos:
            self.productos[p['codigo']] = p
            self.agregar_departamento(p['depto'], persistir=False)
        self.guardar()
        return len(nuevos)


# --- INTERFAZ GRAFICA ---
import hashlib
import hmac
import os
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox, ttk


BLANCO = '#ffffff'
AZUL = '#c8d6e0'
AZUL_OSCURO = '#577587'
GRIS = '#d9d9d9'
CREMA = '#f5efe9'
VINO = '#720039'
MARRON = '#98877f'


class SistemaPOS:
    def __init__(self, root):
        self.root = root
        self.root.minsize(860, 560)
        self.datos = Inventario(Path(__file__).resolve().parent / 'datos')
        self.usuarios_path = Path(__file__).resolve().parent / 'datos' / 'usuarios_locales.csv'
        self.carrito = {}
        self.configurar_estilos()
        self.mostrar_login()

    def configurar_estilos(self):
        estilo = ttk.Style()
        estilo.theme_use('clam')
        estilo.configure('Treeview.Heading', background=AZUL_OSCURO, foreground='white', font=('Arial', 9, 'bold'))
        estilo.configure('Treeview', background='#bccbd5', fieldbackground='#bccbd5', rowheight=25)
        estilo.map('Treeview', background=[('selected', AZUL_OSCURO)], foreground=[('selected', 'white')])

    def limpiar(self, titulo, login=False):
        for w in self.root.winfo_children():
            w.destroy()
        self.root.title('StockControl - ' + titulo)
        self.root.geometry('950x640' if not login else '500x390')
        self.root.configure(bg=MARRON if login else BLANCO)
        if not login:
            self.nav()

    def boton(self, padre, texto, comando, **kw):
        opciones = dict(bg=GRIS, relief='flat', padx=12, pady=7, cursor='hand2', font=('Arial', 9))
        opciones.update(kw)
        return tk.Button(padre, text=texto, command=comando, **opciones)

    def titulo(self, padre, texto):
        tk.Label(padre, text='  ' + texto, bg=AZUL, anchor='w', font=('Arial', 11, 'bold'), pady=6).pack(fill='x', pady=(0, 12))

    def nav(self):
        barra = tk.Frame(self.root, bg=CREMA, pady=8, padx=10)
        barra.pack(fill='x')
        for t, f in [('Caja / venta', self.mostrar_caja), ('Administrar', self.mostrar_administrar),
                     ('Historial', self.mostrar_historial), ('Inventario', self.mostrar_inventario)]:
            self.boton(barra, t, f, bg='white', padx=10, pady=5).pack(side='left', padx=3)
        self.boton(barra, 'Cerrar sesión', self.mostrar_login, bg='#f8d7da', padx=7, pady=5).pack(side='right')

    def marco(self):
        f = tk.Frame(self.root, bg=BLANCO, padx=17, pady=12)
        f.pack(fill='both', expand=True)
        return f

    def crear_tabla(self, padre, columnas, alturas=13):
        cont = tk.Frame(padre, bg=BLANCO)
        cont.pack(fill='both', expand=True, pady=8)
        tabla = ttk.Treeview(cont, columns=columnas, show='headings', height=alturas)
        for c in columnas:
            tabla.heading(c, text=c)
            tabla.column(c, width=120, anchor='center', minwidth=80)
        sc = ttk.Scrollbar(cont, orient='vertical', command=tabla.yview)
        tabla.configure(yscrollcommand=sc.set)
        tabla.pack(side='left', fill='both', expand=True)
        sc.pack(side='right', fill='y')
        return tabla

    def gestionar_error(self, accion):
        try:
            return accion()
        except (ValueError, KeyError, TypeError, OSError, csv.Error) as exc:
            messagebox.showerror('Aviso', str(exc))
            return None

    def leer_usuarios(self):
        if not self.usuarios_path.exists():
            return {}
        with self.usuarios_path.open(newline='', encoding='utf-8-sig') as archivo:
            return {fila['usuario']: {'sal': fila['sal'], 'hash': fila['hash']}
                    for fila in csv.DictReader(archivo)}

    def mostrar_login(self):
        self.limpiar('Inicio de sesión', login=True)
        tk.Label(self.root, text='Punto de venta - Inicio de sesión', bg=MARRON,
                 font=('Arial', 14, 'bold')).pack(pady=(30, 12))
        tk.Label(self.root, text='Ingrese su usuario y contraseña.', bg=MARRON).pack(pady=6)
        f = tk.Frame(self.root, bg=MARRON)
        f.pack(pady=5)
        tk.Label(f, text='Usuario:', bg=MARRON).grid(row=0, column=0, sticky='w', pady=8)
        self.usuario = tk.Entry(f, width=28)
        self.usuario.grid(row=0, column=1, padx=10)
        tk.Label(f, text='Contraseña:', bg=MARRON).grid(row=1, column=0, sticky='w', pady=8)
        self.clave = tk.Entry(f, width=28, show='*')
        self.clave.grid(row=1, column=1, padx=10)
        self.boton(self.root, 'Ingresar', self.ingresar, bg=AZUL, width=19).pack(pady=5)
        self.boton(self.root, 'Crear nuevo usuario', self.registrar_usuario, bg=AZUL, width=19).pack(pady=5)

    def registrar_usuario(self):
        def accion():
            usuario, clave = self.usuario.get().strip(), self.clave.get()
            if len(usuario) < 3 or len(clave) < 8:
                raise ValueError('El usuario debe tener 3 caracteres y la contraseña al menos 8.')
            registros = self.leer_usuarios()
            if usuario in registros:
                raise ValueError('Este usuario ya está registrado')
            sal = os.urandom(16)
            digest = hashlib.pbkdf2_hmac('sha256', clave.encode(), sal, 310000)
            registros[usuario] = {'sal': sal.hex(), 'hash': digest.hex()}
            Inventario.escribir_csv(self.usuarios_path, ['usuario', 'sal', 'hash'],
                                    [{'usuario': nombre, **datos} for nombre, datos in registros.items()])
            messagebox.showinfo('Registro exitoso', 'Usuario registrado. Ahora puedes ingresar.')
        self.gestionar_error(accion)

    def ingresar(self):
        def accion():
            registro = self.leer_usuarios().get(self.usuario.get().strip())
            if not registro:
                raise ValueError('Usuario o contraseña incorrectos')
            sal = bytes.fromhex(registro['sal'])
            digest = hashlib.pbkdf2_hmac('sha256', self.clave.get().encode(), sal, 310000)
            if not hmac.compare_digest(digest, bytes.fromhex(registro['hash'])):
                raise ValueError('Usuario o contraseña incorrectos')
            self.mostrar_caja()
        self.gestionar_error(accion)

    def mostrar_caja(self):
        self.limpiar('Caja / Venta')
        f = self.marco()
        self.titulo(f, 'Caja / Venta')
        buscar = tk.Frame(f, bg=BLANCO)
        buscar.pack(fill='x', pady=7)
        tk.Label(buscar, text='Código o nombre del producto:', bg=BLANCO).pack(side='left')
        self.busca_caja = tk.Entry(buscar, bg=GRIS, width=35)
        self.busca_caja.pack(side='left', padx=8)
        self.busca_caja.bind('<Return>', lambda *_: self.agregar_carrito())
        tk.Label(buscar, text='Cantidad:', bg=BLANCO).pack(side='left', padx=(10, 2))
        self.cant_caja = tk.Spinbox(buscar, from_=1, to=100000, width=7)
        self.cant_caja.pack(side='left')
        self.boton(buscar, 'Agregar', self.agregar_carrito).pack(side='left', padx=8)
        self.boton(buscar, 'Buscar', self.buscar_caja).pack(side='left')
        self.tabla_caja = self.crear_tabla(f, ('Código', 'Producto', 'Precio', 'Cantidad', 'Existencia', 'Categoría'), 12)
        pie = tk.Frame(f, bg=BLANCO)
        pie.pack(fill='x', pady=8)
        self.boton(pie, 'Quitar seleccionado', self.quitar_carrito).pack(side='left')
        self.boton(pie, 'Vaciar carrito', self.vaciar_carrito).pack(side='left', padx=6)
        self.total_caja = tk.Label(pie, text='$0.00', font=('Arial', 21, 'bold'), bg=BLANCO, relief='solid', padx=14)
        self.total_caja.pack(side='right', padx=8)
        self.boton(pie, 'Cobrar', self.cobrar, font=('Arial', 12, 'bold'), padx=18).pack(side='right')
        self.refrescar_carrito()

    def buscar_caja(self):
        palabra = self.busca_caja.get().strip()
        if not palabra:
            return
        encontrados = self.datos.buscar(palabra)
        if not encontrados:
            messagebox.showwarning('Aviso', 'Producto no encontrado')
        elif len(encontrados) == 1:
            self.busca_caja.delete(0, 'end')
            self.busca_caja.insert(0, encontrados[0]['codigo'])
        else:
            messagebox.showinfo('Productos encontrados', '\n'.join(f"{x['codigo']} - {x['nombre']}" for x in encontrados[:15]))

    def agregar_carrito(self):
        def accion():
            codigo = self.busca_caja.get().strip()
            p = self.datos.productos.get(codigo)
            if not p:
                coincidencias = self.datos.buscar(codigo)
                if len(coincidencias) == 1:
                    p = coincidencias[0]
                    codigo = p['codigo']
                else:
                    raise ValueError('Escribe un código válido o busca un producto')
            cantidad = int(self.cant_caja.get())
            if cantidad <= 0 or cantidad + self.carrito.get(codigo, 0) > p['cantidad']:
                raise ValueError(f'Cantidad superior a la existencia disponible: {p["cantidad"]}')
            self.carrito[codigo] = self.carrito.get(codigo, 0) + cantidad
            self.refrescar_carrito()
        self.gestionar_error(accion)

    def refrescar_carrito(self):
        for item in self.tabla_caja.get_children():
            self.tabla_caja.delete(item)
        total = 0
        for cod, cant in self.carrito.items():
            p = self.datos.productos.get(cod)
            if not p:
                continue
            total += p['precio'] * cant
            self.tabla_caja.insert('', 'end', iid=cod, values=(cod, p['nombre'], f"${p['precio']:.2f}", cant, p['cantidad'], p['depto']))
        self.total_caja.config(text=f'${total:,.2f}')

    def quitar_carrito(self):
        for cod in self.tabla_caja.selection():
            self.carrito.pop(cod, None)
        self.refrescar_carrito()

    def vaciar_carrito(self):
        self.carrito.clear()
        self.refrescar_carrito()

    def cobrar(self):
        def accion():
            if not self.carrito:
                raise ValueError('No hay productos en el carrito')
            for cod, cant in self.carrito.items():
                p = self.datos.productos.get(cod)
                if not p or cant > p['cantidad']:
                    raise ValueError(f'Existencia insuficiente para {cod}; actualiza el carrito')
            total = sum(self.datos.productos[c]['precio'] * n for c, n in self.carrito.items())
            if not messagebox.askyesno('Confirmar cobro', f'¿Registrar venta por ${total:,.2f}?'):
                return
            for cod, cant in list(self.carrito.items()):
                self.datos.movimiento(cod, 'venta', cant, 'Caja / Venta', persistir=False)
            self.datos.guardar()
            alertas = [self.datos.productos[c] for c in self.carrito if self.datos.productos[c]['cantidad'] <= self.datos.productos[c]['minimo']]
            self.carrito.clear()
            self.refrescar_carrito()
            messagebox.showinfo('Registro exitoso', f'Venta cobrada: ${total:,.2f}')
            if alertas:
                messagebox.showwarning('Aviso de inventario', 'Bajo inventario o agotados:\n' + '\n'.join(p['nombre'] for p in alertas))
        self.gestionar_error(accion)

    def mostrar_administrar(self):
        self.limpiar('Administrar')
        f = self.marco()
        barra = tk.Frame(f, bg=BLANCO)
        barra.pack(fill='x')
        self.contenedor_admin = tk.Frame(f, bg=BLANCO)
        self.contenedor_admin.pack(fill='both', expand=True, pady=10)
        for txt, fn in [('Registrar', self.subvista_registrar), ('Modificar', self.subvista_modificar),
                        ('Eliminar', self.subvista_eliminar), ('Departamentos', self.subvista_departamentos)]:
            self.boton(barra, txt, fn, width=14).pack(side='left', padx=4)
        self.subvista_registrar()

    def vaciar_admin(self, texto):
        for w in self.contenedor_admin.winfo_children():
            w.destroy()
        self.titulo(self.contenedor_admin, texto)

    def formulario_producto(self, datos=None):
        panel = tk.Frame(self.contenedor_admin, bg=BLANCO)
        panel.pack(anchor='w', padx=25, pady=10)
        entradas = {}
        campos = [('codigo', 'Código'), ('nombre', 'Nombre'), ('precio', 'Precio'),
                  ('depto', 'Departamento'), ('cantidad', 'Cantidad actual'), ('minimo', 'Inventario mínimo')]
        for i, (clave, etiqueta) in enumerate(campos):
            tk.Label(panel, text=etiqueta, bg=BLANCO, width=22, anchor='w', font=('Arial', 9, 'bold')).grid(row=i, column=0, pady=8)
            if clave == 'depto':
                ctrl = ttk.Combobox(panel, values=self.datos.departamentos, width=31, state='readonly')
            else:
                ctrl = tk.Entry(panel, bg=GRIS, relief='flat', width=34)
            ctrl.grid(row=i, column=1, padx=12)
            entradas[clave] = ctrl
            valor = (datos or {}).get(clave, 'General' if clave == 'depto' else '5' if clave == 'minimo' else '0' if clave == 'cantidad' else '')
            if isinstance(ctrl, ttk.Combobox):
                ctrl.set(valor)
            else:
                ctrl.insert(0, str(valor))
        return entradas

    def subvista_registrar(self):
        self.vaciar_admin('Registrar producto')
        entradas = self.formulario_producto()
        def guardar():
            def accion():
                p = {k: v.get().strip() for k, v in entradas.items()}
                self.datos.registrar(**p)
                messagebox.showinfo('Registro exitoso', 'Producto guardado correctamente')
                self.subvista_registrar()
            self.gestionar_error(accion)
        self.boton(self.contenedor_admin, 'Guardar producto', guardar).pack(anchor='w', padx=25)

    def subvista_modificar(self):
        self.vaciar_admin('Modificar producto')
        zona = tk.Frame(self.contenedor_admin, bg=GRIS, padx=20, pady=17)
        zona.pack(anchor='w', padx=25, pady=12)
        tk.Label(zona, text='Código del producto', bg=GRIS).pack()
        codigo = tk.Entry(zona, width=28)
        codigo.pack(pady=6)
        def buscar():
            def accion():
                p = self.datos.productos.get(codigo.get().strip())
                if not p:
                    raise ValueError('Producto inexistente')
                self.vaciar_admin('Modificar producto: ' + p['codigo'])
                entradas = self.formulario_producto(p)
                entradas['codigo'].configure(state='disabled')
                # La cantidad puede editarse, incluso para colocar 0 si se agotó.
                def guardar():
                    def accion_guardar():
                        actualizado = self.datos.modificar(
                            p['codigo'], entradas['nombre'].get(), entradas['precio'].get(),
                            entradas['depto'].get(), entradas['minimo'].get(),
                            entradas['cantidad'].get())
                        mensaje = f"Producto actualizado. Existencia actual: {actualizado['cantidad']}"
                        if actualizado['cantidad'] == 0:
                            mensaje += '\nEl producto quedó agotado.'
                        elif actualizado['cantidad'] <= actualizado['minimo']:
                            mensaje += '\nAviso: producto con bajo inventario.'
                        messagebox.showinfo('Registro exitoso', mensaje)
                        self.subvista_modificar()
                    self.gestionar_error(accion_guardar)
                self.boton(self.contenedor_admin, 'Guardar cambios', guardar).pack(anchor='w', padx=25)
            self.gestionar_error(accion)
        self.boton(zona, 'Aceptar', buscar, bg=CREMA).pack(pady=5)

    def subvista_eliminar(self):
        self.vaciar_admin('Eliminar producto')
        zona = tk.Frame(self.contenedor_admin, bg=GRIS, padx=20, pady=20)
        zona.pack(anchor='w', padx=25, pady=20)
        tk.Label(zona, text='Código del producto', bg=GRIS).pack()
        codigo = tk.Entry(zona, width=28)
        codigo.pack(pady=8)
        def eliminar():
            def accion():
                cod = codigo.get().strip()
                if not messagebox.askyesno('Confirmar', f'¿Eliminar el producto {cod}?'):
                    return
                self.datos.eliminar(cod)
                messagebox.showinfo('Éxito', 'Producto eliminado')
                self.subvista_eliminar()
            self.gestionar_error(accion)
        self.boton(zona, 'Eliminar producto', eliminar, bg='#f8d7da').pack(pady=6)

    def subvista_departamentos(self):
        self.vaciar_admin('Departamentos')
        f = tk.Frame(self.contenedor_admin, bg=BLANCO)
        f.pack(fill='x', padx=25, pady=10)
        lista = tk.Listbox(f, width=35, height=13)
        lista.pack(side='left', padx=(0, 20))
        for dep in self.datos.departamentos:
            lista.insert('end', dep)
        lateral = tk.Frame(f, bg=BLANCO)
        lateral.pack(side='left', anchor='n')
        tk.Label(lateral, text='Nuevo departamento', bg=BLANCO).pack(anchor='w')
        campo = tk.Entry(lateral, bg=GRIS, width=30)
        campo.pack(pady=6)
        def guardar():
            def accion():
                self.datos.agregar_departamento(campo.get())
                messagebox.showinfo('Éxito', 'Departamento guardado')
                self.subvista_departamentos()
            self.gestionar_error(accion)
        self.boton(lateral, 'Guardar', guardar).pack(anchor='w')

    def mostrar_inventario(self):
        self.limpiar('Inventario')
        f = self.marco()
        self.titulo(f, 'Inventario')
        barra = tk.Frame(f, bg=BLANCO)
        barra.pack(fill='x', pady=4)
        tk.Label(barra, text='Buscar:', bg=BLANCO).pack(side='left')
        busca = tk.Entry(barra, width=25, bg=GRIS)
        busca.pack(side='left', padx=5)
        tk.Label(barra, text='Ordenar:', bg=BLANCO).pack(side='left', padx=7)
        orden = ttk.Combobox(barra, values=['codigo', 'nombre', 'precio', 'existencia', 'categoria', 'valor'], width=14, state='readonly')
        orden.set('codigo')
        orden.pack(side='left')
        tabla = self.crear_tabla(f, ('Código', 'Nombre', 'Precio', 'Inventario', 'Inv. mínimo', 'Departamento'))
        def cargar(*_):
            tabla.delete(*tabla.get_children())
            for p in self.datos.buscar(busca.get(), ordenar=orden.get()):
                tabla.insert('', 'end', values=(p['codigo'], p['nombre'], f"${p['precio']:.2f}", p['cantidad'], p['minimo'], p['depto']))
        busca.bind('<KeyRelease>', cargar)
        orden.bind('<<ComboboxSelected>>', cargar)
        self.boton(barra, 'Importar CSV', lambda: self.importar_productos(cargar)).pack(side='right', padx=5)
        self.boton(barra, 'Exportar CSV', lambda: self.exportar('productos')).pack(side='right')
        cargar()

    def importar_productos(self, despues=None):
        ruta = filedialog.askopenfilename(title='Importar productos', filetypes=[('CSV', '*.csv')])
        if ruta:
            def accion():
                n = self.datos.importar_productos(ruta)
                messagebox.showinfo('Importación', f'Se importaron {n} productos')
                if despues:
                    despues()
            self.gestionar_error(accion)

    def exportar(self, tipo):
        ruta = filedialog.asksaveasfilename(title='Exportar ' + tipo, defaultextension='.csv',
                                            filetypes=[('CSV', '*.csv')])
        if ruta:
            def accion():
                self.datos.exportar(ruta, tipo)
                messagebox.showinfo('Exportar', 'Archivo guardado correctamente')
            self.gestionar_error(accion)

    def mostrar_movimientos(self):
        self.limpiar('Movimientos')
        f = self.marco()
        self.titulo(f, 'Registrar entradas y salidas')
        formulario = tk.Frame(f, bg=BLANCO)
        formulario.pack(anchor='w', pady=10)
        campos = {}
        for i, (clave, etiqueta) in enumerate([('codigo', 'Código del producto'), ('tipo', 'Tipo de movimiento'),
                                                 ('cantidad', 'Cantidad'), ('detalle', 'Detalle / motivo')]):
            tk.Label(formulario, text=etiqueta, bg=BLANCO, width=22, anchor='w').grid(row=i, column=0, pady=7)
            if clave == 'tipo':
                ctrl = ttk.Combobox(formulario, values=['entrada', 'salida'], width=30, state='readonly')
                ctrl.set('entrada')
            else:
                ctrl = tk.Entry(formulario, width=33, bg=GRIS, relief='flat')
            ctrl.grid(row=i, column=1, padx=10)
            campos[clave] = ctrl
        campos['cantidad'].insert(0, '1')
        campos['detalle'].insert(0, 'Movimiento manual')
        def guardar():
            def accion():
                mov = self.datos.movimiento(**{k: x.get().strip() for k, x in campos.items()})
                messagebox.showinfo('Registro exitoso', f"Movimiento registrado. Existencia actual: {mov['despues']}")
                p = self.datos.productos[mov['codigo']]
                if p['cantidad'] <= p['minimo']:
                    messagebox.showwarning('Aviso de inventario', f'{p["nombre"]}: existencia {p["cantidad"]}, mínimo {p["minimo"]}')
                self.mostrar_movimientos()
            self.gestionar_error(accion)
        barra = tk.Frame(f, bg=BLANCO)
        barra.pack(fill='x', pady=8)
        self.boton(barra, 'Guardar movimiento', guardar).pack(side='left', padx=5)
        self.boton(barra, 'Procesar lote CSV', self.importar_movimientos).pack(side='left', padx=5)
        tk.Label(f, text='Últimos movimientos', bg=BLANCO, font=('Arial', 10, 'bold')).pack(anchor='w', pady=8)
        tabla = self.crear_tabla(f, ('Fecha', 'Código', 'Tipo', 'Cantidad', 'Antes', 'Después'), 9)
        for m in reversed(self.datos.movimientos[-50:]):
            tabla.insert('', 'end', values=(m['fecha'], m['codigo'], m['tipo'], m['cantidad'], m['antes'], m['despues']))

    def importar_movimientos(self):
        ruta = filedialog.askopenfilename(title='Procesar movimientos', filetypes=[('CSV', '*.csv')])
        if ruta:
            def accion():
                registros = self.datos.leer_registros(ruta)
                resultados = self.datos.procesar_lote(registros)
                buenos = sum(r['estado'] == 'Aceptado' for r in resultados)
                muestra = '\n'.join(f"Fila {r['fila']}: {r['estado']} - {r['detalle']}" for r in resultados[:18])
                messagebox.showinfo('Procesamiento', f'Aceptados: {buenos}\nRechazados: {len(resultados)-buenos}\n\n{muestra}')
                self.mostrar_movimientos()
            self.gestionar_error(accion)

    def mostrar_historial(self):
        self.limpiar('Historial')
        f = self.marco()
        barra = tk.Frame(f, bg=BLANCO)
        barra.pack(fill='x', pady=3)
        self.area_hist = tk.Frame(f, bg=BLANCO)
        self.area_hist.pack(fill='both', expand=True, pady=8)
        for nombre, fn in [('Productos bajos en inventario', self.reporte_bajos),
                           ('Reporte de inventario', self.reporte_inventario),
                           ('Reporte de movimientos', self.reporte_movimientos)]:
            self.boton(barra, nombre, fn).pack(side='left', padx=4)
        self.reporte_bajos()

    def limpiar_hist(self, titulo):
        for w in self.area_hist.winfo_children():
            w.destroy()
        self.titulo(self.area_hist, titulo)

    def reporte_bajos(self):
        self.limpiar_hist('Productos bajos en inventario')
        filtros = tk.Frame(self.area_hist, bg=BLANCO)
        filtros.pack(fill='x')
        tk.Label(filtros, text='Buscar por nombre o categoría:', bg=BLANCO).pack(side='left')
        buscar = tk.Entry(filtros, bg=GRIS, width=30)
        buscar.pack(side='left', padx=9)
        tabla = self.crear_tabla(self.area_hist, ('Código', 'Nombre del producto', 'Precio', 'Inventario', 'Inv. mínimo', 'Departamento'), 14)
        def cargar(*_):
            tabla.delete(*tabla.get_children())
            for p in self.datos.buscar(buscar.get(), estado='Bajo inventario'):
                tabla.insert('', 'end', values=(p['codigo'], p['nombre'], f"${p['precio']:.2f}", p['cantidad'], p['minimo'], p['depto']))
        buscar.bind('<KeyRelease>', cargar)
        cargar()

    def reporte_inventario(self):
        self.limpiar_hist('Reporte de inventario')
        resumen = tk.Frame(self.area_hist, bg=BLANCO)
        resumen.pack(fill='x', pady=6)
        tk.Label(resumen, text=f'Valor de inventario:   ${self.datos.total():,.2f}',
                 font=('Arial', 12, 'bold'), bg=BLANCO).pack(side='left', padx=15)
        tk.Label(resumen, text=f'Unidades en inventario: {sum(p["cantidad"] for p in self.datos.productos.values())}',
                 bg=BLANCO, font=('Arial', 11, 'bold')).pack(side='left', padx=15)
        filtros = tk.Frame(self.area_hist, bg=BLANCO)
        filtros.pack(fill='x', pady=6)
        tk.Label(filtros, text='Departamento:', bg=BLANCO).pack(side='left')
        dep = ttk.Combobox(filtros, values=['Todos'] + self.datos.departamentos, width=17, state='readonly')
        dep.set('Todos')
        dep.pack(side='left', padx=5)
        tk.Label(filtros, text='Estado:', bg=BLANCO).pack(side='left', padx=4)
        estado = ttk.Combobox(filtros, values=['Todos', 'Disponibles', 'Agotados', 'Bajo inventario'], width=16, state='readonly')
        estado.set('Todos')
        estado.pack(side='left')
        tk.Label(filtros, text='Orden:', bg=BLANCO).pack(side='left', padx=4)
        orden = ttk.Combobox(filtros, values=['codigo', 'precio', 'existencia', 'categoria', 'valor'], width=13, state='readonly')
        orden.set('codigo')
        orden.pack(side='left')
        self.boton(filtros, 'Exportar', lambda: self.exportar('productos'), padx=7).pack(side='right', padx=3)
        self.boton(filtros, 'Imprimir', lambda: self.imprimir_reporte('productos'), padx=7).pack(side='right')
        tabla = self.crear_tabla(self.area_hist, ('Código', 'Nombre', 'Precio', 'Existencia', 'Inv. mínimo', 'Departamento', 'Valor'), 14)
        def cargar(*_):
            tabla.delete(*tabla.get_children())
            for p in self.datos.buscar(departamento=dep.get(), estado=estado.get(), ordenar=orden.get()):
                tabla.insert('', 'end', values=(p['codigo'], p['nombre'], f"${p['precio']:.2f}", p['cantidad'], p['minimo'], p['depto'], f"${p['precio']*p['cantidad']:,.2f}"))
        for combo in (dep, estado, orden):
            combo.bind('<<ComboboxSelected>>', cargar)
        cargar()

    def reporte_movimientos(self):
        self.limpiar_hist('Reporte de movimientos')
        filtros = tk.Frame(self.area_hist, bg=BLANCO)
        filtros.pack(fill='x', pady=8)
        tk.Label(filtros, text='Buscar por código o nombre:', bg=BLANCO).pack(side='left')
        buscar = tk.Entry(filtros, bg=GRIS, width=25)
        buscar.pack(side='left', padx=7)
        tk.Label(filtros, text='Tipo:', bg=BLANCO).pack(side='left')
        tipo = ttk.Combobox(filtros, values=['Todos', 'entrada', 'salida', 'venta'], width=12, state='readonly')
        tipo.set('Todos')
        tipo.pack(side='left', padx=6)
        self.boton(filtros, 'Exportar', lambda: self.exportar('movimientos')).pack(side='right', padx=3)
        self.boton(filtros, 'Imprimir', lambda: self.imprimir_reporte('movimientos')).pack(side='right')
        tabla = self.crear_tabla(self.area_hist, ('Fecha', 'Código', 'Nombre', 'Tipo', 'Cantidad', 'Antes', 'Después', 'Departamento'), 14)
        def cargar(*_):
            tabla.delete(*tabla.get_children())
            texto = buscar.get().casefold()
            for m in reversed(self.datos.movimientos):
                if (texto in m['codigo'].casefold() or texto in m['nombre'].casefold()) and (tipo.get() == 'Todos' or tipo.get() == m['tipo']):
                    tabla.insert('', 'end', values=(m['fecha'], m['codigo'], m['nombre'], m['tipo'], m['cantidad'], m['antes'], m['despues'], m['depto']))
        buscar.bind('<KeyRelease>', cargar)
        tipo.bind('<<ComboboxSelected>>', cargar)
        cargar()

    def imprimir_reporte(self, tipo):
        ruta = filedialog.asksaveasfilename(title='Guardar reporte para imprimir', defaultextension='.html', filetypes=[('Página HTML', '*.html')])
        if not ruta:
            return
        import html
        if tipo == 'productos':
            cabeceras = ['Código', 'Nombre', 'Precio', 'Existencia', 'Mínimo', 'Departamento']
            filas = [[p['codigo'], p['nombre'], p['precio'], p['cantidad'], p['minimo'], p['depto']] for p in self.datos.productos.values()]
        else:
            cabeceras = ['Fecha', 'Código', 'Nombre', 'Tipo', 'Cantidad', 'Antes', 'Después']
            filas = [[m.get(x, '') for x in ('fecha', 'codigo', 'nombre', 'tipo', 'cantidad', 'antes', 'despues')] for m in self.datos.movimientos]
        t = '<tr>' + ''.join('<th>' + html.escape(str(c)) + '</th>' for c in cabeceras) + '</tr>'
        t += ''.join('<tr>' + ''.join('<td>' + html.escape(str(c)) + '</td>' for c in fila) + '</tr>' for fila in filas)
        documento = '<!doctype html><html lang="es"><meta charset="utf-8"><title>Reporte</title><style>body{font:14px Arial;padding:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #aaa;padding:8px}th{background:#c8d6e0}</style><h2>StockControl - Reporte de ' + html.escape(tipo) + '</h2><table>' + t + '</table><p>Abre este archivo en un navegador y usa Ctrl+P para imprimir o guardar como PDF.</p></html>'
        try:
            Path(ruta).write_text(documento, encoding='utf-8')
            messagebox.showinfo('Imprimir', 'Reporte HTML guardado. Ábrelo en el navegador y utiliza Ctrl+P.')
        except OSError as exc:
            messagebox.showerror('Error', str(exc))


if __name__ == '__main__':
    raiz = tk.Tk()
    SistemaPOS(raiz)
    raiz.mainloop()