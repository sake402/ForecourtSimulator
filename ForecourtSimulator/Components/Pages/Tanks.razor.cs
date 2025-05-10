using ForecourtSimulator.Core;
using ForecourtSimulator.Services;
using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Client.Components.Draggable;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Common.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.IO.Ports;

namespace ForecourtSimulator.Components.Pages
{
    public partial class Tanks : BaseComponent, IDisposable, IManagedBaseComponent, IDraggableComponent
    {
        [ServiceInject] public SimulatorService Service { get; set; } = default!;
        [ServiceInject] public IJavaScriptRunner JavaScript { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            foreach (var tank in Service.TankSimulator.Tanks)
            {
                tank.OnStateChanged += Tank_OnStateChanged;
            }
            await base.OnInitializedAsync();
        }

        private void Tank_OnStateChanged(object? sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        public override void Dispose()
        {
            foreach (var tank in Service.TankSimulator.Tanks)
            {
                tank.OnStateChanged -= Tank_OnStateChanged;
            }
            base.Dispose();
        }

        Dictionary<Tank, ElementReference> probes = new Dictionary<Tank, ElementReference>();
        Dictionary<Tank, ElementReference> productFloats = new Dictionary<Tank, ElementReference>();
        Dictionary<Tank, ElementReference> waterFloats = new Dictionary<Tank, ElementReference>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JavaScript.Draggable(this,
                       DraggableFeatures.DragY | DraggableFeatures.PositionFeedbackAsPercentage,
                       Service.TankSimulator.Tanks.Select(tank => new Draggabble
                       {
                           DragConstraint = probes[tank],
                           DragHandle = productFloats[tank],
                           DragPosition = productFloats[tank],
                       }).Concat(Service.TankSimulator.Tanks.Select(tank => new Draggabble
                       {
                           DragConstraint = probes[tank],
                           DragHandle = waterFloats[tank],
                           DragPosition = waterFloats[tank],
                       })).ToArray());
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        [JSInvokable]
        public async ValueTask OnDragTo(ElementReference draggableElement, string draggableElementId, float x, float y)
        {
            var tank = productFloats.FirstOrDefault(p => p.Value.Id.Equals(draggableElement.Id)).Key;
            if (tank != null)
            {
                tank.ProductHeight = ((100 - y) * tank.Height) / 100;
                if (tank.WaterHeight > tank.ProductHeight)
                    tank.WaterHeight = tank.ProductHeight;
                await ((ITankStorage)Service).Store(tank.Address, tank.ProductHeight, tank.WaterHeight, tank.Temperature);
                await StateChanged();
            }
            else
            {
                tank = waterFloats.FirstOrDefault(p => p.Value.Id.Equals(draggableElement.Id)).Key;
                if (tank != null)
                {
                    tank.WaterHeight = ((100 - y) * tank.Height) / 100;
                    if (tank.WaterHeight > tank.ProductHeight)
                        tank.WaterHeight = tank.ProductHeight;
                    await ((ITankStorage)Service).Store(tank.Address, tank.ProductHeight, tank.WaterHeight, tank.Temperature);
                    await StateChanged();
                }
            }
        }
    }
}
