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

namespace goldrunnersharp
{
    public class License
    {
        public int? Id { get; set; } = null;

        public int DigAllowed { get; set; } = 0;

        public int DigUsed { get; set; } = 0;
    }

    public class Area
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int SizeX { get; set; } = 1;
        public int SizeY { get; set; } = 1;
    }

    public class Dig
    {
        public int LicenseID { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }

        public int Depth { get; set; } = 1;
    }

    public class ExploreReport
    {
        public Area Area { get; set; }

        public int Amount { get; set; }
    }

    public class ExploreResult
    {
        public int Priority { get; set; }

        public Area Area { get; set; }

        public int Amount { get; set; }
    }

    public class WalletClass
    {
        public int Balance { get; set; } = 0;

        public List<int> Wallet { get; set; } = new List<int>();
    }

    public class Treasure
    {
        public int Priority { get; set; }

        public List<string> Treasures { get; set; } = new List<string>();
    }

    class Program
    {
        static void Main(string[] args)
        {
            var address = Environment.GetEnvironmentVariable("ADDRESS");

            UriBuilder myURI = new UriBuilder("http", address, 8000);

            var client = new Client(myURI.Uri);

            Console.WriteLine(client.ToString());
            //_ = client.Start();
        }
    }

    public class Client
    {
        private readonly Uri Url;
        private int[,,] Field = new int[3500, 3500, 10];
        private readonly HttpClient HttpClient = new HttpClient();

        private int GoldAverage = 0;
        private int LicenseAverageCost = 0;

        private readonly ConcurrentQueue<Func<Task>> _workItems = new ConcurrentQueue<Func<Task>>();
        private readonly List<Task> _runItems = new List<Task>();
        private readonly SemaphoreSlim _signal;
        private const int InitialCount = 5;
        private License license = new License();

        public Client(Uri base_url)
        {
            this.Url = base_url;
        }

        private async Task<IEnumerable<int>> Cash(string treasure)
        {
            try
            {
                var request = await this.HttpClient.PostAsync($"{Url.AbsoluteUri}/cash", new StringContent(treasure, Encoding.UTF8, "application/json"));

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
                throw e;
            }
        }

        private Task GetBalance()
        {
            try
            {
                var request = this.HttpClient.GetAsync($"{Url.AbsoluteUri}/balance");

                return request.ContinueWith(
                    result =>
                    {
                        if (result.Result.StatusCode == HttpStatusCode.OK)
                        {
                            // _json = await resp.json()
                            //wallet = Wallet(**_json)
                        }
                        else if (result.Result.StatusCode != HttpStatusCode.OK)
                        {
                            // запросить лицензию
                        }
                    }
                );
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        private async Task UpdateLicense()
        {
            var coins = new int[] { };

            var licenses = await this.BuyLicense(coins.AsEnumerable());

            if (licenses != null)
            {
                this.license = licenses.FirstOrDefault();
            }
        }

        // Может быть вернётся null
        private async Task<IEnumerable<License>> BuyLicense(IEnumerable<int> coins)
        {
            var json = JsonConvert.SerializeObject(coins);

            try
            {
                var request = await this.HttpClient.PostAsync($"{Url.AbsoluteUri}/licenses", new StringContent(json, Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    var jsonString = await request.Content.ReadAsStringAsync();
                    var licenses = JsonConvert.DeserializeObject<IEnumerable<License>>(jsonString);

                    return licenses;
                }

                return null;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        // Optional[List[License]]
        private Task GetLicenseList()
        {
            try
            {
                var request = this.HttpClient.GetAsync($"{Url.AbsoluteUri}/licenses");

                return request.ContinueWith(
                    result =>
                    {
                        if (result.Result.StatusCode == HttpStatusCode.OK)
                        {
                            //_json = await resp.json()
                            //license_list = [License(**item) for item in _json]
                            //return license_list
                        }
                        else if (result.Result.StatusCode != HttpStatusCode.OK)
                        {
                            // запросить лицензию
                        }
                    }
                );
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        private async Task<ExploreResult> Explore(Area area)
        {
            var json = JsonConvert.SerializeObject(area);

            try
            {
                var request = await this.HttpClient.PostAsync($"{Url.AbsoluteUri}/explore", new StringContent(json, Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    var jsonString = await request.Content.ReadAsStringAsync();
                    var report = JsonConvert.DeserializeObject<ExploreReport>(jsonString);

                    // информацию о разведанной территории, сравнить со среднем показателем и решить, выкапывать или нет.

                    //area = Area(**_json['area'])
                    return new ExploreResult() { Priority = 0, Area = report.Area, Amount = report.Amount };
                }

                // Сделать счетчик
                return null; //await this.Explore(area);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<IEnumerable<string>> Dig(Dig dig)
        {
            var json = JsonConvert.SerializeObject(dig);

            try
            {
                var request = await this.HttpClient.PostAsync($"{Url.AbsoluteUri}/dig", new StringContent(json, Encoding.UTF8, "application/json"));

                if (request.StatusCode == HttpStatusCode.OK)
                {
                    //this.Field[dig.PosX, dig.PosY, dig.Depth] = 1;

                    var jsonString = await request.Content.ReadAsStringAsync();
                    var treasures = JsonConvert.DeserializeObject<IEnumerable<string>>(jsonString);

                    return treasures;

                    // получить сокровища, посчитать количество лицензий
                }
                else if (request.StatusCode == HttpStatusCode.Forbidden)
                {
                    // запросить лицензию
                }

                return Array.Empty<string>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task Start()
        {
            try
            {
                for (int x = 1; x <= 3500; x++)
                {
                    for (int y = 1; y <= 3500; y++)
                    {
                        var area = new Area() { PosX = x, PosY = y };
                        var result = await this.Explore(area);

                        if (result == null)
                        {
                            continue;
                        }

                        var depth = 1;
                        var left = result.Amount;

                        while (depth <= 10 && left > 0)
                        {
                            while (this.license.Id == null || this.license.DigUsed >= this.license.DigAllowed)
                            {
                                await this.UpdateLicense();
                            }

                            var dig = new Dig() { LicenseID = this.license.Id.Value, PosX = x, PosY = y, Depth = depth };

                            var treasures = await this.Dig(dig);

                            this.license.DigUsed += 1;
                            depth += 1;

                            if (treasures.Any())
                            {
                                treasures.AsParallel().ForAll(
                                    async (treasure) => {
                                        await this.Cash(treasure).ContinueWith(result => { left -= 1; }, TaskContinuationOptions.OnlyOnRanToCompletion);
                                    }
                                );
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
}
