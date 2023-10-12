using ConstructorGenerator.Tests.TestClasses;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class Tests
{
    [Test]
    public void When_Class_With_Base_Class_And_Full_ConstructorAttribute()
    {
        TestClassWithBaseClassAndFullConstructorAttribute testClass =
            new("fieldWithInitializer",
                "propertyWithInitializerButAttribute",
                "propertyWithInit",
                "propertyWithSetterButAttribute",
                "readonlyDependency",
                "notInitNameReadOnly",
                "duplicate",
                "baseA",
                "duplicate",
                "baseB");
        
        Assert.Multiple(() =>
        {
            Assert.That(testClass.FieldWithInitializerButAttribute, Is.EqualTo("fieldWithInitializer"));
            Assert.That(testClass.PropertyWithInitializerButAttribute, Is.EqualTo("propertyWithInitializerButAttribute"));
            Assert.That(testClass.PropertyWithInit, Is.EqualTo("propertyWithInit"));
            Assert.That(testClass.ReadonlyDependency, Is.EqualTo("readonlyDependency"));
            Assert.That(testClass.PropertyWithSetterButAttribute, Is.EqualTo("propertyWithSetterButAttribute"));
            Assert.That(testClass.PropertyReadOnly, Is.EqualTo("notInitNameReadOnly"));
            Assert.That(testClass.BaseAProperty, Is.EqualTo("baseA"));
            Assert.That(testClass.BaseBField, Is.EqualTo("baseB"));
            
            Assert.IsNull(testClass.FieldWithInitializer);
            Assert.IsNull(testClass.PropertyWithInitializer);
            Assert.IsNull(testClass.PropertyWithSetter);
            Assert.IsNull(testClass.ComputedProperty);
            Assert.IsNull(testClass.PropertyReadOnlyButIgnored);
        });
    }

    [Test]
    public void When_Class_With_Only_Optional_Parameters()
    {
        ClassWithOptionalDependencies testClassAllOptionalNotSet = new();
        Assert.IsNull(testClassAllOptionalNotSet.OptionalField);
        Assert.IsNull(testClassAllOptionalNotSet.OptionalProperty); 
        Assert.IsNull(testClassAllOptionalNotSet.OptionalFieldBase);
        Assert.IsNull(testClassAllOptionalNotSet.OptionalPropertyBase);
        
        
        // the same as above but this time with all optional parameters set
        ClassWithOptionalDependencies testClassAllOptionalSet = new("optionalField",
            "optionalProperty", "optionalFieldBase", "optionalPropertyBase");
        Assert.Multiple(() =>
        {
            Assert.That(testClassAllOptionalSet.OptionalField, Is.EqualTo("optionalField"));
            Assert.That(testClassAllOptionalSet.OptionalProperty, Is.EqualTo("optionalProperty"));
            Assert.That(testClassAllOptionalSet.OptionalFieldBase, Is.EqualTo("optionalFieldBase"));
            Assert.That(testClassAllOptionalSet.OptionalPropertyBase, Is.EqualTo("optionalPropertyBase"));
        });
    }

    [Test]
    public void When_Class_With_Base_And_GenerateBaseConstructorCallAttribute()
    {
        TestClassWithBaseButNoDependencies testClass = new("baseA", "duplicate","baseB");
        Assert.Multiple(() =>
        {
            Assert.That(testClass.BaseAProperty, Is.EqualTo("baseA"));
            Assert.That(testClass.BaseBField, Is.EqualTo("baseB"));
        });
    }
}