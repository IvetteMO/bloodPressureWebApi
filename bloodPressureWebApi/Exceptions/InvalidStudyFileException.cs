namespace bloodPressureWebApi.Exceptions
{
        /// <summary>
        /// El archivo del estudio no se puede importar: formato inválido, hoja
        /// no encontrada, sin lecturas, estudio duplicado.
        ///
        /// Hereda de Exception (no de InvalidOperationException) a propósito:
        /// reemplaza a "throw new Exception(...)" en MAPAImportService sin
        /// cambiar qué bloques catch de los experimentos la atrapan.
        /// </summary>
        public sealed class InvalidStudyFileException(string message)
            : Exception(message), IUserFacingException
        {
            public string Title => "El archivo del estudio no es válido";
        }
    }

