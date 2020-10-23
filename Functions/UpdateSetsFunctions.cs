using System.Threading.Tasks;
using BricksAppFunction.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace BricksAppFunction
{
    public static class UpdateSetsFunctions
    {
        private const int NUMBER_OF_FUNCTIONS = 10;

        [FunctionName("UpdateSetsStep0")]
        public async static Task UpdateSetsStep0([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(0, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep01")]
        public async static Task UpdateSetsStep01([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(1, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep02")]
        public async static Task UpdateSetsStep02([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(2, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep03")]
        public async static Task UpdateSetsStep03([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(3, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep04")]
        public async static Task UpdateSetsStep04([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(4, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep05")]
        public async static Task UpdateSetsStep05([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(5, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep06")]
        public async static Task UpdateSetsStep06([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(6, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep07")]
        public async static Task UpdateSetsStep07([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(7, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep08")]
        public async static Task UpdateSetsStep08([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(8, NUMBER_OF_FUNCTIONS);
        }

        [FunctionName("UpdateSetsStep09")]
        public async static Task UpdateSetsStep09([TimerTrigger("0 50 3-22 * * *")] TimerInfo myTimer, ILogger log)
        {
            await SetUpdater.UpdateSetsWithNumberBeingReminderOf(9, NUMBER_OF_FUNCTIONS);
        }
    }
}
