using BricksAppFunction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BricksAppFunction.Utilities
{
    public static class MessageCreator
    {
        public static Dictionary<int, MailMessage> GetMessagesForUpdatedSets(List<LegoSet> updatedSets)
        {
            var messages = new Dictionary<int, MailMessage>();

            foreach (LegoSet set in updatedSets)
            {
                if (set.LastLowestPrice != null)
                {
                    messages.Add(
                        set.Number,
                        new MailMessage
                        {
                            Plain = GetUpdatePlainMessage(set),
                            Html = GetUpdateHtmlMessage(set),
                            DiffPercent = CalculateDiffPercent(set),
                            IsBigUpdate = CheckForBigUpdates(set),
                            IsLowestPriceEver = IsLowestPrice(set)
                        });
                }
            }

            return messages;
        }

        private static string GetUpdateHtmlMessage(LegoSet set) =>
            $@"
                Lego {set.Series} - {set.Number} - <strong>{set.Name}</strong>
                {AddShopLine(set)}
                {PriceInfoLine(set)}
                {EventualLowestPriceEverHtml(set)}
                <a href=""{set.Link}"">{set.Link}</a>";

        private static object AddShopLine(LegoSet set) =>
            $@"
            <p>
                <strong>{set.LowestShop}</strong>
            </p>";

        private static string PriceInfoLine(LegoSet set)
        {
            string verbToUse = set.LowestPrice < set.LastLowestPrice ? "decreased" : "increased";
            string color = set.LowestPrice < set.LastLowestPrice
                ? ColorForPriceDecrease(set)
                : ColorForPriceIncrease(set);

            return @$"
                <p style=""color:{color};"">
                    <strong>Price {verbToUse} from {set.LastLowestPrice:0.00} to {set.LowestPrice:0.00}</strong>
                </p>";
        }

        private static string ColorForPriceDecrease(LegoSet set) =>
            IsBigUpdate(set)
                ? "GreenYellow"
                : "MediumSeaGreen";

        private static string ColorForPriceIncrease(LegoSet set) =>
            IsBigUpdate(set)
                ? "Red"
                : "Crimson";

        private static string EventualLowestPriceEverHtml(LegoSet set) =>
            set.LowestPrice > set.LowestPriceEver
                ? ""
                : @"
                    <p style=""color:yellow;"">
                        <strong>LOWEST PRICE EVER</strong>
                    </p>";

        private static string GetUpdatePlainMessage(LegoSet set)
        {
            string verbToUse = set.LowestPrice > set.LastLowestPrice ? "decreased" : "increased";
            return $@"
                Lego {set.Series} - {set.Number} - {set.Name}\n
                {set.LowestShop}\n
                Price {verbToUse} from {set.LastLowestPrice:0.00} to {set.LowestPrice:0.00}\n
                {EventualLowestPriceEverPlain(set)}
                {set.Link}";
        }

        private static string EventualLowestPriceEverPlain(LegoSet set) =>
            set.LowestPrice > set.LowestPriceEver
                ? ""
                : @"LOWEST PRICE EVER\n";

        private static bool CheckForBigUpdates(LegoSet set)
        {
            if (IsBigUpdate(set))
            {
                set.LastReportedLowestPrice = set.LowestPrice;
                return true;
            }

            return false;
        }

        private static bool IsBigUpdate(LegoSet set) =>
            Math.Abs(CalculateDiffPercent(set)) > 0.01f;

        private static bool IsLowestPrice(LegoSet set) =>
            set.LowestPrice <= set.LowestPriceEver;

        private static float CalculateDiffPercent(LegoSet set) =>
            (float)((set.LowestPrice - set.LastReportedLowestPrice) / set.LowestPrice);
    }
}
