namespace Kaaiman_reizen.Services;

public interface IUserTimezoneService
{
    Task EnsureLoadedAsync();

    DateTime ToUserLocal(DateTime utcDateTime);
}
