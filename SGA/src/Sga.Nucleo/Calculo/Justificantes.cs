using System.Security.Cryptography;

namespace Sga.Nucleo.Calculo;

/// <summary>
/// Identificadores con el mismo aspecto que los del justificante real de la AEAT
/// (expediente, codigo seguro de verificacion, numero de justificante). Son SIMULADOS:
/// la demo no se conecta a la AEAT y lo dice en pantalla.
/// </summary>
public static class Justificantes
{
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Csv() => string.Concat(Enumerable.Range(0, 16).Select(_ => Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]));

    /// <summary>Expediente: ejercicio + modelo + 10 digitos + letra, como "202630317710378V".</summary>
    public static string Expediente(short ejercicio, string modelo) =>
        $"{ejercicio}{modelo.PadLeft(3, '0')}{RandomNumberGenerator.GetInt32(1_000_000_000):0000000000}{Alfabeto[RandomNumberGenerator.GetInt32(24)]}";

    /// <summary>Numero de justificante: modelo + 10 digitos, como "3037198161462".</summary>
    public static string NumeroJustificante(string modelo) =>
        $"{modelo.PadLeft(3, '0')}{RandomNumberGenerator.GetInt32(1_000_000_000):0000000000}";
}
