using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace NHibernateQueryService {


    public class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
            log4net.Config.XmlConfigurator.Configure();
            SqlDependency.Start(ConfigurationManager.ConnectionStrings["default"].ConnectionString);
            
        }

        protected void Session_Start(object sender, EventArgs e) {

        }

        protected void Application_BeginRequest(object sender, EventArgs e) {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e) {

        }

        protected void Application_Error(object sender, EventArgs e) {

        }

        protected void Session_End(object sender, EventArgs e) {

        }

        protected void Application_End(object sender, EventArgs e) {
            SqlDependency.Stop("Server=localhost;Database=NHibernateQueryService;User Id=queryservice;Password=queryservice;");
        }
    }
}