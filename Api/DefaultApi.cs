using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using goldrunnersharp.Client;
using goldrunnersharp.Model;
using RestSharp;

namespace goldrunnersharp.Api
{

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class DefaultApi
    {
        private ExceptionFactory _exceptionFactory = (name, response) => null;

        public DefaultApi(String basePath)
        {
            this.Configuration = new goldrunnersharp.Client.Configuration { BasePath = basePath };

            ExceptionFactory = goldrunnersharp.Client.Configuration.DefaultExceptionFactory;
        }

        public String GetBasePath()
        {
            return this.Configuration.ApiClient.RestClient.BaseUrl.ToString();
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public Configuration Configuration {get; set;}

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        public async Task<ApiResponse<Wallet>> CashAsyncWithHttpInfo (string args)
        {
            var localVarPath = "/cash";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (args != null && args.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(args); // http body (model) parameter
            }
            else
            {
                localVarPostBody = args; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("Cash", localVarResponse);
            //    if (exception != null) throw exception;
            //}
            if (localVarStatusCode == 200)
            {
                return new ApiResponse<Wallet>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (Wallet)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(Wallet)));
            }

            return new ApiResponse<Wallet>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);

        }

        public async Task<ApiResponse<TreasureList>> DigAsyncWithHttpInfo (Dig args)
        {
            var localVarPath = "/dig";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (args != null && args.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(args); // http body (model) parameter
            }
            else
            {
                localVarPostBody = args; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("Dig", localVarResponse);
            //    if (exception != null) throw exception;
            //}
            if (localVarStatusCode == 200)
            {
                return new ApiResponse<TreasureList>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (TreasureList)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(TreasureList)));
            }

            return new ApiResponse<TreasureList>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);

        }

        public async Task<ApiResponse<Report>> ExploreAreaAsyncWithHttpInfo (Area args)
        {
            var localVarPath = "/explore";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (args != null && args.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(args); // http body (model) parameter
            }
            else
            {
                localVarPostBody = args; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("ExploreArea", localVarResponse);
            //    if (exception != null) throw exception;
            //}

            if (localVarStatusCode == 200)
            {
                return new ApiResponse<Report>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (Report)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(Report)));
            }

            return new ApiResponse<Report>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);

        }

        public async Task<ApiResponse<Balance>> GetBalanceAsyncWithHttpInfo ()
        {

            var localVarPath = "/balance";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);



            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("GetBalance", localVarResponse);
            //    if (exception != null) throw exception;
            //}


            if (localVarStatusCode == 200)
            {
                return new ApiResponse<Balance>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (Balance)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(Balance)));
            }

            return new ApiResponse<Balance>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);

        }

        public async Task<ApiResponse<Dictionary<string, Object>>> HealthCheckAsyncWithHttpInfo ()
        {

            var localVarPath = "/health-check";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);



            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("HealthCheck", localVarResponse);
            //    if (exception != null) throw exception;
            //}


            if (localVarStatusCode == 200)
            {
                return new ApiResponse<Dictionary<string, Object>>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (Dictionary<string, Object>)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(Dictionary<string, Object>)));
            }

            return new ApiResponse<Dictionary<string, Object>>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);

        }

        public async Task<ApiResponse<License>> IssueLicenseAsyncWithHttpInfo (Wallet args = null)
        {

            var localVarPath = "/licenses";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (args != null && args.GetType() != typeof(byte[]))
            {
                localVarPostBody = this.Configuration.ApiClient.Serialize(args); // http body (model) parameter
            }
            else
            {
                localVarPostBody = args; // byte array
            }

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("IssueLicense", localVarResponse);
            //    if (exception != null) throw exception;
            //}

            if (localVarStatusCode == 200)
            {
                return new ApiResponse<License>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (License)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(License)));
            }

            return new ApiResponse<License>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);
        }

        public async Task<ApiResponse<LicenseList>> ListLicensesAsyncWithHttpInfo ()
        {

            var localVarPath = "/licenses";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);



            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            //if (ExceptionFactory != null)
            //{
            //    Exception exception = ExceptionFactory("ListLicenses", localVarResponse);
            //    if (exception != null) throw exception;
            //}

            if (localVarStatusCode == 200)
            {
                return new ApiResponse<LicenseList>(localVarStatusCode,
                    localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    (LicenseList)this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(LicenseList)));
            }

            return new ApiResponse<LicenseList>(localVarStatusCode, localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()), null);
        }

    }
}
