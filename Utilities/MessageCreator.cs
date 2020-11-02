using BricksAppFunction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BricksAppFunction.Utilities
{
    public static class MessageCreator
    {
        public static Dictionary<(int number, bool isBigUpdate), MailMessage> GetMessagesForUpdatedSets(List<LegoSet> updatedSets)
        {
            var messages = new Dictionary<(int number, bool isBigUpdate), MailMessage>();

            foreach (LegoSet set in updatedSets)
            {
                if (set.LastLowestPrice != null)
                {
                    messages.Add(
                        (set.Number, false),
                        new MailMessage
                        {
                            Plain = GetUpdatePlainMessage(set, set.LastLowestPrice.Value, set.LowestPrice),
                            Html = GetUpdateHtmlMessage(set, set.LastReportedLowestPrice, set.LowestPrice),
                            DiffPercent = CalculateDiffPercent(set),
                            IsBigUpdate = false,
                            IsLowestPriceEver = IsLowestPrice(set)
                        });
                    if (CheckForBigUpdates(set) || IsLowestPrice(set))
                    {
                        messages.Add(
                        (set.Number, true),
                        new MailMessage
                        {
                            Plain = GetUpdatePlainMessage(set, set.LastReportedLowestPrice, set.LowestPrice),
                            Html = GetUpdateHtmlMessage(set, set.LastReportedLowestPrice, set.LowestPrice),
                            DiffPercent = CalculateDiffPercent(set),
                            IsBigUpdate = false,
                            IsLowestPriceEver = IsLowestPrice(set)
                        });

                        UpdateLastReportedLowestPrice(set);
                    }
                }
            }

            return messages;
        }

        private static void UpdateLastReportedLowestPrice(LegoSet set)
        {
            set.LastReportedLowestPrice = set.LowestPrice;
        }

        private static string GetUpdateHtmlMessage(LegoSet set, decimal priceFrom, decimal priceTo) =>
            $@"
                Lego {set.Series} - {set.Number} - <strong>{set.Name}</strong>
                {AddShopLine(set)}
                {PriceInfoLine(priceFrom, priceTo)}
                {EventualLowestPriceEverHtml(set)}
                <a href=""{set.Link}"">{set.Link}</a>";

        private static object AddShopLine(LegoSet set) =>
            $@"
            <p>
                <strong>{set.LowestShop}</strong>
            </p>";

        private static string PriceInfoLine(decimal priceFrom, decimal priceTo)
        {
            string verbToUse = priceTo < priceFrom ? "decreased" : "increased";
            string color = priceTo < priceFrom
                ? ColorForPriceDecrease(priceFrom, priceTo)
                : ColorForPriceIncrease(priceFrom, priceTo);

            return @$"
                <p style=""color:{color};"">
                    <strong>Price {verbToUse} from {priceFrom:0.00} to {priceTo:0.00}</strong>
                </p>";
        }

        private static string ColorForPriceDecrease(decimal priceFrom, decimal priceTo) =>
            IsBigUpdate(priceFrom, priceTo)
                ? "GreenYellow"
                : "MediumSeaGreen";

        private static string ColorForPriceIncrease(decimal priceFrom, decimal priceTo) =>
            IsBigUpdate(priceFrom, priceTo)
                ? "Red"
                : "Crimson";

        private static string EventualLowestPriceEverHtml(LegoSet set) =>
            set.LowestPrice > set.LowestPriceEver
                ? ""
                : @"
                    <p style=""color:yellow;"">
                        <strong>LOWEST PRICE EVER</strong>
                    </p>";

        private static string GetUpdatePlainMessage(LegoSet set, decimal priceFrom, decimal priceTo)
        {
            string verbToUse = priceTo > priceFrom ? "decreased" : "increased";
            return $@"
                Lego {set.Series} - {set.Number} - {set.Name}\n
                {set.LowestShop}\n
                Price {verbToUse} from {priceFrom:0.00} to {priceTo:0.00}\n
                {EventualLowestPriceEverPlain(set)}
                {set.Link}";
        }

        private static string EventualLowestPriceEverPlain(LegoSet set) =>
            set.LowestPrice > set.LowestPriceEver
                ? ""
                : @"LOWEST PRICE EVER\n";

        private static bool CheckForBigUpdates(LegoSet set) =>
            IsBigUpdate(set);

        private static bool IsBigUpdate(LegoSet set) =>
            Math.Abs(CalculateDiffPercent(set)) > 0.01f;

        private static bool IsBigUpdate(decimal priceFrom, decimal priceTo) =>
            Math.Abs(CalculateDiffPercent(priceFrom, priceTo)) > 0.01f;

        private static bool IsLowestPrice(LegoSet set) =>
            set.LowestPrice <= set.LowestPriceEver;

        private static float CalculateDiffPercent(LegoSet set) =>
            (float)((set.LowestPrice - set.LastReportedLowestPrice) / set.LowestPrice);

        private static float CalculateDiffPercent(decimal priceFrom, decimal priceTo) =>
            (float)((priceTo - priceFrom) / priceTo);
    }
}
