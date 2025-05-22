using ForecourtSimulator.Core;
using ForecourtSimulator.Services;
using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Common.Logging;
using System.IO.Ports;

namespace ForecourtSimulator.Components.Pages
{
    public partial class SimulatorWorkBench : BaseComponent, IDisposable, IManagedBaseComponent
    {
        [ServiceInject] public SimulatorWorkBenchService Service { get; set; } = default!;

        Pumps? pumps;
        Tanks? tanks;
        bool loaded;
        protected override async Task OnInitializedAsync()
        {
            await Service.Initialize();
            loaded = true;
            await base.OnInitializedAsync();
        }

        async Task ServiceSaveState()
        {
            await Service.SaveState();
            if (pumps != null)
                await pumps.StateChanged();
            if (tanks != null)
                await tanks.StateChanged();
        }
        async Task ResetPumpStore()
        {
            await Service.ResetPumpStore();
            if (pumps != null)
                await pumps.StateChanged();
        }
        async Task ResetTankStore()
        {
            await Service.ResetTankStore();
            if (tanks != null)
                await tanks.StateChanged();
        }
    }
}
