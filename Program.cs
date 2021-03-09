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
        }
    }

    public class Game
    {
        public BlockingCollection<Task> areasQueue = new BlockingCollection<Task>();
        public BlockingCollection<Task<Report>> exploreQueue = new BlockingCollection<Task<Report>>();

        private readonly string Url;
        private readonly HttpClient HttpClient = new HttpClient();
        private License license;
        private DefaultApi API { get; set; }

        //IObservable<Task> ObservedAreas { get; set; }
        //private readonly ConcurrentQueue<Func<Task>> _workItems = new ConcurrentQueue<Func<Task>>();
        //private readonly List<Task> _runItems = new List<Task>();
        //private readonly SemaphoreSlim _signal;
        //private int GoldAverage = 0;
        //private int LicenseAverageCost = 0;

        public Game(DefaultApi base_url)
        {
            this.API = base_url;

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var action = areasQueue.Take();
                        action.Wait();
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                }
            });


            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var result = exploreQueue.Take().Result;

                        if (result != null)
                        {
                            while (!isAreaOpen())
                            {
                                Task.Delay(105).Wait();
                            }

                            areasQueue.Append(this.AfterExplore(result.Area.PosX.Value, result.Area.PosY.Value, result.Amount));
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                }
            });
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

        private async Task<TreasureList> Dig(Dig dig)
        {
            try
            {
                var request = await this.HttpClient.PostAsync($"{Url}/dig", new StringContent(JsonConvert.SerializeObject(dig), Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    //this.Field[dig.PosX, dig.PosY, dig.Depth] = 1;

                    var jsonString = await request.Content.ReadAsStringAsync();
                    var treasures = JsonConvert.DeserializeObject<TreasureList>(jsonString);

                    return treasures;

                    // получить сокровища, посчитать количество лицензий
                }
                else if (request.StatusCode == HttpStatusCode.Forbidden)
                {
                    await this.UpdateLicense();
                    return new TreasureList();
                }
                else
                {
                    return new TreasureList();
                }
            }
            catch (Exception e)
            {
                return new TreasureList();
            }
        }

        public async Task AfterExplore(int x, int y, int amount)
        {
            var depth = 1;
            var left = amount;

            //await Task.Delay(1000);
            //Console.WriteLine($"{x}={y}");

            while (depth <= 10 && left > 0)
            {
                while (this.license.Id == null || this.license.DigUsed >= this.license.DigAllowed)
                {
                    await this.UpdateLicense();
                }

                var dig = new Dig(this.license.Id.Value, x, y, depth);

                var treasures = await this.Dig(dig);

                this.license.DigUsed += 1;
                depth += 1;

                if (treasures.Any())
                {
                    foreach (var t in treasures)
                    {
                        await this.Cash(t);

                        left -= 1;
                    }
                }
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

        public async Task Start()
        {
            this.license = new License(0, 0, 0);
            try
            {
                for (int x = 1; x <= 3500; x++)
                {
                    for (int y = 1; y <= 3500; y++)
                    {
                        var explore = await this.Explore(new Area(x, y, 1, 1));
                        if (explore != null && explore.Amount > 0)
                        {
                            var left = explore.Amount;
                            var depth = 1;

                            while (left > 0 && depth <= 10)
                            {
                                await this.UpdateLicense();

                                var treasures = await this.API.DigAsyncWithHttpInfo(new Dig(this.license.Id.Value, x, y, depth));
                                this.license.DigUsed += 1;
                                depth += 1;

                                if (treasures.StatusCode == 200 && treasures.Data != null)
                                {
                                    if (treasures.Data.Any())
                                    {
                                        left -= treasures.Data.Count;

                                        foreach (var t in treasures.Data)
                                        {
                                            this.API.CashAsyncWithHttpInfo(t);
                                        }
                                    }
                                }
                                else if (treasures.StatusCode == 403)
                                {
                                    await this.UpdateLicense();
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                this.HttpClient.Dispose();
            }
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