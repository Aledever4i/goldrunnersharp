using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using goldrunnersharp.Model;
using goldrunnersharp.Api;

namespace goldrunnersharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = Environment.GetEnvironmentVariable("ADDRESS");

            UriBuilder myURI = new UriBuilder("http", address, 8000);
            var api = new DefaultApi(myURI.Uri.AbsoluteUri);
            var client = new Game(api);

            client.Start().Wait();
            client.HttpClient.Dispose();
        }
    }

    public class Game
    {
        public BlockingCollection<Task> areasQueue = new BlockingCollection<Task>();
        public BlockingCollection<Task<Report>> exploreQueue = new BlockingCollection<Task<Report>>();

        private readonly string Url;
        public readonly HttpClient HttpClient = new HttpClient();

        private License license;

        private DefaultApi API { get; set; }

        //IObservable<Task> ObservedAreas { get; set; }
        //private readonly ConcurrentQueue<Func<Task>> _workItems = new ConcurrentQueue<Func<Task>>();
        //private readonly List<Task> _runItems = new List<Task>();
        //
        //private int LicenseAverageCost = 0;

        private readonly SemaphoreSlim _licenseSignal = new SemaphoreSlim(0, 1);

        private int Chests;
        private int MoneyInChests;
        private int GoldAverage;

        public Game(DefaultApi base_url)
        {
            this.Chests = 490000;
            this.MoneyInChests = 23030000;
            this.GoldAverage = MoneyInChests / Chests;

            this.API = base_url;

            //Task.Factory.StartNew(() =>
            //{
            //    while (true)
            //    {
            //        if (_licenseSignal.CurrentCount == 0 && this.license.DigAllowed - this.license.DigUsed <= 1)
            //        {
            //            _licenseSignal.Wait();
            //            try
            //            {
            //                var license = this.API.IssueLicenseAsyncWithHttpInfo(new Wallet()).Result;
            //                if (license.Data.Id > 0 && license.Data.DigAllowed > 0)
            //                {
            //                    this.license = license.Data;
            //                }
            //            }
            //            finally
            //            {
            //                _licenseSignal.Release();
            //            }
            //        }
            //    }
            //});


            //Task.Factory.StartNew(() =>
            //{
            //    while (true)
            //    {
            //        try
            //        {
            //            var action = areasQueue.Take();
            //            action.Wait();
            //        }
            //        catch (InvalidOperationException)
            //        {
            //            break;
            //        }
            //    }
            //});


            //Task.Factory.StartNew(() =>
            //{
            //    while (true)
            //    {
            //        try
            //        {
            //            var result = exploreQueue.Take().Result;

            //            if (result != null)
            //            {
            //                while (!isAreaOpen())
            //                {
            //                    Task.Delay(105).Wait();
            //                }

            //                areasQueue.Append(this.AfterExplore(result.Area.PosX.Value, result.Area.PosY.Value, result.Amount));
            //            }
            //        }
            //        catch (InvalidOperationException)
            //        {
            //            break;
            //        }
            //    }
            //});
        }

        private async Task<IEnumerable<int>> Cash(string treasure)
        {
            try
            {
                var request = await this.HttpClient.PostAsync($"{Url}/cash", new StringContent(treasure, Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    var jsonString = await request.Content.ReadAsStringAsync();
                    var wallet = JsonConvert.DeserializeObject<IEnumerable<int>>(jsonString);

                    return wallet;
                }

                return Array.Empty<int>();
            }
            catch (Exception e)
            {
                return Array.Empty<int>();
            }
        }

        private async Task GetBalance()
        {
            try
            {
                var request = await this.HttpClient.GetAsync($"{Url}/balance");

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    // _json = await resp.json()
                    //wallet = Wallet(**_json)
                }
                else if (request.StatusCode != HttpStatusCode.OK)
                {
                    // запросить лицензию
                }
            }
            catch (Exception e)
            {
                return;
            }
        }

        // Может быть вернётся null
        private async Task<License> BuyLicense(Wallet wallet)
        {
            try
            {
                var request = await this.HttpClient.PostAsync($"{Url}/licenses", new StringContent(JsonConvert.SerializeObject(wallet), Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    var jsonString = await request.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<License>(jsonString);
                }

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        // Optional[List[License]]
        private async Task GetLicenseList()
        {
            try
            {
                var request = await this.HttpClient.GetAsync($"{Url}/licenses");

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    //_json = await resp.json()
                    //license_list = [License(**item) for item in _json]
                    //return license_list
                }
                else if (request.StatusCode != HttpStatusCode.OK)
                {
                    // запросить лицензию
                }
            }
            catch (Exception e)
            {
                return;
            }
        }

        public bool isExploreOpen()
        {
            return exploreQueue.Count <= 2;
        }

        public bool isAreaOpen()
        {
            return areasQueue.Count <= 2;
        }

        public async Task UpdateLicense()
        {
            while (this.license.DigUsed >= this.license.DigAllowed || this.license.Id == 0)
            {
                var license = await this.API.IssueLicenseAsyncWithHttpInfo(new Wallet());
                this.license = license.Data;
            }

            //if (this.license.DigAllowed - this.license.DigUsed == 1)
            //{
            //    var license = this.API.IssueLicenseAsyncWithHttpInfo(new Wallet()).Result;
            //    if (license.Data.Id > 0 && license.Data.DigAllowed > 0)
            //    {
            //        this.license = license.Data;
            //    }
            //}
        }

        public async Task<Report> Explore(Area area)
        {
            var tries = 0;

            while (true)
            {
                tries++;

                if (tries > 5)
                {
                    return null;
                }

                var explore = await this.API.ExploreAreaAsyncWithHttpInfo(area);

                if (explore.StatusCode == 200 && explore.Data != null)
                {
                    return explore.Data;
                }
            }
        }

        private Task Cash(TreasureList treasure)
        {
            return Task.Factory.StartNew(() => {
                foreach (var t in treasure)
                {
                    this.API.CashAsyncWithHttpInfo(t);
                }
            });
        }

        public async Task GoExplore(int fromX, int toX)
        {
            var license = new License(0, 0, 0);
            while (license.DigUsed >= license.DigAllowed || license.Id == 0)
            {
                license = (await this.API.IssueLicenseAsyncWithHttpInfo(new Wallet())).Data;
            }

            for (int x = fromX; x <= toX; x += 1)
            {
                for (int y = 1; y <= 3500; y++)
                {
                    var explore = await this.Explore(new Area(x, y, 1, 1));
                    if (explore != null && explore.Amount > 0) // && explore.Amount > this.GoldAverage * 2
                    {
                        var left = explore.Amount;
                        var depth = 1;

                        while (left > 0 && depth <= 10)
                        {
                            while (license.DigUsed >= license.DigAllowed || license.Id == 0)
                            {
                                license = (await this.API.IssueLicenseAsyncWithHttpInfo(new Wallet())).Data;
                            }

                            var treasures = await this.API.DigAsyncWithHttpInfo(new Dig(license.Id.Value, x, y, depth));
                            license.DigUsed += 1;
                            depth += 1;

                            if (treasures.StatusCode == 200 && treasures.Data != null)
                            {
                                left -= treasures.Data.Count;
                                if (treasures.Data.Count > 0)
                                {
                                    await this.Cash(treasures.Data);
                                }
                            }
                        }
                    }
                }
            }
        }

        public Task Start()
        {
            var task1 = this.GoExplore(1, 1000);
            var task2 = this.GoExplore(1001, 2000);
            var task3 = this.GoExplore(2001, 3000);
            var task4 = this.GoExplore(3001, 3500);

            return Task.WhenAll(task1, task2, task3, task4);

            //this.license = new License(0, 0, 0);
            //try
            //{
            //    for (int x = 1; x <= 3500; x += 1)
            //    {
            //        for (int y = 1; y <= 3500; y++)
            //        {
            //            var explore = await this.Explore(new Area(x, y, 1, 1));
            //            if (explore != null && explore.Amount >= 2) // && explore.Amount > this.GoldAverage * 2
            //            {
            //                //var ter = 0;
            //                //while (ter <= 0)
            //                //{
            //                    //explore = await this.Explore(new Area(x + ter, y, 1, 1));
            //                    //if (explore != null && explore.Amount > 0)
            //                    //{
            //                        //ter++;
            //                        var left = explore.Amount;
            //                        var depth = 1;

            //                        while (left > 0 && depth <= 10)
            //                        {
            //                            //if (this.license.DigAllowed - this.license.DigUsed <= 1)
            //                            //{
            //                            //    _ = this.UpdateLicense();
            //                            //}
            //                            //else if (this.license.DigAllowed - this.license.DigUsed == 0)
            //                            //{
            //                            await this.UpdateLicense();
            //                            //}

            //                            var treasures = await this.API.DigAsyncWithHttpInfo(new Dig(this.license.Id.Value, x, y, depth));
            //                            this.license.DigUsed += 1;
            //                            depth += 1;

            //                            if (treasures.StatusCode == 200 && treasures.Data != null)
            //                            {
            //                                left -= treasures.Data.Count;
            //                                if (treasures.Data.Count > 0)
            //                                {
            //                                    _ = this.Cash(treasures.Data);
            //                                }
            //                            }
            //                        }
            //                    //}
            //                //}
            //            }
            //        }
            //    }
            //}
        }
    
        
    }

    public static class TaskEx
    {
        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }
    }
}