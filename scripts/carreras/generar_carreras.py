"""
Script para convertir datos de CSV de Oferta Académica y Formulario SIMETICS
a un documento JSON con estructura de Áreas, Facultades, Escuelas y Carreras.

Este script lee dos archivos CSV:
1. Oferta académica 2026.csv - Contiene información de carreras, facultades, escuelas y sedes
2. Opciones formulario SIMETICS.csv - Contiene la estructura organizacional (opcional para validación)

Genera un archivo JSON (dataAreas.json) con la estructura jerárquica actualizada
incluyendo las carreras organizadas por sede/recinto.
"""

import csv
import json
import re
from pathlib import Path
from collections import defaultdict
from typing import Dict, List, Any

def limpiar_nombre(nombre: str) -> str:
    """Limpia y normaliza nombres de escuelas y sedes."""
    if not nombre:
        return ""
    return nombre.strip()

def mapear_sede_a_recinto(sede: str, recinto: str) -> str:
    """Mapea sede y recinto a un nombre único para la sede."""
    sede = limpiar_nombre(sede)
    recinto = limpiar_nombre(recinto)
    
    # Si hay recinto, usa su nombre
    if recinto:
        # Normalizar nombres de recintos comunes
        recinto_map = {
            "San Ramón": "Recinto de San Ramón",
            "Liberia": "Recinto de Liberia",
            "Turrialba": "Recinto de Turrialba",
            "Guápiles": "Recinto de Guápiles",
            "Limón": "Recinto de Limón",
            "Puntarenas": "Recinto de Puntarenas",
            "Golfito": "Recinto de Golfito",
            "Paraíso": "Recinto de Paraíso",
            "Siquirres": "Recinto de Siquirres",
            "Santa Cruz": "Recinto de Santa Cruz",
            "Tacares": "Recinto de Tacares",
            "Alajuela": "Recinto de Alajuela",
            "Grecia": "Recinto de Grecia",
        }
        
        # Buscar coincidencias
        for key, value in recinto_map.items():
            if key.lower() in recinto.lower():
                return value
        
        # Si no coincide, devolver el recinto como está
        return f"Recinto de {recinto}" if recinto else "Ciudad Universitaria Rodrigo Facio"
    
    # Si no hay recinto, retornar la sede o la ubicación principal
    return "Ciudad Universitaria Rodrigo Facio"

def construir_carreras_por_sede(carreras: List[Dict[str, str]]) -> Dict[str, List[str]]:
    """Construye la estructura de carreras agrupadas por sede/recinto."""
    carreras_por_sede = defaultdict(list)

    for carrera in carreras:
        sede_recinto = mapear_sede_a_recinto(
            carrera.get('sede', ''),
            carrera.get('recinto', '')
        )

        nombre_carrera = carrera.get('nombre', '')
        if nombre_carrera and nombre_carrera not in carreras_por_sede[sede_recinto]:
            carreras_por_sede[sede_recinto].append(nombre_carrera)

    return dict(carreras_por_sede)

def leer_oferta_academica(ruta_csv: Path) -> Dict[str, List[Dict[str, str]]]:
    """
    Lee el CSV de oferta académica y organiza las carreras por escuela.
    
    Returns:
        Dict con estructura: {
            "Escuela": [
                {
                    "nombre": "Nombre de carrera",
                    "facultad": "Nombre facultad",
                    "escuela": "Nombre escuela",
                    "sede": "Nombre sede",
                    "recinto": "Nombre recinto"
                }
            ]
        }
    """
    carreras_por_escuela = defaultdict(list)
    
    try:
        # Intentar diferentes codificaciones - UTF-16 es común en archivos de Excel
        encodings = ['utf-16', 'utf-16-le', 'utf-8-sig', 'utf-8', 'latin-1', 'cp1252', 'iso-8859-1']
        f = None
        
        for encoding in encodings:
            try:
                f = open(ruta_csv, 'r', encoding=encoding)
                reader = csv.DictReader(f)
                # Validar que podemos leer al menos una fila
                next(reader)
                f.seek(0)  # Volver al inicio
                reader = csv.DictReader(f)
                break
            except (UnicodeDecodeError, StopIteration):
                if f:
                    f.close()
                continue
        
        if not f:
            print(f"✗ No se pudo determinar la codificación del archivo")
            return {}
        
        with f:
            
            for row in reader:
                # Extraer datos del CSV
                nombre_carrera = limpiar_nombre(row.get('Nombre de carrera ', ''))
                facultad = limpiar_nombre(row.get('Facultad', ''))
                escuela = limpiar_nombre(row.get('Escuela ', ''))
                sede = limpiar_nombre(row.get('Sede', ''))
                recinto = limpiar_nombre(row.get('Recinto ', ''))
                
                # Saltar filas sin datos de carrera
                if not nombre_carrera:
                    continue

                # Si no hay escuela, conservar en un contenedor de no relacionadas
                if not escuela:
                    escuela = "Sin escuela o facultad (CSV)"
                
                # Crear entrada de carrera
                carrera_data = {
                    'nombre': nombre_carrera,
                    'facultad': facultad,
                    'escuela': escuela,
                    'sede': sede,
                    'recinto': recinto
                }
                
                # Agrupar por escuela
                carreras_por_escuela[escuela].append(carrera_data)
        
        print(f"✓ Leyendo oferta académica: {len(carreras_por_escuela)} escuelas encontradas")
        return dict(carreras_por_escuela)
    
    except FileNotFoundError:
        print(f"✗ Error: No se encontró el archivo {ruta_csv}")
        return {}

def cargar_dataAreas_base(ruta_json: Path) -> Dict[str, Any]:
    """
    Carga el archivo dataAreas.json existente como base.
    Maneja archivos JSON con comentarios eliminándolos primero.
    """
    try:
        with open(ruta_json, 'r', encoding='utf-8') as f:
            contenido = f.read()
        
        # Eliminar comentarios de línea (// comentario)
        contenido_limpio = re.sub(r'//.*?$', '', contenido, flags=re.MULTILINE)
        
        # Parsear JSON
        return json.loads(contenido_limpio)
    except FileNotFoundError:
        print(f"✗ Error: No se encontró {ruta_json}")
        return {"areas": []}
    except json.JSONDecodeError as e:
        print(f"✗ Error al parsear JSON: {e}")
        return {"areas": []}

def agregar_carreras_a_estructura(estructura: Dict[str, Any], carreras_por_escuela: Dict) -> tuple[Dict[str, Any], List[str]]:
    """
    Agrega las carreras a la estructura de áreas, facultades y escuelas.
    Intenta emparejar nombres de escuelas tanto con como sin prefijo "Escuela de".
    
    Returns:
        tuple de (estructura actualizada, lista de escuelas no encontradas)
    """
    escuelas_encontradas = set()
    escuelas_no_encontradas = list(carreras_por_escuela.keys())
    
    # Recorrer todas las áreas
    for area in estructura.get('areas', []):
        # Recorrer departamentos (facultades)
        for departamento in area.get('departamentos', []):
            # Recorrer secciones (escuelas)
            for seccion in departamento.get('secciones', []):
                nombre_escuela = seccion.get('name', '')
                
                # Intentar encontrar carreras
                carrera_key = None
                
                # Opción 1: búsqueda exacta
                if nombre_escuela in carreras_por_escuela:
                    carrera_key = nombre_escuela
                
                # Opción 2: eliminar prefijo "Escuela de"
                elif nombre_escuela.startswith('Escuela de '):
                    nombre_sin_prefijo = nombre_escuela.replace('Escuela de ', '', 1)
                    if nombre_sin_prefijo in carreras_por_escuela:
                        carrera_key = nombre_sin_prefijo
                
                if carrera_key and carrera_key in carreras_por_escuela:
                    escuelas_encontradas.add(carrera_key)
                    if carrera_key in escuelas_no_encontradas:
                        escuelas_no_encontradas.remove(carrera_key)

                    # Organizar carreras por sede/recinto
                    carreras_por_sede = construir_carreras_por_sede(carreras_por_escuela[carrera_key])
                    
                    # Agregar carreras a la sección
                    if carreras_por_sede:
                        seccion['carreras'] = carreras_por_sede
    
    return estructura, escuelas_no_encontradas

def incluir_carreras_no_relacionadas(estructura: Dict[str, Any], carreras_por_escuela: Dict[str, List[Dict[str, str]]], escuelas_no_encontradas: List[str]) -> tuple[Dict[str, Any], int]:
    """
    Incluye carreras que no se pudieron relacionar con escuelas existentes.
    Se agregan bajo el área "Otros" -> departamento "Carreras no relacionadas".

    Returns:
        tuple de (estructura actualizada, total de carreras no relacionadas agregadas)
    """
    if not escuelas_no_encontradas:
        return estructura, 0

    # Buscar o crear área "Otros"
    area_otros = None
    for area in estructura.get('areas', []):
        if area.get('name') == 'Otros':
            area_otros = area
            break

    if area_otros is None:
        area_otros = {
            "name": "Otros",
            "departamentos": []
        }
        estructura.setdefault('areas', []).append(area_otros)

    # Buscar o crear departamento "Carreras no relacionadas"
    dept_no_rel = None
    for dept in area_otros.get('departamentos', []):
        if dept.get('name') == 'Carreras no relacionadas':
            dept_no_rel = dept
            break

    if dept_no_rel is None:
        dept_no_rel = {
            "name": "Carreras no relacionadas",
            "secciones": []
        }
        area_otros.setdefault('departamentos', []).append(dept_no_rel)

    # Índice de secciones ya existentes en el contenedor de no relacionadas
    secciones_idx = {
        seccion.get('name'): seccion
        for seccion in dept_no_rel.get('secciones', [])
        if seccion.get('name')
    }

    total_agregadas = 0

    for escuela in sorted(escuelas_no_encontradas):
        carreras = carreras_por_escuela.get(escuela, [])
        if not carreras:
            continue

        carreras_por_sede = construir_carreras_por_sede(carreras)
        total_agregadas += sum(len(lista) for lista in carreras_por_sede.values())

        # Reusar sección si ya existe
        if escuela in secciones_idx:
            secciones_idx[escuela]['carreras'] = carreras_por_sede
        else:
            nueva_seccion = {
                "name": escuela,
                "carreras": carreras_por_sede
            }
            dept_no_rel.setdefault('secciones', []).append(nueva_seccion)
            secciones_idx[escuela] = nueva_seccion

    return estructura, total_agregadas

def guardar_json(data: Dict[str, Any], ruta_salida: Path) -> bool:
    """
    Guarda la estructura en un archivo JSON formateado.
    """
    try:
        with open(ruta_salida, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print(f"✓ Archivo guardado: {ruta_salida}")
        return True
    except Exception as e:
        print(f"✗ Error al guardar: {e}")
        return False

def main():
    """Función principal del script."""
    print("=" * 70)
    print("GENERADOR DE JSON DE CARRERAS POR ESCUELA Y SEDE")
    print("=" * 70)
    
    # Definir rutas
    base_path = Path(__file__).parent.parent.parent  # Ir a METICS-CORE
    ruta_csv_oferta = base_path / "scripts" / "carreras" / "Oferta académica 2026.csv"
    ruta_dataAreas_base = base_path / "webMetics" / "wwwroot" / "data" / "dataAreas.json"
    ruta_salida = base_path / "webMetics" / "wwwroot" / "data" / "dataAreas.json"
    
    print(f"\nRutas:")
    print(f"   CSV Oferta: {ruta_csv_oferta}")
    print(f"   Base JSON: {ruta_dataAreas_base}")
    print(f"   Salida JSON: {ruta_salida}")
    
    # Paso 1: Leer oferta académica
    print(f"\n1: Leyendo oferta académica...")
    carreras_por_escuela = leer_oferta_academica(ruta_csv_oferta)
    print(f"   Total de escuelas con carreras: {len(carreras_por_escuela)}")
    
    # Paso 2: Cargar estructura base
    print(f"\n2: Cargando estructura base de dataAreas.json...")
    estructura = cargar_dataAreas_base(ruta_dataAreas_base)
    
    # Paso 3: Agregar carreras
    print(f"\n3: Agregando carreras a la estructura...")
    estructura_actualizada, escuelas_no_encontradas = agregar_carreras_a_estructura(estructura, carreras_por_escuela)
    
    # Contar carreras agregadas
    total_carreras = 0
    for area in estructura_actualizada.get('areas', []):
        for dept in area.get('departamentos', []):
            for secc in dept.get('secciones', []):
                if 'carreras' in secc:
                    total_carreras += sum(len(v) for v in secc['carreras'].values())
    
    print(f"Total de carreras agregadas: {total_carreras}")
    
    # Incluir y mostrar escuelas no encontradas
    carreras_no_relacionadas = 0
    if escuelas_no_encontradas:
        estructura_actualizada, carreras_no_relacionadas = incluir_carreras_no_relacionadas(
            estructura_actualizada,
            carreras_por_escuela,
            escuelas_no_encontradas
        )

        print(f"\nEscuelas del CSV NO encontradas en la estructura JSON base:")
        for esc in sorted(escuelas_no_encontradas):
            print(f"   - {esc}")
        print(f"\nCarreras no relacionadas incluidas automáticamente: {carreras_no_relacionadas}")
        print(f"Ubicación: Otros -> Carreras no relacionadas")
    
    # Paso 4: Guardar resultado
    print(f"\n4: Guardando resultado...")
    if guardar_json(estructura_actualizada, ruta_salida):
        print(f"\nPROCESO COMPLETADO EXITOSAMENTE")
        print(f"Archivo generado: {ruta_salida}")
    else:
        print(f"\nERROR durante el proceso")
        return False
    
    return True

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)
