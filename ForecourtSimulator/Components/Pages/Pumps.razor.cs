using ForecourtSimulator.Core;
using ForecourtSimulator.Services;
using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Common.Logging;
using System.IO.Ports;

namespace ForecourtSimulator.Components.Pages
{
    public partial class Pumps : BaseComponent, IDisposable, IManagedBaseComponent
    {
        [ServiceInject] public SimulatorService Service { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            foreach (var pump in Service.PumpSimulator.Pumps)
            {
                pump.OnStateChanged += Pump_OnStateChanged;
            }
            await base.OnInitializedAsync();
        }

        private void Pump_OnStateChanged(object? sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        public override void Dispose()
        {
            foreach (var pump in Service.PumpSimulator.Pumps)
            {
                pump.OnStateChanged -= Pump_OnStateChanged;
            }
            base.Dispose();
        }
    }
}
