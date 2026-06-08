using MudBlazor;
using Kaaiman_reizen.Helpers;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerRoundDraft
{
    private async Task SaveDraftAsync()
    {
        await SaveAsync(isPublished: false);
    }

    private async Task PublishPlanningAsync()
    {
        await SaveAsync(isPublished: true);
    }

    private async Task SaveAsync(bool isPublished)
    {
        if (_round is null || _request is null || _result is null || !_result.IsSuccess)
            return;

        if (isPublished && !CanPublish)
        {
            SetSaveMessage("Publiceren niet mogelijk: niet alle reizen zijn volledig bezet.", Severity.Warning);
            _snackbar.Add("Niet alle reizen hebben het vereiste aantal reisleiders.", Severity.Warning);
            return;
        }

        _isSaving = true;
        ClearSaveMessage();

        try
        {
            var assignments = MapToAssignmentDictionary();
            var userLocalNow = await GetUserLocalNowAsync();

            string planningName = isPublished
                ? $"Published planning {DateDisplay.FormatDateTime(userLocalNow)}"
                : $"Draft planning {DateDisplay.FormatDateTime(userLocalNow)}";

            await _planningService.SavePlanningForRoundAsync(RoundId, _round.Year, planningName, isPublished, assignments);

            var message = isPublished
                ? "Planning is gepubliceerd. Andere gebruikers zien nu deze versie."
                : "Conceptplanning is opgeslagen.";
            var severity = isPublished ? Severity.Success : Severity.Info;

            SetSaveMessage(message, severity);
            _snackbar.Add(message, severity);
        }
        catch (Exception)
        {
            var message = isPublished ? "Publiceren is mislukt." : "Opslaan van het concept is mislukt.";
            SetSaveMessage(message, Severity.Error);
            _snackbar.Add(message, Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void SetSaveMessage(string message, Severity severity)
    {
        _saveMessage = message;
        _saveMessageSeverity = severity;
        StartSaveMessageAutoHide();
    }

    private void ClearSaveMessage()
    {
        _saveMessageCts?.Cancel();
        _saveMessageCts?.Dispose();
        _saveMessageCts = null;
        _saveMessage = null;
    }

    private void StartSaveMessageAutoHide()
    {
        _saveMessageCts?.Cancel();
        _saveMessageCts?.Dispose();
        _saveMessageCts = new CancellationTokenSource();
        var token = _saveMessageCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SaveMessageDisplayMs, token);
                if (token.IsCancellationRequested) return;

                await InvokeAsync(() =>
                {
                    _saveMessage = null;
                    StateHasChanged();
                });
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    private Dictionary<int, IReadOnlyCollection<int>> MapToAssignmentDictionary() =>
        _result!.JourneyAssignments.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<int>)pair.Value.Select(a => a.LeaderId).ToList());
}
