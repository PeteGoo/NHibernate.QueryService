using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NHibernate.Context;
using NHibernateQueryService.Model;

namespace NHibernateQueryService.WebApi.Handlers {
    public class NHibernateSessionHandler : DelegatingHandler {

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) {
                return Task.Factory.StartNew(() => {
                    
                    // Setup the session
                    var session = NHibernateHelper.SessionFactory.OpenSession();
                    CurrentSessionContext.Bind(session);
                    session.BeginTransaction();

                    return new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                        Content = new StringContent("An unknown issue occurred after creating the session")
                    };
                })
                .ContinueWith(task => base.SendAsync(request, cancellationToken).Result)
                .ContinueWith(task => {
                    // Cleanup the session
                    var session = NHibernateHelper.SessionFactory.GetCurrentSession();

                    var transaction = session.Transaction;
                    if (transaction != null && transaction.IsActive) {
                        transaction.Commit();
                    }
                    session = CurrentSessionContext.Unbind(NHibernateHelper.SessionFactory);
                    session.Close();
                    return task.Result;
                });
        }
         
    }
}