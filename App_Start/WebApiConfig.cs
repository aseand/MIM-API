using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Text.RegularExpressions;


using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;

namespace LD.IdentityManagement.API
{
    public static class WebApiConfig
    {

        //[Authorize]
        public static void Register(HttpConfiguration config)
        {
           // Web API configuration and services
           //config.SuppressDefaultHostAuthentication();
           config.Filters.Add(new AuthorizeAttribute());
           config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));
           

            // Web API routes
            config.MapHttpAttributeRoutes();

           //config.Routes.MapHttpRoute(
           //    name: "DefaultApi",
           //    routeTemplate: "api/{controller}/{action}/"
           //    //defaults: new { id = RouteParameter.Optional }
           //);

            
            System.Net.Http.Formatting.MediaTypeFormatterCollection formatters = GlobalConfiguration.Configuration.Formatters;
            //formatters.XmlFormatter.UseXmlSerializer = true;
            formatters.Remove(formatters.XmlFormatter);
            //config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html") )

        }

    }
}
