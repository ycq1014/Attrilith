using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Attrilith.Controller;

public class CamelCaseRouteAttribute : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var controllerName = controller.ControllerName.ToCamelCase();

        foreach (var selector in controller.Selectors)
        {
            if (selector.AttributeRouteModel is { Template: not null })
            {
                selector.AttributeRouteModel.Template =
                    selector.AttributeRouteModel.Template.Replace("[controller]", controllerName);
            }
        }
    }
}

public static class StringExtensions
{
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return char.ToLowerInvariant(str[0]).ToString();
        }

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}