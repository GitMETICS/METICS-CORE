# Generador de JSON de Carreras por Escuela y Sede

Script Python para convertir datos de CSV a un documento JSON estructurado con carreras organizadas por escuela y sede.

## Descripción

Este script procesa dos archivos CSV:

1. **Oferta académica.csv** - Contiene el catálogo completo de carreras con información de:
   - Código de carrera
   - Nombre de carrera
   - Facultad
   - Escuela
   - Sede/Recinto

2. **Opciones formulario SIMETICS.csv** - Proporciona la estructura organizacional de referencia

Y genera un archivo **dataAreas.json** actualizado que incluye:
- Áreas académicas
- Facultades (departamentos)
- Escuelas (secciones)
- **Carreras organizadas por sede/recinto**

## Características principales

- Maneja JSON con comentarios (elimina comentarios automáticamente)
- Emparejas nombres de escuelas flexiblemente (con o sin prefijo "Escuela de")
- Elimina automáticamente carreras duplicadas
- Normaliza nombres de sedes y recintos
- Genera reporte de escuelas no encontradas
- Mantiene la estructura existente sin perder datos
- Reporta cuántas carreras no relacionadas fueron incluidas y dónde quedaron.

## Cómo usar

### Requisitos previos

- Python 3.7 o superior
- Los archivos CSV deben estar presentes en las ubicaciones esperadas:
  - `c:\Users\ebair\Downloads\Oferta académica 2026.csv`
  - (Opcional) `c:\Users\ebair\Downloads\Opciones formulario SIMETICS.csv`

### Pasos de ejecución

1. **Ubicar el script**
   ```
   scripts/carreras/generar_carreras.py
   ```

2. **Ejecutar el script**
   ```bash
   python generar_carreras.py
   ```
   
   O en PowerShell:
   ```powershell
   python .\generar_carreras.py
   ```

3. **Esperar la confirmación**
   
   El script mostrará un progreso similar a:
   ```
    ======================================================================
    GENERADOR DE JSON DE CARRERAS POR ESCUELA Y SEDE
    ======================================================================

    Rutas:
    CSV Oferta: C:\Users\ebair\Home\METICS\METICS-CORE\scripts\carreras\Oferta académica 2026.csv
    Base JSON: C:\Users\ebair\Home\METICS\METICS-CORE\webMetics\wwwroot\data\dataAreas.json   
    Salida JSON: C:\Users\ebair\Home\METICS\METICS-CORE\webMetics\wwwroot\data\dataAreas.json 

    1: Leyendo oferta académica...
    ✓ Leyendo oferta académica: 51 escuelas encontradas
    Total de escuelas con carreras: 51

    2: Cargando estructura base de dataAreas.json...

    3: Agregando carreras a la estructura...
    Total de carreras agregadas: 190

    4: Guardando resultado...
    ✓ Archivo guardado: C:\Users\ebair\Home\METICS\METICS-CORE\webMetics\wwwroot\data\dataAreas.json

    PROCESO COMPLETADO EXITOSAMENTE
    Archivo generado: C:\Users\ebair\Home\METICS\METICS-CORE\webMetics\wwwroot\data\dataAreas.json
   ```

## Estructura del JSON generado

```json
{
  "areas": [
    {
      "name": "Área de Artes y Letras",
      "departamentos": [
        {
          "name": "Facultad de Artes",
          "secciones": [
            {
              "name": "Escuela de Artes Dramáticas",
              "carreras": {
                "Ciudad Universitaria Rodrigo Facio": [
                  "Bachillerato y Licenciatura en Artes Teatrales"
                ],
                "Recinto de San Ramón": [
                  "Bachillerato y Licenciatura en Artes Teatrales"
                ]
              }
            }
          ]
        }
      ]
    }
  ]
}
```

### Claves principales

- **areas**: Array de áreas académicas
  - **name**: Nombre del área
  - **departamentos**: Array de facultades/departamentos
    - **name**: Nombre de la facultad
    - **secciones**: Array de escuelas/secciones
      - **name**: Nombre de la escuela
      - **carreras**: Objeto con sedes como clave y array de carreras como valor

## Proceso de conversión

1. **Lectura de CSV**: El script lee el archivo de Oferta Académica 2026 auto-detectando la codificación
2. **Organización**: Agrupa las carreras por escuela
3. **Mapeo de sedes**: Normaliza los nombres de sedes y recintos a formatos estándar
4. **Carga de base**: Lee el dataAreas.json existente (elimina comentarios si existen)
5. **Emparejamiento**: Intenta emparejar escuelas del CSV con el JSON (flexible, maneja prefijos)
6. **Agregación**: Agrega las carreras a la estructura jerárquica
7. **Deduplicación**: Elimina carreras duplicadas por sede
8. **Reporte**: Identifica escuelas no encontradas para sincronización manual
9. **Escritura**: Guarda el resultado en dataAreas.json sin modificar comentarios originales

## 📝 Notas importantes

- El script **mantiene la estructura existente** de áreas, facultades y escuelas
- **Solo agrega/actualiza** la información de carreras
- **Normaliza automáticamente** los nombres de sedes y recintos según mapeo interno
- **Detecta automáticamente** cuándo no hay sede específica (usa "Ciudad Universitaria Rodrigo Facio")
- **Evita duplicados** de carreras en la misma sede/recinto
- **Preserva comentarios** del JSON original (aunque los ignora al procesar)

## Mapeo de sedes incluido

El script normaliza automáticamente los siguientes recintos:

- San Ramón → Recinto de San Ramón
- Liberia → Recinto de Liberia
- Turrialba → Recinto de Turrialba
- Guápiles → Recinto de Guápiles
- Limón → Recinto de Limón
- Puntarenas → Recinto de Puntarenas
- Golfito → Recinto de Golfito
- Paraíso → Recinto de Paraíso
- Siquirres → Recinto de Siquirres
- Santa Cruz → Recinto de Santa Cruz
- Tacares → Recinto de Tacares
- Alajuela → Recinto de Alajuela
- Grecia → Recinto de Grecia

Si no hay sede/recinto, se usa: **Ciudad Universitaria Rodrigo Facio**

## Personalización

Si se necesita modificar el comportamiento del script:

1. **Cambiar rutas**: Editar las variables en la función `main()`
   ```python
   ruta_csv_oferta = Path("nueva/ruta.csv")
   ruta_salida = Path("nueva/ruta/salida.json")
   ```

2. **Normalizar sedes**: Modificar el diccionario `recinto_map` en `mapear_sede_a_recinto()`
   ```python
   recinto_map = {
       "Recinto X": "Recinto de Ciudad X",
       ...
   }
   ```

3. **Filtrar carreras**: Agregar condiciones en la lectura del CSV dentro de `leer_oferta_academica()`

4. **Cambiar nombre de sede por defecto**: Modificar la última línea de `mapear_sede_a_recinto()`

## Archivos relacionados

- [Oferta académica 2026.csv](./Oferta%20académica%202026.csv)
- [Opciones formulario SIMETICS.csv](../../Downloads/Opciones%20formulario%20SIMETICS.csv)
- [dataAreas.json](../../webMetics/wwwroot/data/dataAreas.json)
