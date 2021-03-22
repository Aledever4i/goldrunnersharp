using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Linq;
using goldrunnersharp.Model;
using goldrunnersharp.Api;
using goldrunnersharp.Client;
using System.Collections.Generic;
using DataStructures;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net;

namespace goldrunnersharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = Environment.GetEnvironmentVariable("ADDRESS");

            UriBuilder myURI = new UriBuilder("http", address, 8000);
            var api = new DefaultApi(myURI.Uri.AbsoluteUri);
            var client = new Game(api, myURI.Uri);

            client.Await().Wait();
        }
    }


    public class ExtendedReport : Report
    {
        public bool Priority { get; set; }

        public ExtendedReport(Report report)
        {
            this.Amount = report.Amount;
            this.Area = report.Area;
        }
    }

    public class Gold
    {
        public string Money { get; set; }

        public int Depth { get; set; }
    }

    public class Game
    {
        public BlockingCollection<Report> exploreQueue = new BlockingCollection<Report>();
        public BlockingCollection<Gold> treasureQueue = new BlockingCollection<Gold>();
        public BlockingCollection<License> licenses = new BlockingCollection<License>();
        public BlockingCollection<Wallet> wallets = new BlockingCollection<Wallet>();

        private HttpClient httpClient { get; set; }

        private Thread treadDig1 { get; set; }

        private Thread treasureThread { get; set; }

        private Thread treadExplore1 { get; set; }

        private Thread treadExplore2 { get; set; }

        private Thread treadExplore3 { get; set; }

        private DefaultApi API { get; set; }

        private SemaphoreSlim _digSignal;

        public Game(DefaultApi base_url, Uri base_url2)
        {
            _digSignal = new SemaphoreSlim(10);

            this.API = base_url;
            this.httpClient = new HttpClient()
            {
                BaseAddress = base_url2,
                Timeout = TimeSpan.FromMilliseconds(1500),
                DefaultRequestVersion = new Version(2, 0)
            };

            treadDig1 = new Thread(processDig);
            treadDig1.Start();

            treasureThread = new Thread(treasureStart);
            treasureThread.Start();

            treadExplore1 = new Thread(Start1);
            treadExplore1.Start();

            treadExplore2 = new Thread(Start2);
            treadExplore2.Start();

            treadExplore3 = new Thread(Start3);
            treadExplore3.Start();
        }

        public void treasureStart()
        {
            while (true)
            {
                if (treasureQueue.TryTake(out Gold item))
                {
                    _ = CashTreasure(item);
                }
            }
        }

        public void processDig()
        {
            while (true)
            {
                if (exploreQueue.TryTake(out Report item))
                {
                    _ = GoDig(item);
                }
            }
        }

        public async Task<License> UpdateLicense()
        {
            if (licenses.TryTake(out License license))
            {
                return license;
            }

            var wallet = new Wallet();

            if (wallets.TryTake(out Wallet ws))
            {
                wallet = ws;
            }

            while (true)
            {
                try
                {
                    return (await this.API.IssueLicenseAsyncWithHttpInfo(wallet)).Data;
                }
                catch (ApiException ex)
                {
                    if (
                        (ex.ErrorCode > 500 && ex.ErrorCode < 600)
                        || (ex.ErrorContent == "An error occurred while sending the request. The response ended prematurely.")
                        || (ex.ErrorContent == "Connection refused Connection refused")
                        || (ex.ErrorContent == "The operation has timed out.")
                    )
                    {
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }

        //public async Task<License> UpdateLicense()
        //{
        //    if (licenses.TryTake(out License license))
        //    {
        //        return license;
        //    }

        //    var wallet = new Wallet();

        //    if (wallets.TryTake(out Wallet ws))
        //    {
        //        wallet = ws;
        //    }

        //    while (true)
        //    {
        //        try
        //        {
        //            var request = await this.httpClient.PostAsync($"/licenses", new StringContent(JsonConvert.SerializeObject(wallet), Encoding.UTF8, "application/json"));

        //            var jsonString = await request.Content.ReadAsStringAsync();

        //            if (request.StatusCode == HttpStatusCode.OK)
        //            {
        //                return JsonConvert.DeserializeObject<License>(jsonString);
        //            }
        //            else if
        //            (
        //                (int)request.StatusCode > 500 && (int)request.StatusCode < 600
        //                || jsonString == "The request timed-out."
        //                || jsonString == "Connection refused Connection refused"
        //                || jsonString == "An error occurred while sending the request. The response ended prematurely."
        //            )
        //            {
        //            }
        //            else
        //            {
        //                throw new Exception();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //    }
        //}

        public async Task<Report> Explore(Area area)
        {
            while (true)
            {
                try
                {
                    var request = await this.httpClient.PostAsync($"/explore", new StringContent(JsonConvert.SerializeObject(area), Encoding.UTF8, "application/json"));

                    if (request.StatusCode == HttpStatusCode.OK)
                    {
                        string jsonString = await request.Content.ReadAsStringAsync();
                        var report = JsonConvert.DeserializeObject<Report>(jsonString);

                        return report;
                    }
                    else if ((int)request.StatusCode == 408 || ((int)request.StatusCode > 500 && (int)request.StatusCode < 600))
                    {
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Message == "Connection refused")
                    { }
                    else
                    {
                        throw ex;
                    }
                }
                catch (TaskCanceledException ex)
                {

                }
                catch (System.IO.IOException ex)
                {
                    if (ex.Message == "The response ended prematurely.")
                    { }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message == "The request timed-out." || ex.Message == "Connection refused" || ex.Message == "An error occurred while sending the request.")
                    { }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            throw new Exception();
        }

        private async Task CashTreasure(Gold treasure)
        {
            Wallet report = null;

            while (report == null)
            {
                try
                {
                    var request = await this.httpClient.PostAsync($"/cash", new StringContent(JsonConvert.SerializeObject(treasure.Money), Encoding.UTF8, "application/json"));

                    if (request.StatusCode == HttpStatusCode.OK)
                    {
                        var jsonString = await request.Content.ReadAsStringAsync();
                        report = JsonConvert.DeserializeObject<Wallet>(jsonString);

                        if (report.Any() && treasure.Depth == 5)
                        {
                            wallets.Add(report);
                        }
                    }
                    else if ((int)request.StatusCode > 500 && (int)request.StatusCode < 600)
                    {
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Message == "Connection refused")
                    { }
                    else
                    {
                        throw ex;
                    }
                }
                catch (TaskCanceledException ex)
                {

                }
                catch (System.IO.IOException ex)
                {
                    if (ex.Message == "The response ended prematurely.")
                    { }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    if (ex.Message == "The request timed-out." || ex.Message == "Connection refused" || ex.Message == "An error occurred while sending the request.")
                    { }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        private async Task<TreasureList> Dig(Dig dig)
        {
            var digParams = dig;

            while (true)
            {
                try
                {
                    var request = await this.httpClient.PostAsync($"/dig", new StringContent(JsonConvert.SerializeObject(digParams), Encoding.UTF8, "application/json"));

                    if (request.StatusCode == HttpStatusCode.OK)
                    {
                        var jsonString = await request.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<TreasureList>(jsonString);
                    }
                    else if ((int)request.StatusCode > 500 && (int)request.StatusCode < 600)
                    {
                    }
                    else if ((int)request.StatusCode == 403)
                    {
                        var license = await UpdateLicense();
                        digParams.LicenseID = license.Id;
                    }
                    else if ((int)request.StatusCode == 404)
                    {
                        break;
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Message == "Connection refused")
                    { }
                    else
                    {
                        throw ex;
                    }
                }
                catch (TaskCanceledException ex)
                {

                }
                catch (System.IO.IOException ex)
                {
                    if (ex.Message == "The response ended prematurely.")
                    { }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    if (ex.Message == "The request timed-out." || ex.Message == "Connection refused" || ex.Message == "An error occurred while sending the request.")
                    { }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            return null;
        }

        public async Task GoDig(Report report)
        {
            var left = report.Amount;

            var initX = report.Area.PosX.Value;
            var sizeX = report.Area.SizeX;

            var initY = report.Area.PosY.Value;
            var sizeY = report.Area.SizeY;

            for (int x = initX; x < (initX + sizeX) && left > 0; x++)
            {
                for (int y = initY; y < (initY + sizeY) && left > 0; y++)
                {
                    var explore = await this.Explore(new Area(x, y, 1, 1));
                    var depth = 1;

                    if (explore.Amount > 0)
                    {
                        var license = new License(0, 0, 0);
                        try
                        {
                            await _digSignal.WaitAsync();
                            license = await UpdateLicense();

                            while (depth <= 10 && left > 0 && explore.Amount > 0)
                            {
                                if (license.DigUsed >= license.DigAllowed)
                                {
                                    license = await UpdateLicense();
                                }

                                var result = await Dig(new Dig(license.Id.Value, x, y, depth));
                                license.DigUsed += 1;

                                if (result != null)
                                {
                                    explore.Amount -= result.Count;
                                    left -= result.Count;
                                    foreach (var treasure in result)
                                    {
                                        treasureQueue.Add(new Gold() { Money = treasure, Depth = depth });
                                    }
                                }
                                depth += 1;
                            }
                        }
                        finally
                        {
                            if (license.DigUsed < license.DigAllowed)
                            {
                                licenses.Add(license);
                            }
                            _digSignal.Release();
                        }
                    }
                }
            }
        }

        public async Task Xy2(Area area, int line)
        {
            var newLine = line / 5;

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var posX = area.PosX.Value + (x * newLine);
                    var posY = area.PosY.Value + (y * newLine);

                    await this.Explore(new Area(posX, posY, newLine, newLine)).ContinueWith(async (result) => {
                        if (result.Result.Amount > result.Result.Area.SizeX * result.Result.Area.SizeY * 0.06) // 0.04 вернуть
                        {
                            if (newLine == 2 && result.Result.Amount > 0)
                            {
                                exploreQueue.Add(result.Result);
                            }
                            else if (newLine != 2)
                            {
                                await Xy2(result.Result.Area, newLine);
                            }
                        }
                    }, TaskContinuationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        public void Start1()
        {
            foreach (var x in Enumerable.Range(0, 25))
            {
                foreach (var y in Enumerable.Range(0, 70))
                {
                    Xy2(new Area(x * 50, y * 50, 50, 50), 50).Wait();
                }
            }
        }
        public void Start2()
        {
            foreach (var x in Enumerable.Range(26, 50))
            {
                foreach (var y in Enumerable.Range(0, 70))
                {
                    Xy2(new Area(x * 50, y * 50, 50, 50), 50).Wait();
                }
            }
        }
        public void Start3()
        {
            foreach (var x in Enumerable.Range(51, 70))
            {
                foreach (var y in Enumerable.Range(0, 70))
                {
                    Xy2(new Area(x * 50, y * 50, 50, 50), 50).Wait();
                }
            }
        }

        public async Task Await()
        {
            await Task.Delay(600000);
        }
    }
}