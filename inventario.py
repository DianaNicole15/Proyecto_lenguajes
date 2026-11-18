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

    def cargar(self):
        """Recupera los datos guardados en archivos CSV."""
        productos = self.carpeta / 'productos.csv'
        movimientos = self.carpeta / 'movimientos.csv'
        departamentos = self.carpeta / 'departamentos.csv'

        if productos.exists():
            registros = self.leer_registros(productos)
            nuevos = [self.validar_producto(p) for p in registros]
            if len({p['codigo'] for p in nuevos}) != len(nuevos):
                raise ValueError('Existen códigos duplicados en productos.csv')
            self.productos = {p['codigo']: p for p in nuevos}

        if movimientos.exists():
            self.movimientos = self.leer_registros(movimientos)
            # Los datos numéricos se recuperan con su tipo original.
            for m in self.movimientos:
                for campo in ('cantidad', 'antes', 'despues'):
                    m[campo] = int(m[campo])
                m['precio'] = float(m['precio'])

        if departamentos.exists():
            registros = self.leer_registros(departamentos)
            nombres = [str(r.get('departamento', '')).strip() for r in registros]
            self.departamentos = sorted(set(['General'] + [x for x in nombres if x]))

        # Recupera categorías de productos también si no existe departamentos.csv.
        self.departamentos = sorted(set(self.departamentos) |
                                    {p['depto'] for p in self.productos.values()})
    @staticmethod
    def _guardar_csv(destino, columnas, registros):
        """Escribe los datos y sustituye el archivo al finalizar."""
        temporal = destino.with_suffix('.tmp')
        with temporal.open('w', encoding='utf-8-sig', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=columnas, extrasaction='ignore')
            writer.writeheader()
            writer.writerows(registros)
        temporal.replace(destino)

    def guardar(self):
        self._guardar_csv(self.carpeta / 'productos.csv', self.COLUMNAS_PRODUCTOS,
                          self.productos.values())
        self._guardar_csv(self.carpeta / 'movimientos.csv', self.COLUMNAS_MOVIMIENTOS,
                          self.movimientos)
        self._guardar_csv(self.carpeta / 'departamentos.csv', ['departamento'],
                          [{'departamento': d} for d in self.departamentos])

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

    def modificar(self, codigo, nombre, precio, depto, minimo):
        if codigo not in self.productos:
            raise ValueError('Producto inexistente')
        anterior = self.productos[codigo]
        p = self.validar_producto(dict(codigo=codigo, nombre=nombre, precio=precio,
                                       depto=depto, cantidad=anterior['cantidad'], minimo=minimo))
        self.productos[codigo] = p
        self.agregar_departamento(p['depto'], persistir=False)
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
        if tipo not in ('productos', 'movimientos'):
            raise ValueError('Tipo de exportación inválido')
        if ruta.suffix.lower() != '.csv':
            raise ValueError('Selecciona un archivo CSV (.csv)')
        datos = list(self.productos.values()) if tipo == 'productos' else self.movimientos
        columnas = (self.COLUMNAS_PRODUCTOS if tipo == 'productos'
                    else self.COLUMNAS_MOVIMIENTOS)
        self._guardar_csv(ruta, columnas, datos)

    @staticmethod
    def leer_registros(ruta):
        ruta = Path(ruta)
        if ruta.suffix.lower() != '.csv':
            raise ValueError('Formato inválido; utiliza únicamente archivos CSV')
        with ruta.open(encoding='utf-8-sig', newline='') as f:
            lector = csv.DictReader(f)
            if not lector.fieldnames:
                raise ValueError('El archivo CSV no tiene encabezados')
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