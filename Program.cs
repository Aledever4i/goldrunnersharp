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

            client.Start().Wait();
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
        public BlockingCollection<Report> exploreQueue = new BlockingCollection<Report>();
        //public TriggeredBlockingCollection<Report> exploreQueue = new TriggeredBlockingCollection<Report>();

        public BlockingCollection<License> licenses = new BlockingCollection<License>();
        public BlockingCollection<Wallet> wallets = new BlockingCollection<Wallet>();

        private Thread treadDig { get; set; }

        private const double Percent = 0.04;

        private DefaultApi API { get; set; }

        private SemaphoreSlim _digSignal;
        private SemaphoreSlim _exploreSignal;

        public Game(DefaultApi base_url)
        {
            _digSignal = new SemaphoreSlim(10);
            _exploreSignal = new SemaphoreSlim(200);

            //exploreQueue.OnAdd += GoDigEvent;

            this.API = base_url;

            treadDig = new Thread(processDig);
            treadDig.Start();
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

        //private void GoDigEvent(object sender, TriggeredBlockingCollectionEventArgs<Report> args)
        //{
        //    GoDig(args.item).Wait();
        //}

        //public Task GetLicense()
        //{
        //    var wallet = new Wallet();

        //    if (wallets.TryTake(out Wallet ws))
        //    {
        //        wallet = ws;
        //    }

        //    try
        //    {
        //        return this.API.IssueLicenseAsyncWithHttpInfo(wallet).ContinueWith((license) => { this.licenses.Add(license.Result.Data); });
        //    }
        //    catch (ApiException ex)
        //    {
        //        if (
        //            (ex.ErrorCode > 500 && ex.ErrorCode < 600)
        //            || (ex.ErrorContent == "An error occurred while sending the request. The response ended prematurely.")
        //            || (ex.ErrorContent == "Connection refused Connection refused")
        //        )
        //        {
        //        }
        //        else
        //        {
        //            throw ex;
        //        }
        //    }
        //}

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

        public async Task<Report> Explore(Area area)
        {
            Report report = null;

            while (report == null)
            {
                try
                {
                    report = (await this.API.ExploreAreaAsyncWithHttpInfo(area)).Data;
                }
                catch (ApiException ex)
                {
                    if (
                        (ex.ErrorCode > 500 && ex.ErrorCode < 600)
                        || ex.ErrorCode == 408
                        || ex.ErrorContent == "The request timed-out."
                        || ex.ErrorContent == "Connection refused Connection refused"
                        || ex.ErrorContent == "An error occurred while sending the request. The response ended prematurely."
                    )
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

        private async Task CashTreasure(string treasure, int depth, int insertDepth)
        {
            Wallet report = null;

            while (report == null)
            {
                try
                {
                    report = (await this.API.CashAsyncWithHttpInfo(treasure)).Data;
                    if (report.Any() && depth == 9)
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

                var license = await UpdateLicense();
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
                                    _ = CashTreasure(treasure, depth, 9);
                                }
                            }
                            depth += 1;
                        }
                    }
                }

                if (license.DigUsed < license.DigAllowed)
                {
                    licenses.Add(license);
                }
            }
            finally
            {
                _digSignal.Release();
            }
        }

        public async Task Xy2(Area area, int line)
        {
            var newLine = line / 5;

            var tasks = new List<Report>();
            var tasks2 = new List<Task>();

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var posX = area.PosX.Value + (x * newLine);
                    var posY = area.PosY.Value + (y * newLine);

                    var explore = await this.Explore(new Area(posX, posY, newLine, newLine));

                    tasks.Add(explore);
                }
            }

            foreach (var task in tasks.OrderByDescending((result) => { return result.Amount; }).Take(5))
            {
                if (newLine == 2 && task.Amount >= newLine * newLine * Percent)
                {
                    exploreQueue.Add(task);
                }
                else if (newLine != 2)
                {
                    tasks2.Add(Xy2(task.Area, newLine));
                }
            }

            await Task.WhenAll(tasks2.ToArray());
        }

        public async Task Start()
        {
            var tasks = new List<Task>();

            foreach (var x in Enumerable.Range(0, 70))
            {
                foreach (var y in Enumerable.Range(0, 70))
                {
                    await Xy2(new Area(x * 50, y * 50, 50, 50), 50);
                }
            }

            await Task.WhenAll(tasks.ToArray());  


            //var tasks = new List<Task>();
            //var searchTask = new List<Task>();

            //foreach (var x in Enumerable.Range(0, 3500))
            //{
            //    if (searchTask.Count > 1000000)
            //    {
            //        break;
            //    }

            //    foreach (var y in Enumerable.Range(0, 3500))
            //    {
            //        try
            //        {
            //            _exploreSignal.Wait();

            //            _ = Task.Run(() =>
            //            {
            //                var explore = this.Explore(new Area(x, y, 1, 1)).ContinueWith(async (result) =>
            //                {
            //                    if (result.Result.Amount > 0)
            //                    {
            //                        await GoDig(result.Result);
            //                    }
            //                });

            //                searchTask.Add(explore);
            //            });
            //        }
            //        finally
            //        {
            //            _exploreSignal.Release();
            //        }
            //    }
            //}

            //Task.WaitAll(searchTask.ToArray());
            //Task.WaitAll(tasks.ToArray());


            //foreach (var x in Enumerable.Range(0, 3500))
            //{
            //    foreach (var y in Enumerable.Range(0, 3500))
            //    {
            //        if (tasks.Count > 50000)
            //        {
            //            break;
            //        }

            //        try
            //        {
            //            await _exploreSignal.WaitAsync();

            //            var explore = await this.Explore(new Area(x, y, 1, 1));

            //            if (explore.Amount > 0)
            //            {
            //                var t = GoDig(explore);
            //                tasks.Add(t);
            //            }
            //        } 
            //        finally
            //        { 
            //            _exploreSignal.Release();
            //        }
            //    }
            //}


            //var t = Enumerable.Range(0, 3500).Select((x) =>
            //{
            //    return Task.Run(async () => {
            //        try
            //        {
            //            await _exploreSignal.WaitAsync();

            //            foreach (var range in Enumerable.Range(0, 3500))
            //            {
            //                if (tasks.Count > 50000)
            //                {
            //                    break;
            //                }

            //                var explore = await this.Explore(new Area(x, range, 1, 1));

            //                if (explore.Amount > 0)
            //                {
            //                    tasks.Add(GoDig(explore));
            //                }
            //            }

            //            //var task = BigExplore(x);
            //            //tasks.Add(task);
            //        }
            //        finally
            //        {
            //            _exploreSignal.Release();
            //        }
            //    });
            //}).ToArray();

            //Task.WaitAll(t);
        }
    }
}