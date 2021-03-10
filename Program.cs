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

namespace goldrunnersharp
{
    public class TriggeredBlockingCollectionEventArgs<T> : EventArgs
    {
        public T item { get; set; }

        public TriggeredBlockingCollectionEventArgs(T item)
        {
            this.item = item;
        }
    }

    public class TriggeredBlockingCollection<T>
    {
        public BlockingCollection<T> exploreQueue = new BlockingCollection<T>();

        public event EventHandler<TriggeredBlockingCollectionEventArgs<T>> OnAdd;

        public void Add(T item)
        {
            OnAdd(this, new TriggeredBlockingCollectionEventArgs<T>(item));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var address = Environment.GetEnvironmentVariable("ADDRESS");

            UriBuilder myURI = new UriBuilder("http", address, 8000);
            var api = new DefaultApi(myURI.Uri.AbsoluteUri);
            var client = new Game(api);

            client.Start();
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

    public class Game
    {
        public TriggeredBlockingCollection<Report> exploreQueue = new TriggeredBlockingCollection<Report>();

        public BlockingCollection<License> licenses = new BlockingCollection<License>();
        public BlockingCollection<Wallet> wallets = new BlockingCollection<Wallet>();

        private DefaultApi API { get; set; }

        private SemaphoreSlim _digSignal;
        private SemaphoreSlim _exploreSignal;

        public Game(DefaultApi base_url)
        {
            _digSignal = new SemaphoreSlim(10);
            _exploreSignal = new SemaphoreSlim(50);

            exploreQueue.OnAdd += GoDigEvent;

            this.API = base_url;
        }

        private void GoDigEvent(object sender, TriggeredBlockingCollectionEventArgs<Report> args)
        {
            _ = GoDig(args.item);
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
                try {
                    return (await this.API.IssueLicenseAsyncWithHttpInfo(wallet)).Data;
                }
                catch (ApiException ex)
                {
                    if (
                        (ex.ErrorCode > 500 && ex.ErrorCode < 600)
                        || (ex.ErrorContent == "An error occurred while sending the request. The response ended prematurely.")
                        || (ex.ErrorContent == "Connection refused Connection refused")
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

        public async Task<Report> Explore(Area area)
        {
            Report report = null;

            while (report == null)
            {
                try
                {
                    await Task.Delay(100);
                    report = (await this.API.ExploreAreaAsyncWithHttpInfo(area)).Data;
                }
                catch (ApiException ex)
                {
                    if ((ex.ErrorCode > 500 && ex.ErrorCode < 600) || ex.ErrorCode == 408 || ex.ErrorContent == "The request timed-out.")
                    {

                    }
                    else
                    {
                        throw ex;
                    }
                }
            }

            return report;
        }

        private async Task<Wallet> CashTreasure(string treasure)
        {
            Wallet report = null;

            while (report == null)
            {
                try
                {
                    report = (await this.API.CashAsyncWithHttpInfo(treasure)).Data;
                    if (report.Sum().HasValue)
                    {
                        wallets.Add(report);
                    }
                }
                catch (ApiException ex)
                {
                    if (ex.ErrorCode > 500 && ex.ErrorCode < 600)
                    {

                    }
                    else if (ex.ErrorCode == 409)
                    {
                        throw new Exception(treasure, ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }

            return report;

        }

        private void CashTreasureList(TreasureList treasures)
        {
            Task.WaitAll(
                treasures.Select((treasure) =>
                {
                    return CashTreasure(treasure);
                }).ToArray()
            );
        }

        private async Task<TreasureList> Dig(Dig dig)
        {
            TreasureList report = null;
            var digParams = dig;

            while (report == null)
            {
                try
                {
                    report = (await this.API.DigAsyncWithHttpInfo(digParams)).Data;
                }
                catch (ApiException ex)
                {
                    if (ex.ErrorCode > 500 && ex.ErrorCode < 600)
                    {

                    }
                    else if (ex.ErrorCode == 403)
                    {
                        var license = await UpdateLicense();
                        digParams.LicenseID = license.Id;
                    }
                    else if (ex.ErrorCode == 404)
                    {
                        break;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }

            return report;
            
        }

        public async Task GoDig(Report report)
        {
            try
            {
                _digSignal.Wait();

                var license = new License(0, 0, 0);
                //var left = report.Amount;
                var initY = report.Area.PosY.Value;
                var sizeY = report.Area.SizeY;

                for (int y = initY; (y < initY + sizeY); y++) // && left > 0
                {
                    var explore = await this.Explore(new Area(report.Area.PosX.Value, y, 1, 1));
                    var depth = 1;

                    while (depth <= 10 && explore.Amount > 0)
                    {
                        while (license.DigUsed >= license.DigAllowed)
                        {
                            license = await UpdateLicense();
                        }

                        var result = await Dig(new Dig(license.Id.Value, report.Area.PosX.Value, y, depth));
                        license.DigUsed += 1;
                        depth += 1;

                        if (result != null)
                        {
                            //left -= result.Count;
                            //explore.Amount -= result.Count;
                            this.CashTreasureList(result);

                            if (license.Id != 0 && license.DigUsed < license.DigAllowed)
                            {
                                licenses.Add(license);
                            }

                            return;
                        }
                    }

                }
            }
            finally
            {
                _digSignal.Release();
            }
        }

        public async Task BigExplore(int x)
        {
            try
            {
                _exploreSignal.Wait();

                foreach (var range in Enumerable.Range(0, 700))
                {
                    var explore = await this.Explore(new Area(x, range * 5, 1, 5));

                    if (explore.Amount > 0)
                    {
                        exploreQueue.Add(explore);
                    }
                }
            }
            finally
            {
                _exploreSignal.Release();
            }
        }

        public void Start()
        {
            var tasks = new List<Task>();

            foreach (var x in Enumerable.Range(0, 3500))
            {
                var task = BigExplore(x);
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
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