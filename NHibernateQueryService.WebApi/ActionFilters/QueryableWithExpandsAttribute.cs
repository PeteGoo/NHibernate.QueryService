using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Query;

namespace NHibernateQueryService.WebApi.ActionFilters {
    public class QueryableWithExpandsAttribute : QueryableAttribute {
        public override void ValidateQuery(System.Net.Http.HttpRequestMessage request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }


            IEnumerable<KeyValuePair<string, string>> queryParameters = request.GetQueryNameValuePairs();
            foreach (KeyValuePair<string, string> kvp in queryParameters) {
                if (!ODataQueryOptions.IsSupported(kvp.Key) &&
                    !kvp.Key.Equals("$expand", StringComparison.InvariantCultureIgnoreCase) &&
                     kvp.Key.StartsWith("$", StringComparison.Ordinal)) {
                    // we don't allow any query parameters that starts with $ but we don't understand
                    throw new HttpResponseException(request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        string.Format("Query parameter {0} not supported", kvp.Key)));
                }
            }

        }
    }
}