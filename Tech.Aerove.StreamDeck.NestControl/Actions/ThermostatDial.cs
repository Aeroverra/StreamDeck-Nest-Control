using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.Client.Events.SharedModels;
using Aeroverra.StreamDeck.Client.Feedback;
using Google.Api;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Tech.Aerove.Tools.Nest.Models;

namespace Aeroverra.StreamDeck.NestControl.Actions
{
    [PluginAction("tech.aerove.streamdeck.nestcontrol.thermostatdial")]
    public class ThermostatDial : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private int MsDelay
        {
            get
            {
                int.TryParse($"{Context.Settings["delay"]}", out int val);
                if (val < 1000)
                {
                    val = 5000;
                }
                return val;
            }
        }


        private ThermostatDevice Thermostat { get; set; }

        private readonly ILogger<TemperatureDown> _logger;
        private readonly ExampleService _handler;
        public ThermostatDial(ILogger<TemperatureDown> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
            _ = AwaitDevice();
            _ = Ticker();
        }

        public override  async Task DialRotateAsync(DialRotatePayload payload)
        {
            if (Thermostat.Mode == ThermostatMode.OFF)
            {
                if(payload.Ticks > 0)
                {
                    Thermostat.SetMode(ThermostatMode.HEAT);
                }
                else
                {
                    Thermostat.SetMode(ThermostatMode.COOL);
                }
            }
            bool success = false;
            if (payload.Ticks > 0)
            {
                 success = Thermostat.SetTempUp(payload.Ticks);
            }
            else
            {
                success = Thermostat.SetTempDown(payload.Ticks * -1);
            }
            if (!success)
            {
                await Dispatcher.ShowAlertAsync();
            }
            else
            {
                await Dispatcher.ShowOkAsync();
                await Dispatcher.SetFeedbackAsync(new
                {
                    value = $"{Thermostat.SetPointRender}"
                });
            }
        }

        private async Task AwaitDevice()
        {
            while (true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(DeviceName))
                    {
                        await Task.Delay(2000);
                        continue;
                    }
                    var lookupThermostat = _handler.GetDevice(DeviceName);
                    Thermostat = lookupThermostat;
                    return;
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        public async Task Ticker()
        {
            var delay = 5000;
            while (true)
            {

                await Task.Delay(delay);
                if (string.IsNullOrWhiteSpace(DeviceName) || Thermostat == null) { continue; }
                try
                {
                    await Dispatcher.SetFeedbackAsync(new
                    {
                        key = "value",
                        type = "text",
                        rect = new[] { 76, 40, 108, 32 },
                        font = new { size = 40, weight = 600 },
                        alignment = "right"
                    });
                    var mode = Thermostat.Mode;
                    var modeText = $"";
                    if (Thermostat.Mode == ThermostatMode.COOL)
                    {
                        await Dispatcher.SetStateAsync(1);
                        await Dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                        modeText = $"{mode}\n{Thermostat.SetPointRender}";
                    }
                    if (Thermostat.Mode == ThermostatMode.HEAT)
                    {
                        await Dispatcher.SetStateAsync(1);
                        await Dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                        modeText = $"{mode}\n{Thermostat.SetPointRender}";
                    }
                    if (Thermostat.Mode == ThermostatMode.HEATCOOL)
                    {
                        await Dispatcher.SetStateAsync(1);
                        await Dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                        modeText = $"H&C\n{Thermostat.SetPointRender}";
                    }
                    if (Thermostat.Mode == ThermostatMode.OFF)
                    {
                        await Dispatcher.SetStateAsync(0);
                        await Dispatcher.SetImageAsync("");
                        modeText = $"{mode}";
                    }
                    await Dispatcher.SetTitleAsync($"{modeText}");
                    await Dispatcher.SetFeedbackAsync(new
                    {
                        value = $"{modeText}"
                    });
                    delay = MsDelay;
                    await Task.Delay(delay);
                    await Dispatcher.SetTitleAsync($"{Thermostat.CurrentTemperature}");
                    await Dispatcher.SetFeedbackAsync(new
                    {
                        value = $"{Thermostat.CurrentTemperature}"
                    });
                }
                catch (Exception)
                {

                }
            }
        }

    }
}
