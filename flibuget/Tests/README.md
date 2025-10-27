# Flibuget Test Projects

This directory contains unit tests for the Flibuget application.

## Test Projects

### flibuget.Tests
Unit tests for the core `flibuget` project including:
- ViewModels
- Services
- Dependency Injection configuration

### flibuget.Desktop.Tests
Unit tests for the `flibuget.Desktop` project including:
- Application startup
- Desktop-specific functionality

## Testing Frameworks and Libraries

All test projects use the following testing stack:

### Test Framework
- **xUnit** - Modern testing framework for .NET

### Test Data & Mocking
- **AutoFixture** - Generates test data automatically
- **AutoFixture.Xunit2** - Integrates AutoFixture with xUnit via `[AutoData]` attribute
- **NSubstitute** - Easy mocking library
- **AutoFixture.AutoNSubstitute** - Auto-generates mocks using NSubstitute

### Assertions
- **FluentAssertions** - Readable and expressive assertion library

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Tests for a Specific Project
```bash
dotnet test Tests/flibuget.Tests/flibuget.Tests.csproj
dotnet test Tests/flibuget.Desktop.Tests/flibuget.Desktop.Tests.csproj
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Tests in Watch Mode
```bash
dotnet watch test --project Tests/flibuget.Tests/flibuget.Tests.csproj
```

## Writing Tests

### Basic Test Example

```csharp
using FluentAssertions;
using Xunit;

public class MyServiceTests
{
    [Fact]
    public void MyMethod_ShouldReturnExpectedValue()
    {
    // Arrange
        var sut = new MyService();

        // Act
        var result = sut.MyMethod();

 // Assert
        result.Should().Be("expected value");
    }
}
```

### Using AutoFixture

```csharp
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Xunit;

public class MyServiceTests
{
private readonly IFixture _fixture;

    public MyServiceTests()
    {
  _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
 public void MyMethod_ShouldProcessData()
    {
     // Arrange
        var sut = _fixture.Create<MyService>();
        var input = _fixture.Create<string>();

        // Act
        var result = sut.MyMethod(input);

    // Assert
        result.Should().NotBeNull();
    }
}
```

### Using AutoData Attribute

```csharp
using AutoFixture.Xunit2;
using FluentAssertions;
using Xunit;

public class MyServiceTests
{
    [Theory]
 [AutoData]
    public void MyMethod_ShouldHandleAnyInput(string input, int count)
    {
  // Arrange
      var sut = new MyService();

        // Act
        var result = sut.MyMethod(input, count);

        // Assert
   result.Should().NotBeNullOrEmpty();
    }
}
```

### Mocking with NSubstitute

```csharp
using NSubstitute;
using FluentAssertions;
using Xunit;

public class MyServiceTests
{
    [Fact]
    public void MyMethod_ShouldCallDependency()
    {
 // Arrange
        var mockDependency = Substitute.For<IMyDependency>();
        mockDependency.GetData().Returns("test data");
        var sut = new MyService(mockDependency);

   // Act
        var result = sut.MyMethod();

      // Assert
        mockDependency.Received(1).GetData();
        result.Should().Be("test data");
    }
}
```

### Testing with ILogger

```csharp
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class MyServiceTests
{
    [Fact]
    public void MyMethod_ShouldLogInformation()
    {
        // Arrange
      var mockLogger = Substitute.For<ILogger<MyService>>();
        var sut = new MyService(mockLogger);

// Act
        sut.MyMethod();

        // Assert
     mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("expected message")),
          Arg.Any<Exception>(),
Arg.Any<Func<object, Exception?, string>>());
    }
}
```

## Test Organization

Tests are organized to mirror the structure of the main project:

```
Tests/
??? flibuget.Tests/
?   ??? ViewModels/
?   ?   ??? MainViewModelTests.cs
? ??? Services/
?   ?   ??? MyServiceTests.cs
?   ??? CompositionRootTests.cs
??? flibuget.Desktop.Tests/
??? ProgramTests.cs
```

## Best Practices

1. **Follow AAA Pattern**: Arrange, Act, Assert
2. **One Assertion Per Test**: Keep tests focused
3. **Meaningful Test Names**: Use descriptive names that explain what is being tested
4. **Use FluentAssertions**: More readable than Assert.Equal
5. **Mock External Dependencies**: Keep tests isolated and fast
6. **Use AutoFixture for Test Data**: Avoid hard-coded test values
7. **Test Behavior, Not Implementation**: Focus on what the code does, not how

## Continuous Integration

Tests are automatically run on:
- Every commit to develop branch
- Every pull request
- Before deployment to production

## Code Coverage

Target: **80% minimum code coverage**

View coverage reports after running:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

Coverage reports are generated in `TestResults/` directory.

## Troubleshooting

### Tests Not Discovered
- Ensure test methods are `public`
- Verify `[Fact]` or `[Theory]` attributes are present
- Rebuild the solution

### Mock Not Working
- Check that the interface/method is `virtual` or part of an interface
- Verify NSubstitute is properly configured

### AutoFixture Errors
- Some types may need custom builders
- Check for circular dependencies in constructors
- Consider using `Fixture.Inject<T>()` for specific instances

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [AutoFixture Documentation](https://github.com/AutoFixture/AutoFixture)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
