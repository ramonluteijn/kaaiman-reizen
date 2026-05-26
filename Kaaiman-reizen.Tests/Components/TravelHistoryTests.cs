using System.Security.Claims;
using Kaaiman_reizen.Components.Pages.TravelLeaders.History;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kaaiman_reizen.Tests.Components;

public class TravelHistoryTests
{
    [Fact]
    public async Task OnInitializedAsync_ShowsOnlyJourneysWithEndDateBeforeToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expectedPastJourney = new Journey
        {
            Id = 1,
            Name = "Past Journey",
            Start = today.AddDays(-7),
            End = today.AddDays(-1)
        };

        var leader = new TravelLeader
        {
            Id = 42,
            Journeys =
            [
                expectedPastJourney,
                new Journey { Id = 2, Name = "Ends Today", Start = today.AddDays(-2), End = today },
                new Journey { Id = 3, Name = "Future Journey", Start = today.AddDays(1), End = today.AddDays(4) }
            ]
        };

        var component = BuildComponent([leader], CreateAuthenticatedUserWithLeaderClaim(42));

        await component.InitializeAsync();

        Assert.Single(component.Journeys);
        Assert.Equal(expectedPastJourney.Id, component.Journeys[0].Id);
    }

    [Fact]
    public async Task OnInitializedAsync_OrdersPastJourneysByEndThenStartDescending()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var leader = new TravelLeader
        {
            Id = 7,
            Journeys =
            [
                new Journey { Id = 1, Name = "Older End", Start = today.AddDays(-20), End = today.AddDays(-10) },
                new Journey { Id = 2, Name = "Same End Earlier Start", Start = today.AddDays(-12), End = today.AddDays(-5) },
                new Journey { Id = 3, Name = "Same End Later Start", Start = today.AddDays(-9), End = today.AddDays(-5) }
            ]
        };

        var component = BuildComponent([leader], CreateAuthenticatedUserWithLeaderClaim(7));

        await component.InitializeAsync();

        Assert.Equal(new[] { 3, 2, 1 }, component.Journeys.Select(j => j.Id));
    }

    private static TestableTravelHistory BuildComponent(IReadOnlyList<TravelLeader> leaders, ClaimsPrincipal user)
    {
        var component = new TestableTravelHistory();
        SetPrivateProperty(component, "LeaderService", new FakeTravelLeaderService(leaders));
        SetPrivateProperty(component, "AuthenticationStateProvider", new FakeAuthenticationStateProvider(user));
        return component;
    }

    private static ClaimsPrincipal CreateAuthenticatedUserWithLeaderClaim(int leaderId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("TravelLeaderId", leaderId.ToString())
        ], "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static void SetPrivateProperty(object instance, string propertyName, object value)
    {
        var property = instance.GetType().BaseType?.GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(property);
        property.SetValue(instance, value);
    }

    private sealed class TestableTravelHistory : TravelHistory
    {
        public Task InitializeAsync() => OnInitializedAsync();

        public IReadOnlyList<Journey> Journeys => _journeys;
    }

    private sealed class FakeAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state = new(user);

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    private sealed class FakeTravelLeaderService(IReadOnlyList<TravelLeader> leaders) : ITravelLeaderService
    {
        private readonly IReadOnlyList<TravelLeader> _leaders = leaders;

        public Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<TravelLeader>> GetTravelLeadersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_leaders);

        public Task AddTravelLeaderAsync(TravelLeader leader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeleteTravelLeaderAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TravelLeader?> GetTravelLeaderByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<TravelLeader?> GetTravelLeaderByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateTravelLeaderAsync(TravelLeader leader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<TravelLeader>> GetTravelLeadersWithoutPreferencesAsync() =>
            throw new NotImplementedException();

        public Task<List<TravelLeader>> GetTravelLeadersWithoutJourneysAsync(int year) =>
            throw new NotImplementedException();

        public Task<List<TravelLeader>> GetTravelLeadersWithNotesAsync() =>
            throw new NotImplementedException();

        public Task<List<Journey>> GetJourneysWithoutTravelLeadersAsync(int year) =>
            throw new NotImplementedException();

        public Task<List<TravelLeaderService.OverlapData>> GetTravelLeadersWithOverlappingJourneys() =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Journey>> GetJourneysOfTravelLeaderAsync(TravelLeader leader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}