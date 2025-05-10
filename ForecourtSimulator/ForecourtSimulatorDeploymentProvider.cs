using LivingThing.Core.Frameworks.Common.Services;
using LivingThing.Core.Frameworks.Common.Data;
using LivingThing.Core.Frameworks.Common.OneOf;
using LivingThing.Core.Frameworks.Common;

namespace ForecourtSimulator
{
    public class ForecourtSimulatorDeploymentProvider : IApplicationDeploymentProvider
    {
        public string? MediaPlaceholderPath { get; }
        public Type? LoadingComponent { get; }
        public string? Title => "ForecourtSimulator";
        public string? Description { get; }
        public IconDescriptor Icon { get; }
        public OneOf<string, Type> LoginComponent { get; }
    }
}
