namespace Aserta.Dominio.Facturacion;

/// <summary>ClaveTipoFacturaType del XSD oficial.</summary>
public enum TipoFactura { F1, F2, F3, R1, R2, R3, R4, R5 }

/// <summary>ClaveTipoRectificativaType: S = por sustitucion, I = por diferencias.</summary>
public enum TipoRectificativa { S, I }

public enum TipoRegistro { ALTA, ANULACION }

/// <summary>Estados del envio (envio-y-cola-reintentos.md §5).</summary>
public enum EstadoEnvio
{
    GENERADO, EN_COLA, ENVIADO, ACEPTADO, ACEPTADO_CON_ERRORES, RECHAZADO, ERROR_TECNICO, DEAD_LETTER, BLOQUEADO_SIN_CERTIFICADO, ERROR_VALIDACION
}

public enum EstadoEnvioPendiente { PENDIENTE, EN_CURSO, COMPLETADO, DEAD_LETTER }

public enum TipoCertificado { Gestoria, Obligado }

public enum EstadoCertificado { Vigente, Caducado, Revocado }

/// <summary>
/// "¿Por que rectifica esta factura?" en lenguaje llano; el sistema deriva el
/// codigo (especificacion.md §4). Sobre una simplificada siempre R5.
/// Correspondencia [VERIFICAR] con la lista de valores del anexo de la orden.
/// </summary>
public enum MotivoRectificacion
{
    /// <summary>Devolucion, descuento posterior o resolucion de la operacion (art. 80 Uno/Dos LIVA) → R1.</summary>
    DevolucionODescuento,
    /// <summary>Error en importe, concepto o datos del destinatario → R4 (resto de casos).</summary>
    ErrorEnDatos,
    /// <summary>Cliente en concurso de acreedores (art. 80 Tres) → R2. Modelado, no ofrecido en la demo.</summary>
    ConcursoAcreedores,
    /// <summary>Credito incobrable (art. 80 Cuatro) → R3. Modelado, no ofrecido en la demo.</summary>
    CreditoIncobrable,
}

public static class TiposFacturaExtensiones
{
    public static bool EsRectificativa(this TipoFactura t) => t is TipoFactura.R1 or TipoFactura.R2 or TipoFactura.R3 or TipoFactura.R4 or TipoFactura.R5;

    public static TipoFactura CodigoRectificativa(this MotivoRectificacion motivo, TipoFactura tipoOriginal) =>
        tipoOriginal == TipoFactura.F2 ? TipoFactura.R5 : motivo switch
        {
            MotivoRectificacion.DevolucionODescuento => TipoFactura.R1,
            MotivoRectificacion.ConcursoAcreedores => TipoFactura.R2,
            MotivoRectificacion.CreditoIncobrable => TipoFactura.R3,
            _ => TipoFactura.R4,
        };

    public static string Etiqueta(this TipoFactura t) => t switch
    {
        TipoFactura.F1 => "Factura completa",
        TipoFactura.F2 => "Factura simplificada",
        TipoFactura.F3 => "Factura en sustitución de simplificadas",
        TipoFactura.R1 => "Rectificativa (art. 80 Uno, Dos y Seis)",
        TipoFactura.R2 => "Rectificativa (concurso)",
        TipoFactura.R3 => "Rectificativa (incobrable)",
        TipoFactura.R4 => "Rectificativa (resto)",
        TipoFactura.R5 => "Rectificativa de simplificada",
        _ => t.ToString()
    };

    public static string Etiqueta(this EstadoEnvio e) => e switch
    {
        EstadoEnvio.GENERADO => "Generado",
        EstadoEnvio.EN_COLA => "En cola de envío",
        EstadoEnvio.ENVIADO => "Enviado, esperando respuesta",
        EstadoEnvio.ACEPTADO => "Aceptado por la AEAT",
        EstadoEnvio.ACEPTADO_CON_ERRORES => "Aceptado con errores",
        EstadoEnvio.RECHAZADO => "Rechazado",
        EstadoEnvio.ERROR_TECNICO => "Error técnico (se reintentará)",
        EstadoEnvio.DEAD_LETTER => "Reintentos agotados",
        EstadoEnvio.BLOQUEADO_SIN_CERTIFICADO => "Bloqueado: sin certificado ni apoderamiento",
        EstadoEnvio.ERROR_VALIDACION => "Error de validación del XML",
        _ => e.ToString()
    };

    /// <summary>Lo que ve el cliente: sin jerga (envio-y-cola-reintentos.md §7).</summary>
    public static string EtiquetaCliente(this EstadoEnvio e) => e switch
    {
        EstadoEnvio.ACEPTADO or EstadoEnvio.ACEPTADO_CON_ERRORES => "Registrada en la AEAT",
        _ => "Pendiente de envío a la AEAT"
    };

    public static bool EsFinal(this EstadoEnvio e) => e is EstadoEnvio.ACEPTADO or EstadoEnvio.ACEPTADO_CON_ERRORES or EstadoEnvio.RECHAZADO;
}
