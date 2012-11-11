using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using NHibernateQueryService.WebApi.ActionFilters;
using NHibernateQueryService.WebApi.Handlers;
using NHibernateQueryService.WebApi.Serialization;

namespace NHibernateQueryService.WebApi {
    public static class WebApiConfig {
        public static void Register(HttpConfiguration config) {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Filters.Add(new NhQueryableEnumeratorAttribute());
            
            config.EnableQuerySupport();

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new NHibernateContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new DynamicProxyJsonConverter());

            // To disable tracing in your application, please comment out or remove the following line of code
            // For more information, refer to: http://www.asp.net/web-api
            TraceConfig.Register(config);
        }
    }
}
