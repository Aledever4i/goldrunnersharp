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
            var client = new Game(api, myURI.Uri);

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
        public BlockingCollection<Report> exploreQueue = new BlockingCollection<Report>();
        public BlockingCollection<License> licenses = new BlockingCollection<License>();
        public BlockingCollection<Wallet> wallets = new BlockingCollection<Wallet>();

        HttpClient httpClient = new HttpClient();

        private Thread treadDig { get; set; }

        private const double Percent = 0.04;

        private DefaultApi API { get; set; }

        private SemaphoreSlim _digSignal;
        private SemaphoreSlim _exploreSignal;

        public Game(DefaultApi base_url, Uri base_url2)
        {
            _digSignal = new SemaphoreSlim(10);
            _exploreSignal = new SemaphoreSlim(100);

            this.API = base_url;
            this.httpClient = new HttpClient()
            {
                BaseAddress = base_url2,
                Timeout = TimeSpan.FromMilliseconds(2000)
            };

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

        public async Task GetLicenses()
        {
            while (true)
            {
                try
                {
                    var s = (await this.API.ListLicensesAsyncWithHttpInfo()).Data;
                    while (licenses.Count > 0) { licenses.TryTake(out License item); }

                    s.ForEach((lic) => {
                        licenses.TryAdd(lic);
                    });
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
                    else if (ex.ErrorCode == 409)
                    {

                    }
                    else
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;

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
                    else if (ex.ErrorCode == 409)
                    {
                        await GetLicenses();

                        if (licenses.TryTake(out license))
                        {
                            return license;
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
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

        private async Task CashTreasure(string treasure, int depth, int insertDepth)
        {
            Wallet report = null;

            while (report == null)
            {
                try
                {
                    var request = await this.httpClient.PostAsync($"/cash", new StringContent(JsonConvert.SerializeObject(treasure), Encoding.UTF8, "application/json"));

                    if (request.StatusCode == HttpStatusCode.OK)
                    {
                        var jsonString = await request.Content.ReadAsStringAsync();
                        report = JsonConvert.DeserializeObject<Wallet>(jsonString);

                        if (report.Any() && depth == 8)
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
                            await _digSignal.WaitAsync();
                            var license = await UpdateLicense();

                            try
                            {
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

        //public async Task Xy2(Area area, int line)
        //{
        //    var newLine = line / 5;

        //    var tasks = new List<Report>();
        //    var tasks2 = new List<Task>();

        //    for (int x = 0; x < 5; x++)
        //    {
        //        for (int y = 0; y < 5; y++)
        //        {
        //            var posX = area.PosX.Value + (x * newLine);
        //            var posY = area.PosY.Value + (y * newLine);

        //            var explore = await this.Explore(new Area(posX, posY, newLine, newLine));

        //            tasks.Add(explore);
        //        }
        //    }

        //    foreach (var task in tasks.OrderByDescending((result) => { return result.Amount; }).Take(5))
        //    {
        //        if (newLine == 2 && task.Amount >= newLine * newLine * Percent)
        //        {
        //            exploreQueue.Add(task);
        //        }
        //        else if (newLine != 2)
        //        {
        //            tasks2.Add(Xy2(task.Area, newLine));
        //        }
        //    }

        //    await Task.WhenAll(tasks2.ToArray());
        //}

        public async Task Xy2(Area area, int line)
        {
            _exploreSignal.Wait();

            var newLine = line / 5;

            var tasks = new List<Report>();
            var tasks2 = new List<Task>();

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var posX = area.PosX.Value + (x * newLine);
                    var posY = area.PosY.Value + (y * newLine);

                    await this.Explore(new Area(posX, posY, newLine, newLine)).ContinueWith(
                        async (result) =>
                        {
                            if (result.Result.Amount > 0)
                            {
                                await GoDig(result.Result);
                            }
                        },
                        TaskContinuationOptions.RunContinuationsAsynchronously
                    );
                }
            }

            _exploreSignal.Release();
        }

        public void Start()
        {
            Parallel.For(0, 350, (x) =>
            {
                Parallel.For(0, 350, async (y) =>
                {
                    await Xy2(new Area(x * 10, y * 10, 10, 10), 10);
                });
            });
        }
    }
}