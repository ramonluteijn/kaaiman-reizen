using MudBlazor;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerDraft
{
    private async Task SaveDraftAsync()
    {
        await SavePlanningAsync(isPublished: false);
    }

    private async Task PublishPlanningAsync()
    {
        await SavePlanningAsync(isPublished: true);
    }

    private async Task SavePlanningAsync(bool isPublished)
    {
        if (_request is null || _result is null || !_result.IsSuccess)
        {
            return;
        }

        _isSaving = true;
        ClearSaveMessage();

        try
        {
            var assignments = MapToAssignmentDictionary();

            string planningName = isPublished
                ? $"Published planning {DateTime.Now:yyyy-MM-dd HH:mm}"
                : $"Draft planning {DateTime.Now:yyyy-MM-dd HH:mm}";

            await PlanningService.SavePlanningAsync(_selectedYear, planningName, isPublished, assignments);

            var message = isPublished
                ? "Planning is gepubliceerd. Andere gebruikers zien nu deze versie."
                : "Conceptplanning is opgeslagen.";
            var severity = isPublished ? Severity.Success : Severity.Info;

            SetSaveMessage(message, severity);
            Snackbar.Add(message, severity);
        }
        catch (Exception)
        {
            var message = isPublished
                ? "Publiceren is mislukt."
                : "Opslaan van het concept is mislukt.";

            SetSaveMessage(message, Severity.Error);
            Snackbar.Add(message, Severity.Error);
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
