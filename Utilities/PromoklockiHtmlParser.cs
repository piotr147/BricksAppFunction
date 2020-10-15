using BricksAppFunction.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BricksAppFunction.Utilities
{
    public static class PromoklockiHtmlParser
    {
        private static readonly Regex TitleElementRegex = new Regex(@"LEGO<sup>&reg;</sup> \d{5}.*</h1>");
        private static readonly Regex TitleRegex = new Regex(@"\d{5}.*<");
        private static readonly Regex SeriesWithBorderRegex = new Regex(@"\d{5}.*-");
        private static readonly Regex CatalogNumberRegex = new Regex(@"\d{5}");
        private static readonly Regex PriceRegex = new Regex(@"\d*,\d*");
        private static readonly Regex ShopRegex = new Regex(@"(?:(col-5 col-lg-2 order-1 align-self-center text-left"">)([\n\r])*).*(?:([\n\r]))");
        private static readonly Regex ShopAndPriceRegex = new Regex(@"col-5 col-lg-2 order-1 align-self-center text-left(.|[\n\r])*?(?:</span>z)");
        private static readonly Regex LowestPriceRegex = new Regex(@"""lowPrice"": ""\d*\.\d*""");
        private static readonly Regex LowestPriceEverRegex = new Regex(@"Najniższa cena</dt><dd class=""col-12 col-sm-8 col-md-6 col-lg-8"">\d*,\d*");
        private const string NajnizszaCenaElement = @"Najniższa cena</dt><dd class=""col-12 col-sm-8 col-md-6 col-lg-8"">";

        public async static Task<LegoSet> GetSetInfo(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

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
                List<(decimal price, string shop)> pricesAndShops = GetPricesAndShops(data);
                (decimal lowestPrice, string lowestShop) = pricesAndShops.OrderBy(p => p.price).First();
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
                    LowestShop = lowestShop,
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

        private static List<(decimal price, string shop)> GetPricesAndShops(string doc)
        {
            decimal lowestPrice = GetLowestPrice(doc);
            var pricesAndShops = new List<(decimal price, string shop)>();
            var xd = ShopAndPriceRegex.Matches(doc);

            foreach (Match match in xd)
            {
                try
                {
                    string price = ExtractPrice(match.Value);
                    string shop = ExtractShop(match.Value);

                    pricesAndShops.Add((decimal.Parse(price), shop));

                    if (decimal.Parse(price) <= lowestPrice)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                }
            }

            return pricesAndShops;
        }

        private static decimal GetLowestPrice(string doc)
        {
            Regex priceRegex = new Regex(@"\d*\.\d*");
            string lowestPriceWithBorder = LowestPriceRegex.Match(doc).Value;

            return decimal.Parse(priceRegex.Match(lowestPriceWithBorder).Value);
        }


        private static string ExtractPrice(string partOfDoc) =>
            PriceRegex.Match(partOfDoc).Value;

        private static string ExtractShop(string partOfDoc)
        {
            string shopWithBorder = ShopRegex.Match(partOfDoc).Value;
            return DeleteTrashFromShop(shopWithBorder);
        }

        private static string DeleteTrashFromShop(string shopWithBorder)
        {
            string shopWithoutTrash = shopWithBorder.Remove(0, @"col-5 col-lg-2 order-1 align-self-center text-left"" > ".Length);
            return shopWithoutTrash.Replace(" ", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        private static decimal GetLowestPriceEver(string doc)
        {
            string priceWithTrash = LowestPriceEverRegex.Match(doc).Value;
            string price = priceWithTrash.Remove(0, NajnizszaCenaElement.Length).Replace(',', '.');
            return decimal.Parse(price);
        }
    }
}
