/* 
 * HighLoad Cup 2021
 *
 * ## Usage ## List of all custom errors First number is HTTP Status code, second is value of \"code\" field in returned JSON object, text description may or may not match \"message\" field in returned JSON object. - 422.1000: wrong coordinates - 422.1001: wrong depth - 409.1002: no more active licenses allowed - 409.1003: treasure is not digged 
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */


using System;
using RestSharp;

namespace goldrunnersharp.Client
{
    /// <summary>
    /// A delegate to ExceptionFactory method
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <param name="response">Response</param>
    /// <returns>Exceptions</returns>
    public delegate Exception ExceptionFactory(string methodName, IRestResponse response);
}
