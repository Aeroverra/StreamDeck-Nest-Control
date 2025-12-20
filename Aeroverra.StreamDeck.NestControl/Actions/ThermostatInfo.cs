using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.NestControl.Services;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Actions
{
    [PluginActionAttribute("aeroverra.streamdeck.nestcontrol.thermostatinfo")]
    public class ThermostatInfo : ActionBase, IAsyncDisposable
    {
        private string DeviceName => $"{Context.Settings["device"]}";

        private int MsDelay
        {
            get
            {
                try
                {
                    int.TryParse($"{Context.Settings["delay"]}", out int val);
                    if (val < 1000)
                    {
                        val = 5000;
                    }
                    return val;
                }
                catch (Exception e)
                {
                    return 200;
                }
            }
        }

        private bool StopAnimation
        {
            get
            {
                try
                {
                    if (Context.Settings.TryGetValue("stopAnimation", out var val))
                    {
                        return Convert.ToBoolean(val);
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        [Inject] private ILogger<ThermostatInfo> _logger { get; set; } = null!;

        [Inject] private DisplayService _displayService { get; set; } = null!;

        [Inject] private NestService _nestService { get; set; } = null!;

        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private GoogleHomeEnterpriseSdmV1Device? Thermostat { get; set; }

        public override async Task OnInitializedAsync()
        {
            await _lock.WaitAsync();
            try
            {
                await Dispatcher.SetTitleAsync($"Aero");

                _nestService.OnDeviceUpdated += NestService_OnDeviceUpdated;

                Thermostat = _nestService.Devices
                    .Where(x => x.Name == DeviceName)
                    .FirstOrDefault();

                _ = Display("setpoint");
            }
            finally
            {
                try { _lock.Release(); } catch (ObjectDisposedException) { }
            }
        }

        private async void NestService_OnDeviceUpdated(object? sender, GoogleHomeEnterpriseSdmV1Device e)
        {
            if (e.Name == DeviceName)
            {
                Thermostat = e;
                await UpdateDisplay("setpoint");
            }
        }

        public override async Task DidReceiveSettingsAsync(JObject settings)
        {
            if (Thermostat?.Name != DeviceName)
            {
                Thermostat = _nestService.Devices
                    .Where(x => x.Name == DeviceName)
                    .FirstOrDefault();

                await UpdateDisplay();
            }
        }

        private async Task UpdateDisplay(string? viewName = null)
        {
            await _lock.WaitAsync();
            try
            {
                try { _cancellationTokenSource.Cancel(); } catch { }
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                _ = Display(viewName);
            }
            finally
            {
                try { _lock.Release(); } catch (ObjectDisposedException) { }
            }
        }

        public async Task Display(string? viewName = null)
        {
            var displayNext = viewName ?? "temperature";
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (StopAnimation)
                    {
                        await _displayService.DisplayStatic(Dispatcher, Thermostat);
                    }
                    else
                    {
                        if (displayNext == "setpoint")
                        {
                            await _displayService.DisplaySetPoint(Dispatcher, Thermostat);
                        }
                        else
                        {
                            await _displayService.DisplayTemperature(Dispatcher, Thermostat);
                        }

                        if (displayNext == "setpoint")
                        {
                            displayNext = "temperature";
                        }
                        else
                        {
                            displayNext = "setpoint";
                        }
                    }

                    await Task.Delay(MsDelay, _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in display loop for ThermostatInfo action.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogWarning("ThermostatInfo Action Disposed for Device: {DeviceName}", DeviceName);
            _lock.Dispose();
            _nestService.OnDeviceUpdated -= NestService_OnDeviceUpdated;
            try { await _cancellationTokenSource.CancelAsync(); } catch { }
            _cancellationTokenSource.Dispose();
        }
    }
}
