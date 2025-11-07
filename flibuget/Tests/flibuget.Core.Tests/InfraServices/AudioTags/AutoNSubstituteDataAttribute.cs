using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;

namespace flibuget.Core.Tests.InfraServices.AudioTags;

public class AutoNSubstituteDataAttribute()
    : AutoDataAttribute(() => new Fixture().Customize(new AutoNSubstituteCustomization()));
