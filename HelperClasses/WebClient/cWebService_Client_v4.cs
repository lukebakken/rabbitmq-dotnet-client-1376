using System;
using System.Collections.Generic;
using System.Net;

namespace NETCore_Common.WebService
{
    /// <summary>
    /// Deprecates cWebService_Client and cWebService_Client_v3.
    /// Combines duplicated request methods into a single call that handles all parameter types.
    /// Added capability to support web calls that return gzip compressed content.
    /// Updated to return a positive result as long as a web response was received, regardless of it's status code.
    /// This change leaves it up to the caller to interpret pass or fail.
    /// </summary>
    static public class cWebService_Client_v4
    {
        #region Public Properties

        static public bool Ignore_SSL_Certificate_Errors = false;

        /// <summary>
        /// Default the web request timeout to 3 seconds...
        /// </summary>
        static public int _request_timeout = 10000;

        /// <summary>
        /// Request timeout in milliseconds.
        /// </summary>
        static public int Request_Timeout
        {
            get => _request_timeout;
            set
            {
                if (value < 1)
                    return;
                else
                    _request_timeout = value;
            }
        }

        #endregion


        /// <summary>
        /// Simplifies functionality to send a request to a web service.
        /// Disposes of all streams and other instances used in request.
        /// This is the base call of all the web service request. And so, it contains all possible parameters.
        /// An HTTP Status code is passed back by this method, that provides feedback from the handling web service controller action.
        /// If the request body is not null, the request header will include a ContentType = "application/json" and ContentLength = jsonrequest.Length.
        /// Returns the following:
        ///  1 = Successful web call. A response was received.
        ///  0 = Web Service is down.
        /// -1 = Request was unsuccessful.
        /// Other negative values indicate generic exception issues during web service call.
        /// </summary>
        /// <param name="url">The full url string of the web service call</param>
        /// <param name="username">Username to be passed as netcredential. Set to null if not used.</param>
        /// <param name="password">Password to be passed as netcredential. Set to null if not used.</param>
        /// <param name="verb">Http verp: GET, PUT, POST, DELETE, etc...</param>
        /// <param name="headerentries">Dictionary list of entries to add to the request header.</param>
        /// <param name="jsonrequest">Request body to send to web service endpoint. If not needed, set to null.</param>
        /// <returns></returns>
        static public cWebClient_Return Web_Request_Method(string url,
                                             string username,
                                             string password,
                                             NETCore_Common.WebService.eHttp_Verbs verb,
                                             Dictionary<string, string> headerentries,
                                             string jsonrequest)
        {
            System.Net.HttpWebRequest req = null;
            System.IO.StreamWriter requestwriter = null;
            System.Net.HttpWebResponse webResponse = null;
            System.IO.Stream webStream = null;
            System.IO.StreamReader responseReader = null;
            System.Net.WebResponse webexception_response = null;
            System.Net.NetworkCredential netcred = null;
            cWebClient_Return callerreturn = new cWebClient_Return();

            var sslFailureCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });

            // This try-finally ensures the SSL cert validation callback is unsubscribed.
            try
            {
                // Setup a callback for certificate handling...
                if (Ignore_SSL_Certificate_Errors)
                {
                    ServicePointManager.ServerCertificateValidationCallback += sslFailureCallback;
                }

                // Setup the base request instance...
                try
                {
                    // Create a new web request for the desired URL.
                    req = Create_WebRequest(url, verb);

                    // Add automatic gzip handling for sites that respond with "content-encoding" = gzip...
                    // This line will automatically decompress responses back to their native types.
                    req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                    // Since this is a highly overridden method, see if the override gave us username and password...
                    if (username == null && password == null)
                    {
                        // No username and password was give.
                        // We won't add anything to the request instance.
                    }
                    else
                    {
                        // The caller gave us a username and password.

                        // Add the user credentials to the web call...
                        netcred = new NetworkCredential(username, password);
                        System.Net.CredentialCache ccache = new CredentialCache { { new Uri(url), "Basic", netcred } };
                        req.PreAuthenticate = true;
                        req.Credentials = netcred;
                    }

                    // Add headers if any are defined.
                    if (headerentries != null)
                    {
                        // An instance is valid.
                        // See if any entries are in it.
                        if (headerentries.Count > 0)
                        {
                            // Setup the header with our session id and token.
                            var headers = req.Headers;

                            foreach (var s in headerentries)
                            {
                                headers.Add(s.Key, s.Value);
                            }

                            req.KeepAlive = true;
                            req.Headers = headers;
                        }
                    }
                }
                catch (Exception e)
                {
                    // An error occurred while setting up the request instance.

                    // Attempt to log the returned status from the server.
                    // Push to the log if logging level is correct.
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                        "Exception occurred while setting up web service request. " +
                                                            "url=" + url + ";\r\n" +
                                                            "verb=" + verb.ToString() + ";\r\n" +
                                                            "Request=" + (jsonrequest ?? "") + ".");

                    callerreturn.Call_ReturnCode = -3;
                    return callerreturn;
                }
                // The base request is setup.

                // Since this method is the base handler for many overrides, we need to see if it is being used to send a request to the web service.
                if (jsonrequest != null)
                {
                    // We have a request body to send to the web service.

                    // Set some values for the payload and type...
                    req.ContentType = "application/json";
                    req.ContentLength = jsonrequest.Length;

                    // This try catch wraps the request writer to ensure it gets disposed.
                    // And, catches any unspecified exceptions from the request push.
                    try
                    {
                        // Push to the log if logging level is correct.
                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                            "Attempting to send a request to the web server. " +
                                                                "url=" + url + ";\r\n" +
                                                                "verb=" + verb.ToString() + ";\r\n" +
                                                                "Request=" + (jsonrequest ?? "") + ".");

                        // Push the web request to the web service.
                        // This is wrapped in a try catch, so we can intelligently catch connection issues without them just hitting the log as errors.
                        try
                        {
                            // Push the request to the server.
                            requestwriter = new System.IO.StreamWriter(req.GetRequestStream(), System.Text.Encoding.ASCII);
                            requestwriter.Write(jsonrequest);
                        }
                        catch (System.Net.WebException we)
                        {
                            // An error occurred while pushing the web request to the web service.

                            // Attempt to log the returned status from the server.
                            OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                                "Attempted to get send a request to the web server, but it was unavailable. " +
                                                "Status=" + we?.Status + ";\r\n" +
                                                "url=" + url + ";\r\n" +
                                                "verb=" + verb.ToString() + ".");

                            // See if a response was received.
                            if (we.Response != null)
                            {
                                // A response was received through the web exception.
                                // Get some data from it.

                                // Get the response and cast it to what we can use.
                                webexception_response = we.Response;
                                System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)webexception_response;

                                // Log the status code and description from it.
                                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                                    "Web service call returned an error with status code of: {0}, and status description of: {1}",
                                    (httpResponse?.StatusCode.ToString() ?? ""), (httpResponse?.StatusDescription ?? ""));
                            }

                            callerreturn.Call_ReturnCode = 0;
                            return callerreturn;
                        }
                        finally
                        {
                            if (webexception_response != null)
                            {
                                webexception_response?.Dispose();
                                webexception_response = null;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // An error was caught while pushing a web service request.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                                "An error was caught while pushing a web service request. " +
                                                                "url=" + url + ";\r\n" +
                                                                "verb=" + verb.ToString() + ";\r\n" +
                                                                "Request=" + (jsonrequest ?? "") + ".");

                        callerreturn.Call_ReturnCode = -2;
                        return callerreturn;
                    }
                    finally
                    {
                        try
                        {
                            requestwriter?.Close();
                        }
                        catch (Exception f)
                        { }
                        try
                        {
                            requestwriter?.Dispose();
                        }
                        catch (Exception g)
                        { }
                    }
                    // The web service request has been sent.
                    // We need to process the reply.
                }
                // Request effort is done.
                // We need to move on and process any response.

                // Get a response from the web service...
                // This try block wraps all response logic to ensure streams are disposed and unspecified exceptions are caught.
                try
                {
                    // Get the response from the web service.
                    // This is wrapped in a try catch, so we can intelligently catch connection issues without them just hitting the log as errors.
                    try
                    {
                        // Get the response from the web service.
                        webResponse = (System.Net.HttpWebResponse)req.GetResponse();
                    }
                    catch (System.Net.WebException we)
                    {
                        // An error occurred while getting the web response from the web service.

                        if (we?.Message == "The remote server returned an error: (404) Not Found.")
                        {
                            // The web service reports a 404, Not Found.
                        }

                        // Attempt to log the returned status from the server.
                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Warn(
                                "Attempted to get send a request to the web server, but a WebException occurred. " +
                                            "Message=" + (we?.Message ?? "") + ";\r\n" +
                                            "Status=" + we.Status + ";\r\n" +
                                            "url=" + url + ";\r\n" +
                                            "verb=" + verb.ToString() + ".");

                        // See if a response was received.
                        if (we.Response != null)
                        {
                            // A response was received through the web exception.
                            // Get some data from it.

                            // Get the response and cast it to what we can use.
                            webexception_response = we.Response;
                            System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)webexception_response;

                            // Log the status code and description from it.
                            OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(
                                "Web service call returned an error with status code of: {0}, and status description of: {1}",
                                (httpResponse?.StatusCode.ToString() ?? ""), (httpResponse?.StatusDescription ?? ""));

                            callerreturn.StatusCode = httpResponse.StatusCode;
                            callerreturn.Call_ReturnCode = 1;
                            return callerreturn;
                        }
                        else
                        {
                            // No response is available.

                            // We will return a zero for lack of response...
                            callerreturn.Call_ReturnCode = 0;
                            return callerreturn;
                        }
                    }
                    finally
                    {
                        if (webexception_response != null)
                        {
                            webexception_response.Dispose();
                            webexception_response = null;
                        }
                    }
                    // If here, the web service is up, and we are processing response content or errors it gives us.

                    // Pass back the status code.
                    callerreturn.StatusCode = webResponse.StatusCode;

                    // Attempt to retrieve the response from the call...
                    try
                    {
                        // Create a stream to read back the reply.
                        webStream = webResponse.GetResponseStream();
                        responseReader = new System.IO.StreamReader(webStream);

                        // Get the response body from the web service.
                        string response = responseReader.ReadToEnd();

                        // Copy it to our response string.
                        callerreturn.JSONResponse = response;

                        // Push to the log if logging level is correct.
                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                                "Web service call was successful. " +
                                "url=" + url + ";\r\n" +
                                "verb=" + verb.ToString() + ";\r\n" +
                                "Request=" + (jsonrequest ?? "") + ";\r\n" +
                                "StatusCode=" + webResponse?.StatusCode.ToString() + ";\r\n" +
                                "StatusDescription=" + webResponse?.StatusDescription + ";\r\n" +
                                "Response=" + (callerreturn?.JSONResponse ?? "") + ".");

                        callerreturn.Call_ReturnCode = 1;
                        return callerreturn;
                    }
                    catch (Exception e)
                    {
                        // An error was caught while doing the web call.

                        OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                                "An exception occurred while processing the web response.");

                        callerreturn.Call_ReturnCode = -4;
                        return callerreturn;
                    }
                    finally
                    {
                        try
                        {
                            responseReader?.Close();
                        }
                        catch (Exception h)
                        { }
                        try
                        {
                            responseReader?.Dispose();
                        }
                        catch (Exception i)
                        { }

                        try
                        {
                            webStream?.Close();
                        }
                        catch (Exception h)
                        { }
                        try
                        {
                            webStream?.Dispose();
                        }
                        catch (Exception i)
                        { }
                    }
                }
                catch (Exception e)
                {
                    // An error was caught while doing the web call.

                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                            "An error was caught while doing the web call.");

                    callerreturn.Call_ReturnCode = -5;
                    return callerreturn;
                }
                finally
                {
                    try
                    {
                        webResponse?.Close();
                    }
                    catch (Exception h)
                    { }
                    try
                    {
                        webResponse?.Dispose();
                    }
                    catch (Exception i)
                    { }
                }
            }
            finally
            {
                if (Ignore_SSL_Certificate_Errors)
                {
                    try
                    {
                        ServicePointManager.ServerCertificateValidationCallback -= sslFailureCallback;
                    }
                    catch (Exception e) { }
                }
            }
        }

        /// <summary>
        /// Simplifies functionality to send a request to a web service.
        /// Disposes of all streams and other instances used in request.
        /// This is the base call of all the web service request. And so, it contains all possible parameters.
        /// An HTTP Status code is passed back by this method, that provides feedback from the handling web service controller action.
        /// Returns the following:
        ///  1 = Successful web call. A response was received.
        ///  0 = Web Service is down.
        /// -1 = Request was unsuccessful.
        /// Other negative values indicate generic exception issues during web service call.
        /// </summary>
        /// <param name="url">The full url string of the web service call</param>
        /// <param name="username">Username to be passed as netcredential. Set to null if not used.</param>
        /// <param name="password">Password to be passed as netcredential. Set to null if not used.</param>
        /// <param name="verb">Http verp: GET, PUT, POST, DELETE, etc...</param>
        /// <returns></returns>
        static public cWebClient_Return Web_Request_Method(string url,
                                             string username,
                                             string password,
                                             NETCore_Common.WebService.eHttp_Verbs verb)
        {
            return Web_Request_Method(url, username, password, verb, null, null);
        }


        #region Private Methods

        static private HttpWebRequest Create_WebRequest(string url, NETCore_Common.WebService.eHttp_Verbs verb)
        {
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);

            req.Timeout = _request_timeout;

            req.ReadWriteTimeout = _request_timeout;

            // Assign the desired verb.
            req.Method = verb.ToString();

            return req;
        }

        #endregion
    }

    public enum eHttp_Verbs
    {
        GET,
        POST,
        PUT,
        PUSH,
        DELETE
    }
}
