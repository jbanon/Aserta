namespace Aserta.Dominio.Comun;

/// <summary>
/// Validacion sintactica y de digito de control de NIF (persona fisica), NIE y
/// CIF (persona juridica). Vive en el dominio y no en un CHECK de SQL porque las
/// reglas son ricas y necesitan mensaje explicativo (04-modelo-datos.md §3.1).
/// </summary>
public static class ValidadorNif
{
    private const string LetrasNif = "TRWAGMYFPDXBNJZSQVHLCKE";
    private const string LetrasCif = "JABCDEFGHI";

    public enum TipoIdentificador { NifPersonaFisica, Nie, Cif }

    public sealed record Resultado(bool EsValido, string? Normalizado, TipoIdentificador? Tipo, string? Error)
    {
        public static Resultado Ok(string normalizado, TipoIdentificador tipo) => new(true, normalizado, tipo, null);
        public static Resultado Fallo(string error) => new(false, null, null, error);
    }

    public static string Normalizar(string? valor) =>
        (valor ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "").Replace(".", "");

    public static Resultado Validar(string? valor)
    {
        var nif = Normalizar(valor);
        if (nif.Length == 0) return Resultado.Fallo("El NIF es obligatorio.");
        if (nif.Length != 9) return Resultado.Fallo("El NIF debe tener 9 caracteres.");

        char primera = nif[0];
        if (char.IsDigit(primera) || primera is 'K' or 'L' or 'M')
            return ValidarPersonaFisica(nif);
        if (primera is 'X' or 'Y' or 'Z')
            return ValidarNie(nif);
        if (LetraCifValida(primera))
            return ValidarCif(nif);

        return Resultado.Fallo($"'{nif}' no tiene un formato de NIF, NIE ni CIF reconocible.");
    }

    public static bool EsValido(string? valor) => Validar(valor).EsValido;

    /// <summary>Construye un NIF de persona fisica sintacticamente valido a partir de un numero (para datos ficticios y tests).</summary>
    public static string ConstruirNif(int numero)
    {
        var cuerpo = numero.ToString("00000000");
        return cuerpo + LetrasNif[int.Parse(cuerpo) % 23];
    }

    /// <summary>Construye un CIF sintacticamente valido: letra de entidad + 7 digitos + control (para datos ficticios y tests).</summary>
    public static string ConstruirCif(char letra, int numero)
    {
        var digitos = numero.ToString("0000000");
        int sumaPares = 0, sumaImpares = 0;
        for (int i = 0; i < 7; i++)
        {
            int d = digitos[i] - '0';
            if (i % 2 == 0) { int doble = d * 2; sumaImpares += doble / 10 + doble % 10; }
            else sumaPares += d;
        }
        int control = (10 - (sumaPares + sumaImpares) % 10) % 10;
        bool letraControl = "KPQSNW".Contains(letra);
        return letra + digitos + (letraControl ? LetrasCif[control] : (char)('0' + control));
    }

    private static Resultado ValidarPersonaFisica(string nif)
    {
        // NIF normal: 8 digitos + letra. K/L/M: letra + 7 digitos + letra.
        string cuerpo = nif[..8];
        char letra = nif[8];
        if (nif[0] is 'K' or 'L' or 'M')
        {
            cuerpo = "0" + nif[1..8];
        }
        if (!cuerpo.All(char.IsDigit))
            return Resultado.Fallo("Un NIF de persona fisica son 8 digitos seguidos de una letra.");
        char esperada = LetrasNif[int.Parse(cuerpo) % 23];
        if (letra != esperada)
            return Resultado.Fallo($"La letra de control del NIF no es correcta (se esperaba '{esperada}').");
        return Resultado.Ok(nif, TipoIdentificador.NifPersonaFisica);
    }

    private static Resultado ValidarNie(string nie)
    {
        string prefijo = nie[0] switch { 'X' => "0", 'Y' => "1", _ => "2" };
        string cuerpo = prefijo + nie[1..8];
        if (!cuerpo.All(char.IsDigit))
            return Resultado.Fallo("Un NIE es X/Y/Z seguido de 7 digitos y una letra.");
        char esperada = LetrasNif[int.Parse(cuerpo) % 23];
        if (nie[8] != esperada)
            return Resultado.Fallo($"La letra de control del NIE no es correcta (se esperaba '{esperada}').");
        return Resultado.Ok(nie, TipoIdentificador.Nie);
    }

    private static bool LetraCifValida(char c) => "ABCDEFGHJNPQRSUVW".Contains(c);

    private static Resultado ValidarCif(string cif)
    {
        string digitos = cif[1..8];
        if (!digitos.All(char.IsDigit))
            return Resultado.Fallo("Un CIF es una letra, 7 digitos y un caracter de control.");

        int sumaPares = 0, sumaImpares = 0;
        for (int i = 0; i < 7; i++)
        {
            int d = digitos[i] - '0';
            if (i % 2 == 0) // posiciones impares (1,3,5,7) en notacion 1-based
            {
                int doble = d * 2;
                sumaImpares += doble / 10 + doble % 10;
            }
            else sumaPares += d;
        }
        int total = sumaPares + sumaImpares;
        int digitoControl = (10 - total % 10) % 10;
        char controlNumero = (char)('0' + digitoControl);
        char controlLetra = LetrasCif[digitoControl];
        char control = cif[8];

        // Entidades cuyo control es obligatoriamente letra: K, P, Q, S (y N, W). Con digito: A, B, E, H. Resto: cualquiera.
        bool debeSerLetra = "KPQSNW".Contains(cif[0]);
        bool debeSerNumero = "ABEH".Contains(cif[0]);

        if (debeSerLetra && control != controlLetra)
            return Resultado.Fallo($"El caracter de control del CIF no es correcto (se esperaba '{controlLetra}').");
        if (debeSerNumero && control != controlNumero)
            return Resultado.Fallo($"El digito de control del CIF no es correcto (se esperaba '{controlNumero}').");
        if (!debeSerLetra && !debeSerNumero && control != controlLetra && control != controlNumero)
            return Resultado.Fallo($"El caracter de control del CIF no es correcto (se esperaba '{controlNumero}' o '{controlLetra}').");

        return Resultado.Ok(cif, TipoIdentificador.Cif);
    }
}
