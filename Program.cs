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

            //ThreadPool.SetMinThreads(100, 100);

            var client = new Game(myURI.Uri);

            client.Await();
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

    public class GoldComparer : IComparer<Gold>
    {
        public int Compare(Gold x, Gold y)
        {
            return x.Depth.CompareTo(y.Depth);
        }
    }

    public class Game
    {
        public ConcurrentPriorityQueue<Report> exploreQueue = new ConcurrentPriorityQueue<Report>(new ReportComparer());
        public ConcurrentPriorityQueue<Gold> treasureQueue = new ConcurrentPriorityQueue<Gold>(new GoldComparer());
        public BlockingCollection<License> licenses = new BlockingCollection<License>();
        public BlockingCollection<Wallet> wallets = new BlockingCollection<Wallet>();
        public ConcurrentPriorityQueue<Report> searchQueue = new ConcurrentPriorityQueue<Report>(new ReportComparer());

        private HttpClient httpClient { get; set; }

        private Thread treadDig1 { get; set; }

        private Thread treasureThread { get; set; }

        private Thread searchThread { get; set; }

        private SemaphoreSlim _digSignal;

        public Game(Uri base_url2)
        {
            _digSignal = new SemaphoreSlim(12);
            this.httpClient = new HttpClient()
            {
                BaseAddress = base_url2,
                Timeout = TimeSpan.FromMilliseconds(2000),
                DefaultRequestVersion = new Version(2, 0)
            };

            treadDig1 = new Thread(ProcessDig) { IsBackground = true, Priority = ThreadPriority.Highest };
            treadDig1.Start();

            treasureThread = new Thread(TreasureStart);
            treasureThread.Start();

            searchThread = new Thread(SearchStart);
            searchThread.Start();
        }

        public void SearchStart()
        {
            while (true)
            {
                if (searchQueue.TryTake(out Report item))
                {
                    _ = Xy2(item.Area, 15);
                }
            }
        }

        public void TreasureStart()
        {
            while (true)
            {
                if (treasureQueue.TryTake(out Gold item))
                {
                    _ = CashTreasure(item);
                }
            }
        }

        public void ProcessDig()
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
                License:
                try
                {
                        var request = await this.httpClient.PostAsync($"/licenses", new StringContent(JsonConvert.SerializeObject(wallet), Encoding.UTF8, "application/json"));

                        var jsonString = await request.Content.ReadAsStringAsync();

                        if (request.StatusCode == HttpStatusCode.OK)
                        {
                            return JsonConvert.DeserializeObject<License>(jsonString);
                        }
                        else if
                        (
                            (int)request.StatusCode > 500 && (int)request.StatusCode < 600
                            || jsonString == "The request timed-out."
                            || jsonString == "Connection refused Connection refused"
                            || jsonString == "An error occurred while sending the request. The response ended prematurely."
                        )
                        {
                            goto License;
                        }
                        else
                        {
                            goto License;
                        //throw new Exception();
                        }
                }
                catch (Exception)
                {
                    goto License;
                    //throw new Exception(ex.Message);
                }
            }
        }

        public async Task<Report> Explore(Area area)
        {
            //while (true)
            //{
                Explore:
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
                            goto Explore;
                        }
                }
                catch (WebException)
                {
                    goto Explore;
                    //if (ex.Message == "Connection refused")
                    //{ }
                    //else
                    //{
                    //    throw ex;
                    //}
                }
                catch (TaskCanceledException)
                {
                    goto Explore;
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
                catch (HttpRequestException)
                {
                    goto Explore;
                    //if (ex.Message == "The request timed-out." || ex.Message == "Connection refused" || ex.Message == "An error occurred while sending the request.")
                    //{ }
                    //else
                    //{
                    //    throw new Exception(ex.Message);
                    //}
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            //}

            throw new Exception();
        }

        private async Task CashTreasure(Gold treasure)
        {
            Wallet report = null;

            //while (report == null)
            //{
                Restart:
                try
                {
                    var request = await this.httpClient.PostAsync($"/cash", new StringContent(JsonConvert.SerializeObject(treasure.Money), Encoding.UTF8, "application/json"));

                    if (request.StatusCode == HttpStatusCode.OK)
                    {
                        var jsonString = await request.Content.ReadAsStringAsync();
                        report = JsonConvert.DeserializeObject<Wallet>(jsonString);

                        if (report.Any() && treasure.Depth == 8)
                        {
                            wallets.Add(report);
                        }
                    }
                    else if ((int)request.StatusCode > 500 && (int)request.StatusCode < 600)
                    {
                        goto Restart;
                    }
                }
                catch (WebException)
                {
                    goto Restart;
                    //if (ex.Message == "Connection refused")
                    //{ }
                    //else
                    //{
                    //    throw ex;
                    //}
                }
                catch (TaskCanceledException)
                {
                    goto Restart;
                }
                catch (System.IO.IOException)
                {
                    goto Restart;
                    //if (ex.Message == "The response ended prematurely.")
                    //{ }
                    //else
                    //{
                    //    throw new Exception(ex.Message);
                    //}
                }
                catch (HttpRequestException)
                {
                    goto Restart;
                    //if (ex.Message == "The request timed-out." || ex.Message == "Connection refused" || ex.Message == "An error occurred while sending the request.")
                    //{ }
                    //else
                    //{
                    //    throw new Exception(ex.Message);
                    //}
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            //}
        }

        private async Task<TreasureList> Dig(Dig dig)
        {
            var digParams = dig;

            //while (true)
            //{
                Restart:
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
                        goto Restart;
                    }
                    else if ((int)request.StatusCode == 403)
                    {
                        var license = await UpdateLicense();
                        digParams.LicenseID = license.Id;
                        goto Restart;
                    }
                    else if ((int)request.StatusCode == 404)
                    {
                    }
                }
                catch (WebException)
                {
                    goto Restart;
                    //if (ex.Message == "Connection refused")
                    //{ }
                    //else
                    //{
                    //    throw ex;
                    //}
                }
                catch (TaskCanceledException)
                {
                    goto Restart;
                }
                catch (System.IO.IOException)
                {
                    goto Restart;
                    //if (ex.Message == "The response ended prematurely.")
                    //{ }
                    //else
                    //{
                    //    throw new Exception(ex.Message);
                    //}
                }
                catch (HttpRequestException)
                {
                    goto Restart;
                    //if (ex.Message == "The request timed-out." || ex.Message == "Connection refused" || ex.Message == "An error occurred while sending the request.")
                    //{ }
                    //else
                    //{
                    //    throw new Exception(ex.Message);
                    //}
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            //}

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

                    await this.Explore(new Area(posX, posY, newLine, newLine)).ContinueWith((result) => {
                        if (newLine == 3 && result.Result.Amount > 0)
                        {
                            exploreQueue.Add(result.Result);
                        }
                    });
                }
            }
        }

        public async Task Xy(Area area, int line)
        {
            var newLine = line / 5;

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var posX = area.PosX.Value + (x * newLine);
                    var posY = area.PosY.Value + (y * newLine);

                    await this.Explore(new Area(posX, posY, newLine, newLine))
                        .ContinueWith((result) =>
                        {
                            if (result.Result.Amount >= result.Result.Area.SizeX * result.Result.Area.SizeY * 0.05)
                            {
                                searchQueue.TryAdd(result.Result);
                            }
                        });
                }
            }
        }

        public void Await()
        {
            using SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(100);
            List<Task> tasks = new List<Task>();

            foreach (var x in Enumerable.Range(0, 45))
            {
                foreach (var y in Enumerable.Range(0, 45))
                {
                    Xy(new Area(x * 75, y * 75, 75, 75), 75).Wait();
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        //public static void DoSomethingALotWithActionsThrottled()
        //{
        //    var listOfActions = new List<Action>();
        //    for (int i = 0; i < 100; i++)
        //    {
        //        var count = i;
        //        // Note that we create the Action here, but do not start it.
        //        listOfActions.Add(() => DoSomething(count));
        //    }

        //    var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };
        //    Parallel.Invoke(options, listOfActions.ToArray());
        //}
    }
}