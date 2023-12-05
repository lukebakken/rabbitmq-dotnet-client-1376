using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCore_Common.WebService
{
    /// <summary>
    /// Used to simplify the return data from a web client call.
    /// This class is usually returned from the web client call instead of a simple integer and ref-passed data.
    /// It includes the status code, json response, as well as, the regular return code of the function call.
    /// </summary>
    public class cWebClient_Return
    {
        /// <summary>
        /// Return value from function call, indicating if any errors occurred.
        /// The following values are returned:
        ///  1 - Web service returned a response to our request. See the HTTP Status code for more details.
        ///  0 - The web service is down, or a web exception occurred during the request.
        /// -2 - Exception occurred while pushing the request to the web service.
        /// -3 - Exception occurred while setting up the request, locally.
        /// -4 - Exception occurred while processing web response from the service.
        /// -5 - Exception occurred while processing web response from the service.
        /// </summary>
        public int Call_ReturnCode { get; set; }

        /// <summary>
        /// Response body from web service call.
        /// </summary>
        public string JSONResponse { get; set; }

        /// <summary>
        /// HTTP Status Code from web service call.
        /// </summary>
        public System.Net.HttpStatusCode StatusCode { get; set; }

        public cWebClient_Return()
        {
            Call_ReturnCode = 0;
            JSONResponse = "";
        }
    }
}
