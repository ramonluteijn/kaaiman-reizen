using Kaaiman_reizen.Helpers;
using Microsoft.JSInterop;

namespace Kaaiman_reizen.Services;

public sealed class UserTimezoneService(IJSRuntime jsRuntime) : IUserTimezoneService
{
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private bool _isLoaded;
    private int _timezoneOffsetMinutes;

    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        await _loadSemaphore.WaitAsync();
        try
        {
            if (_isLoaded)
            {
                return;
            }

            try
            {
                _timezoneOffsetMinutes = await jsRuntime.InvokeAsync<int>("kaaimanDateTime.getTimezoneOffsetMinutes");
            }
            catch
            {
                _timezoneOffsetMinutes = 0;
            }

            _isLoaded = true;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public DateTime ToUserLocal(DateTime utcDateTime)
    {
        return DateDisplay.ToUserLocal(utcDateTime, _timezoneOffsetMinutes);
    }
}
