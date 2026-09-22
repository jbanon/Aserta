using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aserta.Web.Infraestructura;

/// <summary>
/// Con cultura es-ES el binder estandar rechaza "1234.56" (que es lo que envia
/// input type="number"). Este binder acepta coma y punto como separador decimal.
/// </summary>
public sealed class ProveedorBinderDecimal : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var t = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return t == typeof(decimal) ? new BinderDecimal() : null;
    }

    private sealed class BinderDecimal : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext ctx)
        {
            var valor = ctx.ValueProvider.GetValue(ctx.ModelName);
            if (valor == ValueProviderResult.None) return Task.CompletedTask;
            ctx.ModelState.SetModelValue(ctx.ModelName, valor);
            var texto = valor.FirstValue?.Trim();
            if (string.IsNullOrEmpty(texto))
            {
                if (Nullable.GetUnderlyingType(ctx.ModelType) is not null) ctx.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }
            // "1.234,56" => "1234.56"; "1234.56" => igual; "1234,56" => "1234.56"
            var normalizado = texto.Contains(',') ? texto.Replace(".", "").Replace(',', '.') : texto;
            if (decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                ctx.Result = ModelBindingResult.Success(d);
            else
                ctx.ModelState.TryAddModelError(ctx.ModelName, "Introduzca un número válido.");
            return Task.CompletedTask;
        }
    }
}
