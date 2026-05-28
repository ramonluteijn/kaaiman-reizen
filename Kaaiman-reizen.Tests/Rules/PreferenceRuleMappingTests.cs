using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Rules;

namespace Kaaiman_reizen.Tests.Rules
{
    public class PreferenceRuleMappingTests
    {
        [Fact]
        public void FromRules_Uses_PreferencesEnabled_For_PreferencesEnabled()
        {
            var rules = new List<Rule>
            {
                new Rule { Id = 100, Key = "PreferencesEnabled", IsActive = false }
            };

            var settings = CheckRules.FromRules(rules);

            Assert.False(settings.PreferencesEnabled);
        }

        [Fact]
        public void FromRules_Uses_PreferencesEnabled_Weight_As_Fallback_For_PreferenceWeight()
        {
            var rules = new List<Rule>
            {
                new Rule { Id = 101, Key = "PreferencesEnabled", IsActive = true, Weight = 7 }
            };

            var settings = CheckRules.FromRules(rules);

            Assert.Equal(7, settings.PreferenceWeight);
        }
    }
}


