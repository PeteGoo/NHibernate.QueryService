using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using NHibernate;
using NHibernate.Linq;
using NHibernateQueryService.Model;
using NHibernateQueryService.WebApi.ActionFilters;

namespace NHibernateQueryService.WebApi.Controllers {

    
    public class PeopleController : ApiController {
        private readonly ISession session;

        public PeopleController(ISession session) {
            this.session = session;
        }

        public IQueryable<Person> Get() {
            return session.Query<Person>();
            //.Cacheable().CacheRegion("People") // Not working until fix in NH https://nhibernate.jira.com/browse/NH-2856

        }
        
        protected override void Dispose(bool disposing) {
            if (session != null && session.IsOpen) {
                session.Close();
            }
            base.Dispose(disposing);
        }

    }
}
