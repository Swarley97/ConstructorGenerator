using System;
using System.Collections.Generic;
using System.Text;

namespace ConstructorGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExcludeConstructorDependencyAttribute : Attribute
    {
    }
}