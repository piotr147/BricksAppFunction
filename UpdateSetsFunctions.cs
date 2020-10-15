using System.Threading.Tasks;
using BricksAppFunction.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace BricksAppFunction
{
    public static class UpdateSetsFunctions
    {
        private const int NUMBER_OF_FUNCTIONS = 10;

        [FunctionName("UpdateSetsStep1")]
        public async static Task UpdateSetsStep1([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(1, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep2")]
        public async static Task UpdateSetsStep2([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(2, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep3")]
        public async static Task UpdateSetsStep3([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(3, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep4")]
        public async static Task UpdateSetsStep4([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(4, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep5")]
        public async static Task UpdateSetsStep5([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(5, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep6")]
        public async static Task UpdateSetsStep6([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(6, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep7")]
        public async static Task UpdateSetsStep7([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(7, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep8")]
        public async static Task UpdateSetsStep8([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(8, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep9")]
        public async static Task UpdateSetsStep9([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(9, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep10")]
        public async static Task UpdateSetsStep10([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(10, NUMBER_OF_FUNCTIONS);
        }
    }
}
