namespace bloodPressureWebApi.Exceptions
{
    /// <summary>
    /// Marca las excepciones cuyo mensaje está escrito para el usuario
    /// (por ejemplo: "Fila 12: la hora esta vacia"). El
    /// GlobalExceptionHandler las devuelve como 400 con su mensaje.
    ///
    /// Cualquier otra excepción se devuelve como 500 con un mensaje
    /// genérico: su texto puede traer detalles internos (SQL, MATLAB,
    /// rutas) y solo se escribe en el log.
    /// </summary>
    public interface IUserFacingException
    {
        string Title { get; }
    }
}
