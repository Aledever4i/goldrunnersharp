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

namespace goldrunnersharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = Environment.GetEnvironmentVariable("ADDRESS");
            //var address = "asd.com";

            UriBuilder myURI = new UriBuilder("http", address, 8000);
            var client = new Game(myURI.Uri);

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

        //IObservable<Task> ObservedAreas { get; set; }
        //private readonly ConcurrentQueue<Func<Task>> _workItems = new ConcurrentQueue<Func<Task>>();
        //private readonly List<Task> _runItems = new List<Task>();
        //private readonly SemaphoreSlim _signal;
        //private int GoldAverage = 0;
        //private int LicenseAverageCost = 0;

        public Game(Uri base_url)
        {
            this.Url = base_url.AbsoluteUri;

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

        private async Task UpdateLicense()
        {
            var wallet = new Wallet();

            var license = await this.BuyLicense(wallet);

            if (license != null)
            {
                this.license = license;
            }
            else
            {
                this.license = new License(0, 0, 0);
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

        private async Task<Report> Explore(Area area)
        {
            //return new ExploreResult() { Priority = 0, Area = new Area() { PosX = area.PosX, PosY = area.PosY }, Amount = 1000 };
            try
            {
                var request = await this.HttpClient.PostAsync($"{Url}/explore", new StringContent(JsonConvert.SerializeObject(area), Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    var jsonString = await request.Content.ReadAsStringAsync();
                    var report = JsonConvert.DeserializeObject<Report>(jsonString);

                    // информацию о разведанной территории, сравнить со среднем показателем и решить, выкапывать или нет.
                    //area = Area(**_json['area'])
                    return new Report(report.Area, report.Amount);
                }

                // Сделать счетчик
                return null; //await this.Explore(area);
            }
            catch (Exception e)
            {
                return null;
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

        public async Task Start()
        {
            try
            {
                for (int x = 1; x <= 3500; x++)
                {
                    for (int y = 1; y <= 3500; y++)
                    {
                        var s = await this.Explore(new Area(x, y, 1, 1));
                        if (s != null && s.Amount > 0)
                        {
                            await this.UpdateLicense();
                            var left = s.Amount;

                            while (left > 0 && license.DigAllowed > 0 && license.DigUsed < license.DigAllowed)
                            {
                                for (int i = 1; i <= 10; i++)
                                {
                                    var treasures = await this.Dig(new Dig(this.license.Id.Value, x, y, i));
                                    foreach (var t in treasures)
                                    {
                                        await this.Cash(t);
                                    }
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



//using Newtonsoft.Json;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Linq;
//using System.Reactive;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using goldrunnersharp.Model;
//using goldrunnersharp.Api;

//namespace goldrunnersharp
//{
//    //public class License
//    //{
//    //    public int? Id { get; set; } = null;

//    //    public int DigAllowed { get; set; } = 0;

//    //    public int DigUsed { get; set; } = 0;
//    //}

//    //public class Area
//    //{
//    //    public int PosX { get; set; }
//    //    public int PosY { get; set; }
//    //    public int SizeX { get; set; } = 1;
//    //    public int SizeY { get; set; } = 1;
//    //}

//    //public class Dig
//    //{
//    //    public int LicenseID { get; set; }

//    //    public int PosX { get; set; }

//    //    public int PosY { get; set; }

//    //    public int Depth { get; set; } = 1;
//    //}

//    //public class ExploreReport
//    //{
//    //    public Area Area { get; set; }

//    //    public int Amount { get; set; }
//    //}

//    //public class ExploreResult
//    //{
//    //    public int Priority { get; set; }

//    //    public Area Area { get; set; }

//    //    public int Amount { get; set; }
//    //}

//    //public class WalletClass
//    //{
//    //    public int Balance { get; set; } = 0;

//    //    public List<int> Wallet { get; set; } = new List<int>();
//    //}

//    //public class Treasure
//    //{
//    //    public int Priority { get; set; }

//    //    public List<string> Treasures { get; set; } = new List<string>();
//    //}

//    class Program
//    {
//        static void Main(string[] args)
//        {
//            //var address = "asd.com";
//            var address = Environment.GetEnvironmentVariable("ADDRESS");

//            var Uri = new UriBuilder()
//            {
//                Host = "http",
//                Port = 8000,
//                Path = address
//            };

//            var api = new DefaultApi(Uri.Uri.AbsoluteUri);
//            var client = new Game(api);
//            client.Start().Wait();
//        }
//    }

//    public class Game
//    {
//        public BlockingCollection<Task> areasQueue = new BlockingCollection<Task>();
//        public BlockingCollection<Task<Report>> exploreQueue = new BlockingCollection<Task<Report>>();

//        private readonly Uri Url;
//        private int[,,] Field = new int[3500, 3500, 10];

//        private const int InitialCount = 5;
//        private License license = new License();

//        private DefaultApi API { get; set; }


//        //IObservable<Task> ObservedAreas { get; set; }
//        //private readonly ConcurrentQueue<Func<Task>> _workItems = new ConcurrentQueue<Func<Task>>();
//        //private readonly List<Task> _runItems = new List<Task>();
//        //private readonly SemaphoreSlim _signal;
//        //private int GoldAverage = 0;
//        //private int LicenseAverageCost = 0;

//        public Game(DefaultApi api)
//        {
//            this.API = api;

//            Task.Factory.StartNew(() =>
//            {
//                while (true)
//                {
//                    try
//                    {
//                        var action = areasQueue.Take();
//                        action.Wait();
//                    }
//                    catch (InvalidOperationException)
//                    {
//                        break;
//                    }
//                }
//            });


//            Task.Factory.StartNew(() =>
//            {
//                while (true)
//                {
//                    try
//                    {
//                        var result = exploreQueue.Take().Result;

//                        if (result != null)
//                        {
//                            while (!isAreaOpen())
//                            {
//                                Task.Delay(105).Wait();
//                            }

//                            areasQueue.Append(this.AfterExplore(result.Area.PosX.Value, result.Area.PosY.Value, result.Amount));
//                        }
//                    }
//                    catch (InvalidOperationException)
//                    {
//                        break;
//                    }
//                }
//            });
//        }

//        private async Task<Wallet> Cash(string treasure)
//        {
//            try
//            {
//                var request = await this.API.CashAsyncWithHttpInfo(treasure);

//                if (request.StatusCode == (int)HttpStatusCode.OK)
//                {
//                    return request.Data;
//                }

//                return new Wallet();
//            }
//            catch (Exception e)
//            {
//                throw e;
//            }
//        }

//        private Task GetBalance()
//        {
//            try
//            {
//                var request = this.API.GetBalanceAsyncWithHttpInfo();

//                return request;

//                // ok
//                // _json = await resp.json()
//                //wallet = Wallet(**_json)


//                // или
//                // запросить лицензию
//            }
//            catch (Exception e)
//            {
//                return Task.FromException(e);
//            }
//        }

//        private async Task UpdateLicense()
//        {
//            var wallet = new Wallet();

//            var license = await this.BuyLicense(wallet);

//            this.license = license;
//        }

//        // Может быть вернётся null
//        private async Task<License> BuyLicense(Wallet coins)
//        {
//            try
//            {
//                var request = await this.API.IssueLicenseAsyncWithHttpInfo(coins);

//                if (request.StatusCode == (int)HttpStatusCode.OK)
//                {
//                    return request.Data;
//                }

//                return null;
//            }
//            catch (Exception e)
//            {
//                throw e;
//            }
//        }

//        // Optional[List[License]]
//        private async Task GetLicenseList()
//        {
//            try
//            {
//                var exploreRequest = await this.API.ListLicensesAsyncWithHttpInfo();

//                if (exploreRequest.StatusCode == (int)HttpStatusCode.OK)
//                {
//                    //_json = await resp.json()
//                    //license_list = [License(**item) for item in _json]
//                    //return license_list
//                }
//                else if (exploreRequest.StatusCode != (int)HttpStatusCode.OK)
//                {
//                    // запросить лицензию
//                }
//            }
//            catch (Exception e)
//            {
//                throw e;
//            }
//        }

//        private async Task<Report> Explore(Area area)
//        {
//            //return new ExploreResult() { Priority = 0, Area = new Area() { PosX = area.PosX, PosY = area.PosY }, Amount = 1000 };

//            try
//            {
//                var exploreRequest = await this.API.ExploreAreaAsyncWithHttpInfo(area);

//                if (exploreRequest.StatusCode == (int)HttpStatusCode.OK)
//                {
//                    return exploreRequest.Data;

//                    // информацию о разведанной территории, сравнить со среднем показателем и решить, выкапывать или нет.

//                    //area = Area(**_json['area'])
//                    //return new ExploreResult() { Priority = 0, Area = report.Area, Amount = report.Amount };
//                }

//                // Сделать счетчик
//                return null; //await this.Explore(area);
//            }
//            catch (Exception e)
//            {
//                throw e;
//            }
//        }

//        private async Task<IEnumerable<string>> Dig(Dig dig)
//        {
//            try
//            {
//                var treasuresRequest = await this.API.DigAsyncWithHttpInfo(dig);

//                if (treasuresRequest.StatusCode == (int)HttpStatusCode.OK)
//                {
//                    //this.Field[dig.PosX, dig.PosY, dig.Depth] = 1;

//                    // получить сокровища, посчитать количество лицензий
//                    return treasuresRequest.Data;
//                }
//                else if (treasuresRequest.StatusCode == (int)HttpStatusCode.Forbidden)
//                {
//                    // запросить лицензию
//                    await this.API.IssueLicenseAsyncWithHttpInfo();
//                }

//                return Array.Empty<string>();
//            }
//            catch (Exception e)
//            {
//                throw e;
//            }
//        }

//        public async Task AfterExplore(int x, int y, int amount)
//        {
//            var depth = 1;
//            var left = amount;

//            //await Task.Delay(1000);
//            //Console.WriteLine($"{x}={y}");

//            while (depth <= 10 && left > 0)
//            {
//                while (this.license.Id == null || this.license.DigUsed >= this.license.DigAllowed)
//                {
//                    await this.UpdateLicense();
//                }

//                var dig = new Dig() { LicenseID = this.license.Id.Value, PosX = x, PosY = y, Depth = depth };

//                var treasures = await this.Dig(dig);

//                this.license.DigUsed += 1;
//                depth += 1;

//                if (treasures.Any())
//                {
//                    treasures.AsParallel().ForAll(
//                        async (treasure) =>
//                        {
//                            await this.Cash(treasure).ContinueWith(result => { left -= 1; }, TaskContinuationOptions.OnlyOnRanToCompletion);
//                        }
//                    );
//                }
//            }
//        }

//        public bool isExploreOpen()
//        {
//            return exploreQueue.Count <= 2;
//        }

//        public bool isAreaOpen()
//        {
//            return areasQueue.Count <= 2;
//        }

//        public async Task Start()
//        {
//            try
//            {
//                for (int x = 1; x <= 3500; x++)
//                {
//                    for (int y = 1; y <= 3500; y++)
//                    {
//                        var area = new Area() { PosX = x, PosY = y };

//                        while (!isExploreOpen())
//                        {
//                            await Task.Delay(105);
//                        }

//                        exploreQueue.Add(this.Explore(area));
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }
//    }

//    public static class TaskEx
//    {
//        /// <summary>
//        /// Blocks while condition is true or timeout occurs.
//        /// </summary>
//        /// <param name="condition">The condition that will perpetuate the block.</param>
//        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
//        /// <param name="timeout">Timeout in milliseconds.</param>
//        /// <exception cref="TimeoutException"></exception>
//        /// <returns></returns>
//        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
//        {
//            var waitTask = Task.Run(async () =>
//            {
//                while (condition()) await Task.Delay(frequency);
//            });

//            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
//                throw new TimeoutException();
//        }

//        /// <summary>
//        /// Blocks until condition is true or timeout occurs.
//        /// </summary>
//        /// <param name="condition">The break condition.</param>
//        /// <param name="frequency">The frequency at which the condition will be checked.</param>
//        /// <param name="timeout">The timeout in milliseconds.</param>
//        /// <returns></returns>
//        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
//        {
//            var waitTask = Task.Run(async () =>
//            {
//                while (!condition()) await Task.Delay(frequency);
//            });

//            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
//            {
//                throw new TimeoutException();
//            }
//        }
//    }
//}
