using LivingThing.Core.Frameworks.Client.Components;
using LivingThing.Core.Frameworks.Client.Interface;
using LivingThing.Core.Frameworks.Client.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForecourtSimulator.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] public JavaScriptRunner JavaScript { get;set;}=default!;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                JavaScript.Ready();
            }
            base.OnAfterRender(firstRender);
        }
    }
}
