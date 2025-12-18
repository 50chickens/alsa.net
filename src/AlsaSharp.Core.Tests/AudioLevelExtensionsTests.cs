using AlsaSharp.Library.Extensions;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class AudioLevelExtensionsTests
{
    [Test]
    public void ToRms_WithZeroDbfs_ReturnsOne()
    {
        // Arrange
        const double dbfs = 0.0;

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(result, Is.EqualTo(1.0).Within(0.0001));
    }

    [Test]
    public void ToRms_WithNegative20Dbfs_ReturnsCorrectValue()
    {
        // Arrange
        const double dbfs = -20.0;
        double expected = Math.Pow(10.0, -20.0 / 20.0); // 0.1

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(result, Is.EqualTo(expected).Within(0.0001));
    }

    [Test]
    public void ToRms_WithNegative6Dbfs_ReturnsCorrectValue()
    {
        // Arrange
        const double dbfs = -6.0;
        double expected = Math.Pow(10.0, -6.0 / 20.0); // approximately 0.5012

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(result, Is.EqualTo(expected).Within(0.0001));
    }

    [Test]
    public void ToRms_WithPositiveDbfs_ReturnsCorrectValue()
    {
        // Arrange
        const double dbfs = 6.0;
        double expected = Math.Pow(10.0, 6.0 / 20.0); // approximately 1.995

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(result, Is.EqualTo(expected).Within(0.0001));
    }

    [Test]
    public void ToRms_WithNaN_ReturnsNaN()
    {
        // Arrange
        double dbfs = double.NaN;

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(double.IsNaN(result), Is.True);
    }

    [Test]
    public void ToRms_WithNegativeInfinity_ReturnsZero()
    {
        // Arrange
        double dbfs = double.NegativeInfinity;

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void ToRms_WithVeryLargeNegativeValue_ReturnsSmallPositiveValue()
    {
        // Arrange
        const double dbfs = -100.0;

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(result, Is.GreaterThan(0.0));
        Assert.That(result, Is.LessThan(0.0001));
    }

    [Test]
    public void ToRms_WithPositiveInfinity_ReturnsPositiveInfinity()
    {
        // Arrange
        double dbfs = double.PositiveInfinity;

        // Act
        var result = dbfs.ToRms();

        // Assert
        Assert.That(double.IsPositiveInfinity(result), Is.True);
    }

    [TestCase(-40.0)]
    [TestCase(-30.0)]
    [TestCase(-10.0)]
    [TestCase(0.0)]
    [TestCase(10.0)]
    public void ToRms_IsMonotonicIncreasing(double dbfs)
    {
        // Act
        var result1 = dbfs.ToRms();
        var result2 = (dbfs + 1).ToRms();

        // Assert - Higher dBFS should result in higher RMS
        Assert.That(result2, Is.GreaterThan(result1));
    }
}
