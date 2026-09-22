namespace Aserta.Dominio.Clientes;

/// <summary>Vocabulario de 01-mapa-dominio.md §2. Se persisten como texto (nvarchar) con estos mismos nombres.</summary>
public enum FormaJuridica { Autonomo, SL, SA, CB, Particular }

public enum RegimenIrpf { DirectaNormal, DirectaSimplificada, Objetiva, NoAplica }

public enum RegimenIva { General, RecargoEquivalencia, Simplificado, CriterioCaja, Exento, NoAplica }

public enum PeriodicidadIva { Trimestral, Mensual, NoAplica }

public enum Territorio { Comun, Foral }

public enum EstadoCliente { Activo, Baja }
