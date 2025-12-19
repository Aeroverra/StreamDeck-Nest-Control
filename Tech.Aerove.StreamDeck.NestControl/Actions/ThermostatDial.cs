using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.Client.Events.SharedModels;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Extensions;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aeroverra.StreamDeck.NestControl.Actions
{
    /// <summary>
    /// Stream Deck + dial action for thermostat control with delayed API commits.
    /// </summary>
    [PluginActionAttribute("aeroverra.streamdeck.nestcontrol.thermostatdial")]
    public class ThermostatDial : ActionBase, IAsyncDisposable
    {
        private const int HoldToOffSeconds = 3;

        [Inject] private ILogger<ThermostatDial> _logger { get; set; } = null!;
        [Inject] private NestService _nestService { get; set; } = null!;

        private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

        private GoogleHomeEnterpriseSdmV1Device? _thermostat;

        private decimal? _pendingHeatF;
        private decimal? _pendingCoolF;
        private bool _hasPendingChange;

        private bool _menuOpen;
        private int _menuIndex;
        private List<MenuOption> _menuOptions = new List<MenuOption>();

        private DateTime _dialDownAt = DateTime.MinValue;
        private bool _disposed;

        private string DeviceName => $"{Context.Settings["device"]}";

        public override async Task OnInitializedAsync()
        {
            AttachEvents();

            await _stateLock.WaitAsync();
            try
            {
                await RefreshThermostatAsync();
                await RenderIdleAsync();
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public override async Task DidReceiveSettingsAsync(JObject settings)
        {
            await _stateLock.WaitAsync();
            try
            {
                await RefreshThermostatAsync();
                await RenderIdleAsync();
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public override async Task WillAppearAsync()
        {
            await _stateLock.WaitAsync();
            try
            {
                await RefreshThermostatAsync();
                await RenderIdleAsync();
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public override Task DialDownAsync(EncoderPayload payload)
        {
            _dialDownAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public override async Task DialUpAsync(EncoderPayload payload)
        {
            var heldFor = _dialDownAt == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - _dialDownAt;
            _dialDownAt = DateTime.MinValue;

            await _stateLock.WaitAsync();
            try
            {
                if (_thermostat == null)
                {
                    await RenderMessageAsync("No device", "Select a thermostat");
                    return;
                }

                if (heldFor.TotalSeconds >= HoldToOffSeconds)
                {
                    ClearPendingAdjustments();
                    _menuOpen = false;
                    await SetModeAsync(ThermostatMode.OFF);
                    await RenderIdleAsync();
                    return;
                }

                if (_menuOpen)
                {
                    await ApplyMenuSelectionAsync();
                }
                else
                {
                    OpenMenu();
                    await RenderMenuAsync();
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public override async Task DialRotateAsync(DialRotatePayload payload)
        {
            await _stateLock.WaitAsync();
            try
            {
                if (_thermostat == null)
                {
                    await RenderMessageAsync("No device", "Select a thermostat");
                    return;
                }

                if (_menuOpen)
                {
                    UpdateMenuSelection(payload.Ticks);
                    await RenderMenuAsync();
                    return;
                }

                var mode = _thermostat.GetThermostatMode().Mode;
                if (mode == ThermostatMode.OFF)
                {
                    await RenderMessageAsync("Off", "Press to choose mode");
                    return;
                }

                EnsurePendingSetpoints();
                ApplyTicks(mode, payload.Ticks);
                _hasPendingChange = true;

                await RenderAdjustingAsync(mode);
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public override async Task DialStopExtraLongAsync(DialRotatePayload payload)
        {
            await _stateLock.WaitAsync();
            try
            {
                if (_menuOpen || !_hasPendingChange || _thermostat == null)
                {
                    return;
                }

                var mode = _thermostat.GetThermostatMode().Mode;
                if (mode == ThermostatMode.OFF)
                {
                    return;
                }

                var (heatCelsius, coolCelsius) = GetPendingCelsius(mode);

                await RenderMessageAsync("Applying…", $"{heatCelsius.ToFahrenheit():F0}°");

                var success = await _nestService.SetTemp(_thermostat, heatCelsius, coolCelsius);
                if (success)
                {
                    await Dispatcher.ShowOkAsync();
                }
                else
                {
                    await Dispatcher.ShowAlertAsync();
                }

                ClearPendingAdjustments();
                await RenderIdleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send temperature update.");
                await Dispatcher.ShowAlertAsync();
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private void ApplyTicks(ThermostatMode mode, int ticks)
        {
            if (ticks == 0)
            {
                return;
            }

            switch (mode)
            {
                case ThermostatMode.HEAT:
                    _pendingHeatF = (_pendingHeatF ?? CurrentHeatF()) + ticks;
                    break;
                case ThermostatMode.COOL:
                    _pendingCoolF = (_pendingCoolF ?? CurrentCoolF()) + ticks;
                    break;
                case ThermostatMode.HEATCOOL:
                    _pendingHeatF = (_pendingHeatF ?? CurrentHeatF()) + ticks;
                    _pendingCoolF = (_pendingCoolF ?? CurrentCoolF()) + ticks;
                    break;
            }
        }

        private (decimal heatCelsius, decimal coolCelsius) GetPendingCelsius(ThermostatMode mode)
        {
            var heatF = _pendingHeatF ?? CurrentHeatF();
            var coolF = _pendingCoolF ?? CurrentCoolF();

            return (heatF.ToCelsius(), coolF.ToCelsius());
        }

        private decimal CurrentHeatF()
        {
            var setPoint = _thermostat!.GetThermostatSetPoint();
            return setPoint.HeatCelsius.ToFahrenheit();
        }

        private decimal CurrentCoolF()
        {
            var setPoint = _thermostat!.GetThermostatSetPoint();
            return setPoint.CoolCelsius.ToFahrenheit();
        }

        private void EnsurePendingSetpoints()
        {
            if (_pendingHeatF == null || _pendingCoolF == null)
            {
                var setPoint = _thermostat!.GetThermostatSetPoint();
                _pendingHeatF ??= setPoint.HeatCelsius.ToFahrenheit();
                _pendingCoolF ??= setPoint.CoolCelsius.ToFahrenheit();
            }
        }

        private void ClearPendingAdjustments()
        {
            _pendingHeatF = null;
            _pendingCoolF = null;
            _hasPendingChange = false;
        }

        private void OpenMenu()
        {
            _menuOptions = BuildMenuOptions();
            _menuIndex = 0;
            _menuOpen = true;
        }

        private List<MenuOption> BuildMenuOptions()
        {
            var options = new List<MenuOption>
            {
                MenuOption.Exit()
            };

            var currentMode = _thermostat?.GetThermostatMode().Mode ?? ThermostatMode.OFF;

            foreach (var mode in new[] { ThermostatMode.HEAT, ThermostatMode.HEATCOOL, ThermostatMode.COOL })
            {
                if (mode == currentMode)
                {
                    options.Add(MenuOption.Off());
                }
                else
                {
                    options.Add(MenuOption.FromMode(mode));
                }
            }

            return options;
        }

        private void UpdateMenuSelection(int ticks)
        {
            if (_menuOptions.Count == 0 || ticks == 0)
            {
                return;
            }

            _menuIndex = ((_menuIndex + ticks) % _menuOptions.Count + _menuOptions.Count) % _menuOptions.Count;
        }

        private async Task ApplyMenuSelectionAsync()
        {
            if (_menuOptions.Count == 0)
            {
                _menuOpen = false;
                await RenderIdleAsync();
                return;
            }

            var selection = _menuOptions[_menuIndex];
            _menuOpen = false;
            ClearPendingAdjustments();

            switch (selection.Kind)
            {
                case MenuOptionKind.Exit:
                    await RenderIdleAsync();
                    return;
                case MenuOptionKind.Off:
                    await SetModeAsync(ThermostatMode.OFF);
                    break;
                case MenuOptionKind.Mode:
                    if (selection.Mode.HasValue)
                    {
                        await SetModeAsync(selection.Mode.Value);
                    }
                    break;
            }

            await RenderIdleAsync();
        }

        private async Task SetModeAsync(ThermostatMode mode)
        {
            if (_thermostat == null)
            {
                return;
            }

            var success = await _nestService.SetMode(_thermostat, mode);
            if (success)
            {
                await Dispatcher.ShowOkAsync();

                var modeTrait = _thermostat.GetThermostatMode();
                modeTrait.Mode = mode;
            }
            else
            {
                await Dispatcher.ShowAlertAsync();
            }
        }

        private async Task RefreshThermostatAsync()
        {
            _thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        private async void NestService_OnDeviceUpdated(object? sender, GoogleHomeEnterpriseSdmV1Device device)
        {
            if (device.Name != DeviceName)
            {
                return;
            }

            await _stateLock.WaitAsync();
            try
            {
                _thermostat = device;

                if (!_menuOpen && !_hasPendingChange)
                {
                    await RenderIdleAsync();
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private async void NestService_OnConnected(object? sender, EventArgs e)
        {
            await _stateLock.WaitAsync();
            try
            {
                await RefreshThermostatAsync();
                await RenderIdleAsync();
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private async Task RenderIdleAsync()
        {
            await Dispatcher.SetImageAsync(string.Empty);
            await Dispatcher.SetTitleAsync(string.Empty);

            if (_thermostat == null)
            {
                // Switch to default layout (small text) for setup message
                await Dispatcher.SetFeedbackLayoutAsync("Layouts/ThermostatDialDefaultLayout.json");
                await Dispatcher.SetFeedbackAsync(new
                {
                    title = "Not linked",
                    value = "Select device"
                });
                return;
            }

            // Switch to active layout (large text) for temperature values
            await Dispatcher.SetFeedbackLayoutAsync("Layouts/ThermostatDialLayout.json");

            var mode = _thermostat.GetThermostatMode().Mode;
            var ambient = _thermostat.GetThermostatRenderedTemperature(TemperatureScale.FAHRENHEIT);

            if (mode == ThermostatMode.OFF)
            {
                await Dispatcher.SetFeedbackAsync(new
                {
                    title = $"Inside {ambient}°",
                    value = "OFF"
                });
                return;
            }

            var setPoint = _thermostat.GetThermostatRenderedSetPoint(TemperatureScale.FAHRENHEIT);
            var modeLabel = ModeLabel(mode);

            await Dispatcher.SetFeedbackAsync(new
            {
                title = $"{modeLabel} • Inside {ambient}°",
                value = $"{setPoint}°"
            });
        }

        private async Task RenderAdjustingAsync(ThermostatMode mode)
        {
            await Dispatcher.SetImageAsync(string.Empty);
            await Dispatcher.SetTitleAsync(string.Empty);

            var main = mode switch
            {
                ThermostatMode.HEAT => $"{_pendingHeatF?.ToString("F0")}°",
                ThermostatMode.COOL => $"{_pendingCoolF?.ToString("F0")}°",
                ThermostatMode.HEATCOOL => $"{_pendingHeatF?.ToString("F0")}-{_pendingCoolF?.ToString("F0")}°",
                _ => "Adjust"
            };

            await Dispatcher.SetFeedbackAsync(new
            {
                title = "Adjusting",
                value = main
            });
        }

        private async Task RenderMenuAsync()
        {
            await Dispatcher.SetImageAsync(string.Empty);
            await Dispatcher.SetTitleAsync(string.Empty);

            if (_menuOptions.Count == 0)
            {
                await Dispatcher.SetFeedbackAsync(new
                {
                    title = "Mode",
                    value = "Exit"
                });
                return;
            }

            var option = _menuOptions[_menuIndex];

            await Dispatcher.SetFeedbackAsync(new
            {
                title = "Mode",
                value = option.Label
            });
        }

        private async Task RenderMessageAsync(string title, string? value)
        {
            await Dispatcher.SetImageAsync(string.Empty);
            await Dispatcher.SetTitleAsync(string.Empty);

            await Dispatcher.SetFeedbackAsync(new
            {
                title,
                value = value ?? string.Empty
            });
        }

        private static string ModeLabel(ThermostatMode mode)
        {
            return mode switch
            {
                ThermostatMode.HEAT => "Heat",
                ThermostatMode.COOL => "Cool",
                ThermostatMode.HEATCOOL => "H&C",
                _ => "Off"
            };
        }

        private void AttachEvents()
        {
            _nestService.OnDeviceUpdated += NestService_OnDeviceUpdated;
            _nestService.OnConnected += NestService_OnConnected;
        }

        private void DetachEvents()
        {
            _nestService.OnDeviceUpdated -= NestService_OnDeviceUpdated;
            _nestService.OnConnected -= NestService_OnConnected;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            _stateLock.Dispose();
            DetachEvents();

            await Task.CompletedTask;
        }

        private sealed class MenuOption
        {
            public MenuOptionKind Kind { get; init; }
            public ThermostatMode? Mode { get; init; }
            public required string Label { get; init; }

            public static MenuOption Exit() => new MenuOption { Kind = MenuOptionKind.Exit, Label = "Exit" };
            public static MenuOption Off() => new MenuOption { Kind = MenuOptionKind.Off, Label = "Off" };
            public static MenuOption FromMode(ThermostatMode mode) => new MenuOption { Kind = MenuOptionKind.Mode, Mode = mode, Label = ModeLabel(mode) };
        }

        private enum MenuOptionKind
        {
            Exit,
            Mode,
            Off
        }
    }
}
