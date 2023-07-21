namespace ConstructorGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExcludeConstructorDependencyAttribute : Attribute
    {
    }
}