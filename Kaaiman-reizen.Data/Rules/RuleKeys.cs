namespace Kaaiman_reizen.Data.Rules;

public static class RuleKeys
{
    public const string NoOverlap = "NoOverlap";
    public const string MinimumGapDays = "MinimumGapDays";
    public const string RequiredExperience = "RequiredExperience";
    public const string MinMaxJourneys = "MinMaxJourneys";
    public const string PreferencesEnabled = "PreferencesEnabled";
    public const string JourneyReminderEnabled = "JourneyReminderEnabled";
    public const string JourneyReminderDays = "JourneyReminderDays";
    public const string WelcomeEmailEnabled = "WelcomeEmailEnabled";
    public const string PlanningPublishedEnabled = "PlanningPublishedEnabled";
    public const string PlanningChangedEnabled = "PlanningChangedEnabled";
    public const string PreferenceWeight = "PreferenceWeight";
    public const string NoOverlapWeight = "NoOverlapWeight";
    public const string MinimumGapWeight = "MinimumGapWeight";
    public const string RequiredExperienceWeight = "RequiredExperienceWeight";
    public const int DefaultMinimumGapDays = 3;
    public const int DefaultRequiredExperience = 3;
    public const int DefaultPreferenceWeight = 5;
    public const int DefaultNoOverlapWeight = 200;
    public const int DefaultMinimumGapWeight = 100;
    public const int DefaultRequiredExperienceWeight = 100;
    public const bool DefaultJourneyReminderEnabled = true;
    public const bool DefaultWelcomeEmailEnabled = true;
    public const bool DefaultPlanningPublishedEnabled = true;
    public const bool DefaultPlanningChangedEnabled = true;
    public static readonly int[] DefaultJourneyReminderDays = [7, 3];
}
