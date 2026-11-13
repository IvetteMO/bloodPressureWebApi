namespace bloodPressureWebApi.Exceptions
{

        /// <summary>
        /// Los datos del estudio no cumplen lo que exige el modelo: filas con
        /// hora o valores inválidos, número de lecturas distinto de 47, sin
        /// lecturas de día o de noche.
        ///
        /// Hereda de InvalidOperationException a propósito: reemplaza a
        /// "throw new InvalidOperationException(...)" y los experimentos E1/E1-B
        /// que atrapan InvalidOperationException se comportan igual que antes.
        /// </summary>
        public sealed class InvalidStudyDataException(string message)
            : InvalidOperationException(message), IUserFacingException
        {
            public string Title => "Los datos del estudio no son válidos";
        }
    }

