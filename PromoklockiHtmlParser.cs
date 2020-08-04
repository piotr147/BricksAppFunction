using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BricksAppFunction
{
    public static class PromoklockiHtmlParser
    {
        private static readonly Regex TitleElementRegex = new Regex(@"<h1 class=""htitle"">.*</h1>");
        private static readonly Regex TitleRegex = new Regex(@"\d{5}.*<");
        private static readonly Regex SeriesWithBorderRegex = new Regex(@"\d{5}.*-");
        private static readonly Regex CatalogNumberRegex = new Regex(@"\d{5}");
        private static readonly Regex PriceRegex = new Regex(@"<strong>\d*,\d*</strong>");
        private static readonly Regex LowestPriceEverRegex = new Regex(@"<dt>Najniższa cena</dt><dd>\d*,\d*");
        private const string NajnizszaCenaElement = "<dt>Najniższa cena</dt><dd>";

        public async static Task<LegoSet> GetSetInfo(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream;

                if (string.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();
                string title = GetTitle(data);
                int catalogNumber = GetCatalogNumber(title);
                string name = GetName(title);
                string series = GetSeries(title);
                List<decimal> prices = GetPrices(data);
                decimal lowestPrice = prices.Min();
                decimal lowestPriceEver = GetLowestPriceEver(data);

                response.Close();
                readStream.Close();

                return new LegoSet
                {
                    Number = catalogNumber,
                    Name = name,
                    Series = series,
                    Link = url,
                    LowestPrice = lowestPrice,
                    LowestPriceEver = lowestPriceEver
                };
            }

            throw new Exception("Info about set not found");
        }

        private static string GetTitle(string doc)
        {
            string titleElement = TitleElementRegex.Match(doc).Value;
            string titleWithTrash = TitleRegex.Match(titleElement).Value;
            return titleWithTrash.Remove(titleWithTrash.Length - 1);
        }

        private static int GetCatalogNumber(string title) => int.Parse(CatalogNumberRegex.Match(title).Value);

        private static string GetName(string title)
        {
            int firstDashIndex = title.IndexOf('-');
            return title.Substring(firstDashIndex + 2);
        }

        private static string GetSeries(string title)
        {
            int firstDashIndex = title.IndexOf('-');
            string seriesWtihTrash = SeriesWithBorderRegex.Match(title.Remove(firstDashIndex + 1)).Value;
            return seriesWtihTrash.Remove(seriesWtihTrash.Length - 2, 2).Remove(0, 6);
        }

        private static List<decimal> GetPrices(string doc)
        {
            var prices = new List<decimal>();
            foreach (Match match in PriceRegex.Matches(doc))
            {
                string price = match.Value.Remove(match.Value.Length - 9, 9).Remove(0, 8).Replace(',', '.');
                prices.Add(decimal.Parse(price));
            }

            return prices;
        }

        private static decimal GetLowestPriceEver(string doc)
        {
            string priceWithTrash = LowestPriceEverRegex.Match(doc).Value;
            string price = priceWithTrash.Remove(0, NajnizszaCenaElement.Length).Replace(',', '.');
            return decimal.Parse(price);
        }
    }
}
