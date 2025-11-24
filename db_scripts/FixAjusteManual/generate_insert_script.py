"""
Script para convertir datos de XLSX/CSV a script SQL con INSERT INTO
Uso: python generate_insert_script.py <archivo_entrada> [archivo_salida]
"""

import sys
import os
import csv
from pathlib import Path

def leer_csv(ruta_archivo, delimitador=',', codificacion='utf-8'):
    """Lee un archivo CSV y retorna lista de diccionarios"""
    filas = []
    try:
        with open(ruta_archivo, 'r', encoding=codificacion) as archivo:
            lector = csv.DictReader(archivo, delimiter=delimitador)
            for fila in lector:
                if any(fila.values()):  # Ignorar filas vacías
                    filas.append(fila)
    except UnicodeDecodeError:
        # Reintentar con otra codificación
        with open(ruta_archivo, 'r', encoding='latin-1') as archivo:
            lector = csv.DictReader(archivo, delimiter=delimitador)
            for fila in lector:
                if any(fila.values()):
                    filas.append(fila)
    
    return filas


def escapar_sql(valor):
    """Escapa comillas simples para SQL"""
    if valor is None:
        return "NULL"
    return str(valor).replace("'", "''")


def generar_insert_script(filas, columnas, nombre_tabla="@updates"):
    """
    Genera script SQL con INSERT INTO
    
    Args:
        filas: Lista de diccionarios con los datos
        columnas: Diccionario con mapeo {columna_sql: tipo_dato}
                 Tipos soportados: 'nvarchar', 'int', 'float', 'date', 'bit'
        nombre_tabla: Nombre de la tabla (por defecto @updates)
    
    Returns:
        String con el script SQL
    """
    if not filas:
        print("No hay datos para procesar")
        return ""
    
    script = []
    script.append(f"INSERT INTO {nombre_tabla} ({', '.join(columnas.keys())}) VALUES")
    
    for idx, fila in enumerate(filas):
        valores = []
        
        for col_sql, tipo_dato in columnas.items():
            valor = fila.get(col_sql, "")
            
            if valor is None or valor == "":
                valores.append("NULL")
            elif tipo_dato.lower() == 'nvarchar':
                valores.append(f"N'{escapar_sql(valor)}'")
            elif tipo_dato.lower() in ['int', 'integer']:
                try:
                    valores.append(str(int(float(valor))))
                except (ValueError, TypeError):
                    valores.append("0")
            elif tipo_dato.lower() == 'float':
                try:
                    valores.append(str(float(valor)))
                except (ValueError, TypeError):
                    valores.append("0.0")
            elif tipo_dato.lower() == 'date':
                valores.append(f"'{escapar_sql(valor)}'")
            elif tipo_dato.lower() == 'bit':
                valor_bool = 1 if str(valor).lower() in ['true', '1', 'sí', 'yes'] else 0
                valores.append(str(valor_bool))
            else:
                valores.append(f"'{escapar_sql(valor)}'")
        
        coma = "," if idx < len(filas) - 1 else ";"
        script.append(f"({', '.join(valores)}){coma}")
    
    return "\n".join(script)


def main():
    """Función principal"""
    if len(sys.argv) < 2:
        print("Uso: python generate_insert_script.py <archivo_entrada> [archivo_salida] [--config config.ini]")
        print("\nEjemplos:")
        print("  python generate_insert_script.py datos.csv datos.sql")
        print("\nFormatos soportados: .csv")
        sys.exit(1)
    
    archivo_entrada = sys.argv[1]
    archivo_salida = sys.argv[2] if len(sys.argv) > 2 else "output_insert.sql"
    
    # Validar que existe el archivo
    if not os.path.exists(archivo_entrada):
        print(f"Error: El archivo '{archivo_entrada}' no existe")
        sys.exit(1)
    
    print(f"Leyendo: {archivo_entrada}")
    
    # Detectar formato y leer archivo
    extension = Path(archivo_entrada).suffix.lower()
    
    try:
        if extension == '.csv':
            delimitador = ','
            filas = leer_csv(archivo_entrada, delimitador=delimitador)
            
        else:
            print(f"Formato no soportado: {extension}")
            print("Soportado: .csv")
            sys.exit(1)
        
        if not filas:
            print("No se encontraron datos en el archivo")
            sys.exit(1)
        
        print(f"Se encontraron {len(filas)} filas")
        print(f"Columnas: {', '.join(filas[0].keys())}")
        
        # Definir mapeo de columnas y tipos

        # PERSONALIZAR ESTO SEGÚN EL SCRIPT SQL
        columnas = {
            'id_participante_FK': 'nvarchar',
            'nombre_grupo': 'nvarchar',
            'numero_grupo': 'int'
        }
        
        # Validar que existan las columnas requeridas
        columnas_archivo = set(filas[0].keys())
        columnas_requeridas = set(columnas.keys())
        
        if not columnas_requeridas.issubset(columnas_archivo):
            columnas_faltantes = columnas_requeridas - columnas_archivo
            print(f"\nAdvertencia: Columnas faltantes en el archivo: {', '.join(columnas_faltantes)}")
            print(f"   Columnas disponibles: {', '.join(columnas_archivo)}")
            respuesta = input("\n¿Continuar? (s/n): ")
            if respuesta.lower() != 's':
                sys.exit(1)
        
        # Generar script
        script_sql = generar_insert_script(filas, columnas)
        
        # Guardar archivo
        with open(archivo_salida, 'w', encoding='utf-8') as f:
            f.write(script_sql)
        
        print(f"Script guardado en: {archivo_salida}")
        
    except Exception as e:
        print(f"Error al procesar: {str(e)}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
