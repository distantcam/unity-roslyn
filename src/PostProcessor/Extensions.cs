using System.Linq;
using Mono.Cecil;

static class Extensions
{
    public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
    {
        if (provider == null || !provider.HasCustomAttributes)
            return false;

        return provider.CustomAttributes
            .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
    }
}