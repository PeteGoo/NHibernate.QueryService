using System;
using System.Web.Http.Dependencies;
using Ninject;
using Ninject.Syntax;

namespace NHibernateQueryService.WebApi.Ninject {
    /// <summary>
    /// Web API dependency scope for Ninject
    /// </summary>
    public class NinjectDependencyScope : IDependencyScope {
        IResolutionRoot resolver;

        public NinjectDependencyScope(IResolutionRoot resolver) {
            this.resolver = resolver;
        }

        public object GetService(Type serviceType) {
            if (resolver == null)
                throw new ObjectDisposedException("this", "This scope has been disposed");

            return resolver.TryGet(serviceType);
        }

        public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType) {
            if (resolver == null)
                throw new ObjectDisposedException("this", "This scope has been disposed");

            return resolver.GetAll(serviceType);
        }

        public void Dispose() {
            IDisposable disposable = resolver as IDisposable;
            if (disposable != null)
                disposable.Dispose();

            resolver = null;
        }
    }
}