using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace webMetics.Services
{
    /// <summary>
    /// Resuelve el valor de carrera que se persiste en participante.carrera.
    /// Lógica pura: no toca base de datos, archivos ni HttpContext.
    /// </summary>
    public static class CarreraResolver
    {
        private const int LongitudMaxima = 512;

        /// <summary>
        /// Normaliza una carrera escrita a mano: quita diacríticos, pasa a mayúsculas,
        /// conserva solo A-Z y 0-9, trata cualquier otro carácter como separador,
        /// colapsa separadores en un espacio simple y trunca a 512 caracteres.
        /// </summary>
        public static string Normalizar(string? entrada)
        {
            if (string.IsNullOrWhiteSpace(entrada))
            {
                return string.Empty;
            }

            string sinDiacriticos = QuitarDiacriticos(entrada).ToUpperInvariant();

            var construccion = new StringBuilder(sinDiacriticos.Length);
            bool separadorPendiente = false;

            foreach (char caracter in sinDiacriticos)
            {
                bool esAlfanumerico = (caracter >= 'A' && caracter <= 'Z')
                                      || (caracter >= '0' && caracter <= '9');

                if (esAlfanumerico)
                {
                    if (separadorPendiente && construccion.Length > 0)
                    {
                        construccion.Append(' ');
                    }

                    separadorPendiente = false;
                    construccion.Append(caracter);
                }
                else
                {
                    // Espacios, símbolos y puntuación actúan como separador:
                    // "DISENO/MODAS" queda "DISENO MODAS", no "DISENOMODAS".
                    separadorPendiente = true;
                }
            }

            string resultado = construccion.ToString();

            return resultado.Length > LongitudMaxima
                ? resultado.Substring(0, LongitudMaxima)
                : resultado;
        }

        /// <summary>
        /// Si <paramref name="entrada"/> calza con una carrera del catálogo — comparando
        /// ambos lados normalizados — devuelve el nombre oficial tal cual viene del
        /// catálogo. Si no calza, devuelve la entrada normalizada.
        /// </summary>
        public static string Resolver(string? entrada, IEnumerable<string>? catalogo)
        {
            string normalizada = Normalizar(entrada);

            if (normalizada.Length == 0 || catalogo == null)
            {
                return normalizada;
            }

            foreach (string oficial in catalogo)
            {
                if (string.IsNullOrWhiteSpace(oficial))
                {
                    continue;
                }

                if (string.Equals(Normalizar(oficial), normalizada, StringComparison.Ordinal))
                {
                    return oficial;
                }
            }

            return normalizada;
        }

        private static string QuitarDiacriticos(string texto)
        {
            string descompuesto = texto.Normalize(NormalizationForm.FormD);
            var construccion = new StringBuilder(descompuesto.Length);

            foreach (char caracter in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                {
                    construccion.Append(caracter);
                }
            }

            return construccion.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
