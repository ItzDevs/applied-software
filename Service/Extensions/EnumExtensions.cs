namespace AppliedSoftware.Extensions;

public static class EnumExtensions
{
    public static IEnumerable<Enum> GetFlags(this Enum value)
        => Enum.GetValues(value.GetType()).Cast<Enum>().Where(value.HasFlag);
    
}